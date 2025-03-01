using Confluent.Kafka;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.KafkaPlayground.Shared;

public class KafkaSerializer<TMessage>(ISerializer serializer) : ISerializer<TMessage>, IDeserializer<TMessage>
{
    public byte[] Serialize(TMessage data, SerializationContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return serializer.SerializeBinary(data);
    }

    public TMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return serializer.DeserializeBinary<TMessage>(data.ToArray())!;
    }
}