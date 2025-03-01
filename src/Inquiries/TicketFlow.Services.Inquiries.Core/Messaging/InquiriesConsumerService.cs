using Microsoft.Extensions.Hosting;
using TicketFlow.Services.Inquiries.Core.Messaging.Consuming.TicketCreated;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.AzureServiceBus;

namespace TicketFlow.Services.Inquiries.Core.Messaging;

public class InquiriesConsumerService(IMessageConsumer messageConsumer, AnomalySynchronizationConfigurator anomalyConfigurator) : BackgroundService
{
    public const string TicketCreatedQueue = "inquiries-ticket-created";
    public const string TicketsTopic = "tickets-exchange";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageConsumer
            .ConsumeMessage<TicketCreated>(
                queue: AzureServiceBusConventions.ForTopicAndSubscription(TicketsTopic, TicketCreatedQueue),
                acceptedMessageTypes: null, // Handled by filter on ASB instead (RoutingKey=ticket-created)
                cancellationToken: stoppingToken);
        
        await anomalyConfigurator.ConsumeAnomalyChanges();
    }
}