using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.DeadlinesCalculated;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.InquirySubmitted;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.TranslationCompleted;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging;

internal sealed class TicketsConsumerService(
    IMessageConsumer messageConsumer, 
    AnomalySynchronizationConfigurator anomalyConfigurator, 
    IServiceProvider serviceProvider) : BackgroundService
{
    public const string SLAChangesQueue = "tickets-sla-changes";
    public const string TicketCreatedQueue = "tickets-ticket-created";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // await messageConsumer.ConsumeMessage<InquirySubmitted>(queue: "inquiry-submitted-tickets-service-queue");

        while (stoppingToken.IsCancellationRequested is false)
        {
            await messageConsumer.GetMessage<InquirySubmitted>(async message =>
                {
                    var iocScope = serviceProvider.CreateScope();
                    var handler = iocScope.ServiceProvider.GetService<IMessageHandler<InquirySubmitted>>();
                    await handler!.HandleAsync(message, stoppingToken);
                },
                queue: "inquiry-submitted-tickets-service-queue",
                cancellationToken: stoppingToken);
            
            await Task.Delay(100, stoppingToken);
        }
    }
}