using Microsoft.Extensions.Hosting;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.DeadlinesCalculated;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.InquirySubmitted;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.TranslationCompleted;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging;

internal sealed class TicketsConsumerService(IMessageConsumer messageConsumer, AnomalySynchronizationConfigurator anomalyConfigurator) : BackgroundService
{
    public const string SLAChangesQueue = "tickets-sla-changes";
    public const string TicketCreatedQueue = "tickets-ticket-created";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageConsumer.ConsumeMessage<InquirySubmitted>(queue: "inquiry-submitted-tickets-service-queue");
    }
}