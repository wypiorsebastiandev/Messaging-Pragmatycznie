using System.Transactions;
using Azure.Messaging.ServiceBus;
using RabbitMQ.Client;
using TicketFlow.CourseUtils;
using TicketFlow.Shared.Messaging.Partitioning;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.Messaging.AzureServiceBus;

internal class AzureServiceBusMessagePublisher(
    ISerializer serializer, 
    MessagePropertiesAccessor propertiesAccessor,
    ServiceBusClient client) : IMessagePublisher
{
    public const string RoutingKeyPropertyName = "RoutingKey";
    public const string MessageTypePropertyName = "MessageType";
    
    public async Task PublishAsync<TMessage>(TMessage message, string? destination = default, string? routingKey = default,
        string? messageId = default, IDictionary<string, object>? headers = default, CancellationToken cancellationToken = default) where TMessage : class, IMessage
    {
        //Required because for .NET clients, Azure SDK tries to enlist ServiceBus into distributed transaction...
        using var scope = SuppressEnlistingToDistributedTrx();
        
        if (destination == default)
        {
            throw new ArgumentNullException(nameof(destination), "Destination is required for Azure Service Bus messages.");
        }
        
        var payload = serializer.SerializeBinary(message);
        var envelope = new ServiceBusMessage(payload);
        
        CreateMessageProperties<TMessage>(
            envelope: envelope, 
            messageId: messageId, 
            headers: headers,
            routingKey: routingKey ?? CreateDefaultRoutingKey<TMessage>());
        
        SetPartitionKey(envelope, message);

        var sender = client.CreateSender(destination);
        await sender.SendMessageAsync(envelope, cancellationToken);
        scope.Complete();
    }

    private void CreateMessageProperties<TMessage>(ServiceBusMessage envelope, string? messageId = default,
        IDictionary<string, object>? headers = default, string? routingKey = null)
        where TMessage : class,IMessage
    {
        var messageProperties = propertiesAccessor.Get();

        envelope.MessageId = messageId ?? Guid.NewGuid().ToString();
        envelope.ApplicationProperties[MessageTypePropertyName] = MessageTypeName.CreateFor<TMessage>();
        envelope.ApplicationProperties[RoutingKeyPropertyName] = routingKey;
        
        var headersToAdd = headers 
                           ?? messageProperties?.Headers 
                           ?? Enumerable.Empty<KeyValuePair<string, object>>();

        foreach (var header in headersToAdd)
        {
            if (envelope.ApplicationProperties.ContainsKey(header.Key))
            {
                envelope.ApplicationProperties[header.Key] = header.Value.ToString();
            }
            else
            {
                envelope.ApplicationProperties.Add(header.Key, header.Value.ToString());   
            }
        }
    }
    
    private void SetPartitionKey<TMessage>(ServiceBusMessage envelope, TMessage message)
    {
        if (FeatureFlags.UsePartitioningExample is false)
        {
            return;
        }

        if (typeof(IMessageWithPartitionKey).IsAssignableFrom(typeof(TMessage)))
        {
            var partitionKey = (message as IMessageWithPartitionKey)?.PartitionKey;
            envelope.SessionId = partitionKey;
        }
    }

    private string CreateDefaultRoutingKey<TMessage>() where TMessage : IMessage
    {
        var messageType = typeof(TMessage).Name;
        return string.Concat(messageType.SelectMany(ConvertChar));

        IEnumerable<char> ConvertChar(char c, int index)
        {
            if (char.IsUpper(c) && index != 0) yield return '-';
            yield return char.ToLower(c);
        }
    }
    
    private TransactionScope SuppressEnlistingToDistributedTrx()
    {
        var opts = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
        };
        return new TransactionScope(
            TransactionScopeOption.Suppress,
            opts,
            TransactionScopeAsyncFlowOption.Enabled);
    }
}