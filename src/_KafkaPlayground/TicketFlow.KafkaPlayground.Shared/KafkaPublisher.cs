using Confluent.Kafka;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.KafkaPlayground.Shared;

public class KafkaPublisher(ProducerConfig config, ISerializer serializer) : IDisposable
{
    private readonly Lazy<IProducer<string, object>> _producer = new(() => 
        new ProducerBuilder<string, object>(config)
            .SetValueSerializer(new KafkaSerializer<object>(serializer))
            .Build());
    private IProducer<string, object> Producer => _producer.Value;
    
    public async Task PublishBlockingAsync<TMessage>(
        TMessage message, 
        string topic,
        string? messageId = default,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        await Producer.ProduceAsync(topic, new Message<string, object>
        {
            Key = messageId ?? Guid.NewGuid().ToString(),
            Value = message
        }, cancellationToken);
    }
    
    public void PublishNonBlockingAsync<TMessage>(
        TMessage message, 
        string topic,
        Action<DeliveryReport<string, object>> deliveryHandler,
        string? messageId = default,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        Producer.Produce(topic, new Message<string, object>
        {
            Key = messageId ?? Guid.NewGuid().ToString(),
            Value = message
        },
        deliveryHandler);
    }

    public void Dispose()
    {
        Producer.Dispose();
    }
}