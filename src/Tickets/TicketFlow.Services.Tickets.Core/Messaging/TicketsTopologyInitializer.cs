using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Communication.Alerting;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.TicketCreated;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing.Conventions;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Tickets.Core.Messaging;

public class TicketsTopologyInitializer : TopologyInitializerBase
{
    public TicketsTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider){}

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        await CreateAlertingTopology(stoppingToken);
        await CreateAnomalySynchronizationTopology(stoppingToken);

        var topologyBuilder = ServiceProvider.GetService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: TicketsMessagePublisherConventionProvider.ExchangeName,
            consumerDestination: "", // As publisher, we are consumer-ignorant
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken
        );

        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "sla-exchange",
            consumerDestination: TicketsConsumerService.SLAChangesQueue,
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);

        if (FeatureFlags.UseListenToYourselfExample)
        {
            await topologyBuilder.CreateTopologyAsync(
                publisherSource: TicketsMessagePublisherConventionProvider.ExchangeName,
                consumerDestination: TicketsConsumerService.TicketCreatedQueue,
                TopologyType.PublishSubscribe,
                cancellationToken: stoppingToken);
        }
    }
    
    private async Task CreateAlertingTopology(CancellationToken stoppingToken)
    {
        var alertingTopologyBuilder = new AlertingTopologyBuilder(
            ServiceProvider.GetService<ITopologyBuilder>(),
            ServiceProvider.GetService<IMessagePublisherConventionProvider>());

        await alertingTopologyBuilder.CreateTopologyAsync(stoppingToken);
    }
}