using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.KafkaPlayground.Shared;

public class KafkaConsumer<TMessage>(
    ConsumerConfig consumerConfig,
    string topic,
    ISerializer serializer,
   IServiceProvider serviceProvider) : BackgroundService where TMessage : class, IMessage
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var consumer = new ConsumerBuilder<string, TMessage>(consumerConfig)
                   .SetValueDeserializer(new KafkaSerializer<TMessage>(serializer))
                   .Build())
        {
            consumer.Subscribe(topic);

            int consumed = 0;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);

                var iocScope = serviceProvider.CreateScope();
                var handler = iocScope.ServiceProvider.GetService<IMessageHandler<TMessage>>();
                await handler!.HandleAsync(consumeResult.Message.Value, stoppingToken);
                
                ManualCommitIfEnabled(consumed, consumer, consumeResult);
                ManualOffsetIfEnabled(consumer, consumeResult);
                
                Console.WriteLine($"{consumeResult.Message.Key} => {consumeResult.Message.Value}");
            }

            consumer.Close();
        }
        

        void ManualCommitIfEnabled(int consumed, IConsumer<string, TMessage> consumer, ConsumeResult<string, TMessage> consumeResult)
        {
            if (consumerConfig.EnableAutoCommit is false && consumed % 10 == 0)
            {
                consumer.Commit(consumeResult);
            }
        }
        
        void ManualOffsetIfEnabled(IConsumer<string, TMessage> consumer, ConsumeResult<string, TMessage> consumeResult)
        {
            if (consumerConfig.EnableAutoCommit is true && consumerConfig.EnableAutoOffsetStore is false)
            {
                consumer.StoreOffset(consumeResult);
            }
        }
    }
}