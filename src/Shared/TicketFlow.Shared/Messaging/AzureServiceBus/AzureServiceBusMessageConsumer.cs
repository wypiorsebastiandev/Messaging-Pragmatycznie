using System.Diagnostics;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Messaging.Partitioning;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.Messaging.AzureServiceBus;

internal class AzureServiceBusMessageConsumer(
    ServiceBusClient client,
    IServiceProvider serviceProvider,
    ISerializer serializer,
    ResiliencyOptions resiliencyOptions,
    MessagePropertiesAccessor messagePropertiesAccessor,
    TopologyReadinessAccessor topologyReadinessAccessor,
    ILogger<AzureServiceBusMessageConsumer> logger) : IMessageConsumer
{
    private readonly Dictionary<string, ServiceBusProcessor> _registeredProcessors = new();
    private readonly Dictionary<string, ServiceBusSessionProcessor> _registeredSessionProcessors = new();
    
    public async Task<IMessageConsumer> ConsumeMessage<TMessage>(
        Func<TMessage, Task>? handle = default,
        string? queue = default,
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default) where TMessage : class, IMessage
    {
        if (queue == default)
        {
            throw new ArgumentNullException(nameof(queue), "Queue or subscription+topic is required for Azure Service Bus messages.");
        }

        var processorOptions = new ServiceBusProcessorOptions
        {
            PrefetchCount = resiliencyOptions.Consumer.MaxMessagesFetchedPerConsumer
        };
        
        var processor = AzureServiceBusConventions.TryGetTopicAndSubscription(queue, out var topic, out var subscription) ?
            client.CreateProcessor(topic, subscription, processorOptions) :
            client.CreateProcessor(queue, processorOptions);

        processor.ProcessMessageAsync += async args =>
        {
            SetMessageProperties(args, args.Message.DeliveryCount > 1);

            if (IsNotAcceptedMessageType(acceptedMessageTypes, args))
            {
                await args.CompleteMessageAsync(args.Message, cancellationToken);
                return;
            }

            await HandleMessageAsync(args, handle, cancellationToken);

            await args.CompleteMessageAsync(args.Message, cancellationToken);
        };
            
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "An error occured while processing a message");
            return Task.CompletedTask;
        };
        
        await EnsureTopologyReady(cancellationToken);
        await processor.StartProcessingAsync(cancellationToken);
        _registeredProcessors.Add(processor.Identifier, processor);

        return this;
    }

    public async Task<IMessageConsumer> ConsumeNonGeneric(
        Func<MessageData, Task> handleRawPayload, 
        string queue, 
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default)
    {
        if (queue == default)
        {
            throw new ArgumentNullException(nameof(queue), "Queue or subscription+topic is required for Azure Service Bus messages.");
        }

        var processorOptions = new ServiceBusProcessorOptions
        {
            PrefetchCount = resiliencyOptions.Consumer.MaxMessagesFetchedPerConsumer
        };
        
        var processor = AzureServiceBusConventions.TryGetTopicAndSubscription(queue, out var topic, out var subscription) ?
            client.CreateProcessor(topic, subscription, processorOptions) :
            client.CreateProcessor(queue, processorOptions);

        processor.ProcessMessageAsync += async args =>
        {
            SetMessageProperties(args, args.Message.DeliveryCount > 1);

            if (IsNotAcceptedMessageType(acceptedMessageTypes, args))
            {
                await args.CompleteMessageAsync(args.Message, cancellationToken);
                return;
            }

            var messageData = CreateMessageData(args);
            await handleRawPayload(messageData);

            await args.CompleteMessageAsync(args.Message, cancellationToken);
        };
            
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "An error occured while processing a message");
            return Task.CompletedTask;
        };
        
        await EnsureTopologyReady(cancellationToken);
        await processor.StartProcessingAsync(cancellationToken);
        _registeredProcessors.Add(processor.Identifier, processor);

        return this;
    }

    public async Task GetMessage<TMessage>(Func<TMessage, Task> handle, string? queue = default, CancellationToken cancellationToken = default) where TMessage : class, IMessage
    {
        if (queue == default)
        {
            throw new ArgumentNullException(nameof(queue), "Queue or subscription+topic is required for Azure Service Bus messages.");
        }
        
        var receiver = AzureServiceBusConventions.TryGetTopicAndSubscription(queue, out var topic, out var subscription) ?
            client.CreateReceiver(topic, subscription, new ServiceBusReceiverOptions()) :
            client.CreateReceiver(queue, new ServiceBusReceiverOptions());
        
        await EnsureTopologyReady(cancellationToken);
        var result = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1), cancellationToken);
        if (result is null)
        {
            return;
        }
        var message = serializer.DeserializeBinary<TMessage>(result.Body.ToArray());
        SetMessageProperties(result, result.DeliveryCount > 1);

        await handle(message);
        
        await receiver.CompleteMessageAsync(result, cancellationToken);
    }

    public async Task<IMessageConsumer> ConsumeMessageFromPartitions<TMessage>(
        IConsumerSpecificPartitioningSetup consumerPartitioningSetup,
        Func<TMessage, Task>? handle = default, 
        string? queue = default, 
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default) where TMessage : class, IMessage
    {
        //Ignore PartitionNumbersToConsume - Azure SDK handles it differently:
        //It does not distribute (at least visibly for us) partition keys per "buckets"
        //instead it claims "random", yet unclaimed partitions by partitionKey and locks them until it finishes processing
                
        if (queue == default)
        {
            throw new ArgumentNullException(nameof(queue), "Queue or subscription+topic is required for Azure Service Bus messages.");
        }

        var processorOptions = new ServiceBusSessionProcessorOptions
        {
            PrefetchCount = resiliencyOptions.Consumer.MaxMessagesFetchedPerConsumer,
            MaxConcurrentSessions = consumerPartitioningSetup.PartitioningOptions.NumberOfPartitions,
            MaxConcurrentCallsPerSession = 1 // default anyway, explicit for demo
        };
        
        var processor = AzureServiceBusConventions.TryGetTopicAndSubscription(queue, out var topic, out var subscription) ?
            client.CreateSessionProcessor(topic, subscription, processorOptions) :
            client.CreateSessionProcessor(queue, processorOptions);

        processor.ProcessMessageAsync += async args =>
        {
            SetMessageProperties(args, args.Message.DeliveryCount > 1);

            if (IsNotAcceptedMessageType(acceptedMessageTypes, args))
            {
                await args.CompleteMessageAsync(args.Message, cancellationToken);
                return;
            }

            await HandleMessageAsync(args, handle, cancellationToken);

            await args.CompleteMessageAsync(args.Message, cancellationToken);
        };
            
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "An error occured while processing a message");
            return Task.CompletedTask;
        };
        
        await EnsureTopologyReady(cancellationToken);
        await processor.StartProcessingAsync(cancellationToken);
        _registeredSessionProcessors.Add(processor.Identifier, processor);

        return this;
    }

    public async Task<IMessageConsumer> ConsumeNonGenericFromPartitions(
        IConsumerSpecificPartitioningSetup consumerPartitioningSetup,
        Func<MessageData, Task> handleRawPayload, 
        string? queue, 
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default)
    {
        //Ignore PartitionNumbersToConsume - Azure SDK handles it differently:
        //It does not distribute (at least visibly for us) partition keys per "buckets" 
        //instead it claims "random", yet unclaimed partitions by partitionKey and locks them until it finishes processing
        
        if (queue == default)
        {
            throw new ArgumentNullException(nameof(queue), "Queue or subscription+topic is required for Azure Service Bus messages.");
        }

        var processorOptions = new ServiceBusSessionProcessorOptions
        {
            PrefetchCount = resiliencyOptions.Consumer.MaxMessagesFetchedPerConsumer,
            MaxConcurrentSessions = consumerPartitioningSetup.PartitioningOptions.NumberOfPartitions,
            MaxConcurrentCallsPerSession = 1 // default anyway, explicit for demo
        };
        
        var processor = AzureServiceBusConventions.TryGetTopicAndSubscription(queue, out var topic, out var subscription) ?
            client.CreateSessionProcessor(topic, subscription, processorOptions) :
            client.CreateSessionProcessor(queue, processorOptions);

        processor.ProcessMessageAsync += async args =>
        {
            SetMessageProperties(args, args.Message.DeliveryCount > 1);

            if (IsNotAcceptedMessageType(acceptedMessageTypes, args))
            {
                await args.CompleteMessageAsync(args.Message, cancellationToken);
                return;
            }

            var messageData = CreateMessageData(args);
            await handleRawPayload(messageData);

            await args.CompleteMessageAsync(args.Message, cancellationToken);
        };
            
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "An error occured while processing a message");
            return Task.CompletedTask;
        };
        
        await EnsureTopologyReady(cancellationToken);
        await processor.StartProcessingAsync(cancellationToken);
        _registeredSessionProcessors.Add(processor.Identifier, processor);

        return this;
    }

    private async Task HandleMessageAsync<TMessage>(
        ProcessMessageEventArgs args,
        Func<TMessage, Task>? handle = null,
        CancellationToken cancellationToken = default) where TMessage : class, IMessage
    {
        var message = serializer.DeserializeBinary<TMessage>(args.Message.Body.ToArray());

        logger.LogWarning($"[{DateTime.UtcNow:O}] Received message:{Environment.NewLine} {message}");
        
        if (handle is null)
        {
            var scope = serviceProvider.CreateScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();

            await messageHandler.HandleAsync(message, cancellationToken);
        }
        else
        {
            await handle(message);
        }
        
        logger.LogWarning($"[{DateTime.UtcNow:O}] Processed message:{Environment.NewLine} {message}");
    }
    
    private async Task HandleMessageAsync<TMessage>(
        ProcessSessionMessageEventArgs args,
        Func<TMessage, Task>? handle = null,
        CancellationToken cancellationToken = default) where TMessage : class, IMessage
    {
        var message = serializer.DeserializeBinary<TMessage>(args.Message.Body.ToArray());

        logger.LogWarning($"[{DateTime.UtcNow:O}] Received message:{Environment.NewLine} {message}");
        
        if (handle is null)
        {
            var scope = serviceProvider.CreateScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();

            await messageHandler.HandleAsync(message, cancellationToken);
        }
        else
        {
            await handle(message);
        }
        
        logger.LogWarning($"[{DateTime.UtcNow:O}] Processed message:{Environment.NewLine} {message}");
    }
    
    private void SetMessageProperties(ProcessMessageEventArgs args, bool redelivered = false)
    {
        var messageId = GetMessageId(args.Message.MessageId);
        var headers = args.Message.ApplicationProperties?
            .Select(x => (x.Key, x.Value)).ToDictionary();
        
        var messageType = args.Message.ApplicationProperties[AzureServiceBusMessagePublisher.MessageTypePropertyName] as string;
        var messageProperties = new MessageProperties(messageId.ToString(), headers, messageType, redelivered);
        messagePropertiesAccessor.Set(messageProperties);
    }
    
    private void SetMessageProperties(ServiceBusReceivedMessage msg, bool redelivered = false)
    {
        var messageId = GetMessageId(msg.MessageId);
        var headers = msg.ApplicationProperties?
            .Select(x => (x.Key, x.Value)).ToDictionary();
        
        var messageType = msg.ApplicationProperties[AzureServiceBusMessagePublisher.MessageTypePropertyName] as string;
        var messageProperties = new MessageProperties(messageId.ToString(), headers, messageType, redelivered);
        messagePropertiesAccessor.Set(messageProperties);
    }
    
    private void SetMessageProperties(ProcessSessionMessageEventArgs args, bool redelivered = false)
    {
        var messageId = GetMessageId(args.Message.MessageId);
        var headers = args.Message.ApplicationProperties?
            .Select(x => (x.Key, x.Value)).ToDictionary();
        
        var messageType = args.Message.ApplicationProperties[AzureServiceBusMessagePublisher.MessageTypePropertyName] as string;
        var messageProperties = new MessageProperties(messageId.ToString(), headers, messageType, redelivered);
        messagePropertiesAccessor.Set(messageProperties);
    }
    
    private static bool IsNotAcceptedMessageType(string[]? acceptedMessageTypes, ProcessMessageEventArgs args)
    {
        return acceptedMessageTypes is not null && !acceptedMessageTypes.Contains(
            args.Message.ApplicationProperties[AzureServiceBusMessagePublisher.MessageTypePropertyName] as string);
    }
    
    private static bool IsNotAcceptedMessageType(string[]? acceptedMessageTypes, ProcessSessionMessageEventArgs args)
    {
        return acceptedMessageTypes is not null && !acceptedMessageTypes.Contains(
            args.Message.ApplicationProperties[AzureServiceBusMessagePublisher.MessageTypePropertyName] as string);
    }
    
    private MessageData CreateMessageData(ProcessMessageEventArgs args)
    {
        var messageId = GetMessageId(args.Message.MessageId);
        
        var messageType = args.Message.ApplicationProperties[AzureServiceBusMessagePublisher.MessageTypePropertyName] as string;
        var payload = args.Message.Body.ToArray();
        
        return new MessageData(messageId, payload, messageType);
    }
    
    private MessageData CreateMessageData(ProcessSessionMessageEventArgs args)
    {
        var messageId = GetMessageId(args.Message.MessageId);
        
        var messageType = args.Message.ApplicationProperties[AzureServiceBusMessagePublisher.MessageTypePropertyName] as string;
        var payload = args.Message.Body.ToArray();
        
        return new MessageData(messageId, payload, messageType);
    }

    private static Guid GetMessageId(string messageIdRaw)
    {
        var messageId = messageIdRaw.Contains('-')
            ? Guid.Parse(messageIdRaw) // Generated by us, normal Guid with "-";
            : Guid.ParseExact(messageIdRaw, "N"); // Generated by ServiceBus with no "-";
        return messageId;
    }
    
    private async Task EnsureTopologyReady(CancellationToken cancellationToken)
    {
        while (topologyReadinessAccessor.TopologyProvisioned is false)
        {
            logger.LogInformation("Waiting for topology to be provisioned...");
            await Task.Delay(1000, cancellationToken);
        }
        logger.LogInformation("Topology ready!");
    }
}