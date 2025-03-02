using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketFlow.KafkaPlayground.Shared;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Inquiries.Core.Messaging;

public class InquiriesTopologyInitializer : TopologyInitializerBase
{
    public InquiriesTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {}
    
    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        await CreateAnomalySynchronizationTopology(stoppingToken);
        
        var topologyBuilder = ServiceProvider.GetService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "tickets-exchange",
            consumerDestination: InquiriesConsumerService.TicketCreatedQueue,
            TopologyType.PublishSubscribe,
            filter: "ticket-created",
            cancellationToken: stoppingToken
        );
        
        var topologyInitializer = ServiceProvider.GetService<KafkaTopologyInitializer>();
        await topologyInitializer!.CreateTopicAsync("inquiry-changes", numberOfPartitions: 3);
    }
}