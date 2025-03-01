using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace TicketFlow.KafkaPlayground.Shared;

public class KafkaTopologyInitializer(IAdminClient adminClient)
{
    public async Task CreateTopicAsync(string topicName, int numberOfPartitions)
    {
        var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(20));

        var topicExists = meta.Topics.Any(x => x.Topic == topicName);
        if (topicExists)
        {
            return;
        }
        
        await adminClient.CreateTopicsAsync([ 
            new TopicSpecification
            {
                Name = topicName, 
                NumPartitions = numberOfPartitions
            } 
        ]);
    }
}