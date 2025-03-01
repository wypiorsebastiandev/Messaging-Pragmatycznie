using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.KafkaPlayground.Shared;

public static class Extensions
{
    public static IServiceCollection AddKafka(this IServiceCollection services,
        ProducerConfig? producerConfig = default,
        ConsumerConfig? consumerConfig = default)
    {
        services
            .AddSingleton<ProducerConfig>(_ =>
            {
                if (producerConfig is not null)
                {
                    producerConfig.BootstrapServers = "localhost:9092";
                    return producerConfig;
                }

                return new ProducerConfig
                {
                    BootstrapServers = "localhost:9092"
                };
            })
            .AddSingleton<ConsumerConfig>(_ =>
            {
                if (consumerConfig is not null)
                {
                    consumerConfig.BootstrapServers = "localhost:9092";
                    return consumerConfig;
                }

                return new ConsumerConfig
                {
                    BootstrapServers = "localhost:9092"
                };
            })
            .AddTransient<IAdminClient>(_ => new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = "localhost:9092"
            }).Build())
            .AddSingleton<KafkaPublisher>()
            .AddSingleton<KafkaTopologyInitializer>();

        return services;
    }

    public static IServiceCollection AddKafkaConsumer<TMessage>(this IServiceCollection services, string topicName) where TMessage : class, IMessage
    {
        services.AddHostedService<KafkaConsumer<TMessage>>(provider =>
            new KafkaConsumer<TMessage>(
                provider.GetService<ConsumerConfig>()!, 
                topicName,
                provider.GetService<ISerializer>()!,
                provider));
        return services;
    }
}