using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Translations.Core.Messaging;

public class TranslationTopologyInitializer : TopologyInitializerBase
{
    public TranslationTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        var topologyBuilder = ServiceProvider.GetService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "",
            consumerDestination: TranslationsConsumerService.RequestTranslationV2Queue,
            TopologyType.Direct,
            cancellationToken: stoppingToken
        );
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: RequestTranslationHandler.TranslationCompletedTopic,
            consumerDestination: "", // As publisher, we are consumer-ignorant
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);
    }
}