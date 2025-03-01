using Microsoft.Extensions.Hosting;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.DeadlinesCalculated;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.InquirySubmitted;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.TranslationCompleted;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing.Conventions;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.AzureServiceBus;

namespace TicketFlow.Services.Tickets.Core.Messaging;

internal sealed class TicketsConsumerService(IMessageConsumer messageConsumer, AnomalySynchronizationConfigurator anomalyConfigurator) : BackgroundService
{
    public const string SLATopic = "sla-exchange";
    public const string SLAChangesSubscription = "tickets-sla-changes";
    public const string TicketCreatedSubscription = "tickets-ticket-created";
    public const string InquirySubmittedTopic = "inquiry-submitted-exchange";
    public const string InquirySubmittedSubscription = "tickets-inquiry-submitted";
    public const string TranslationCompletedTopic = "translation-completed-exchange";
    public const string TranslationCompletedSubscription = "tickets-translation-completed";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageConsumer.ConsumeMessage<InquirySubmitted>(
            queue: AzureServiceBusConventions.ForTopicAndSubscription(InquirySubmittedTopic, InquirySubmittedSubscription));
        
        await messageConsumer.ConsumeMessage<TranslationCompleted>(
            queue: AzureServiceBusConventions.ForTopicAndSubscription(TranslationCompletedTopic, TranslationCompletedSubscription));
        
        await messageConsumer.ConsumeMessage<DeadlinesCalculated>(
            queue: AzureServiceBusConventions.ForTopicAndSubscription(SLATopic, SLAChangesSubscription), 
            acceptedMessageTypes: ["DeadlinesCalculated"]);
        
        if (FeatureFlags.UseListenToYourselfExample)
        {
            await messageConsumer.ConsumeMessage<TicketCreated>(
                queue: AzureServiceBusConventions.ForTopicAndSubscription(TicketsMessagePublisherConventionProvider.TopicName, TicketCreatedSubscription), 
                acceptedMessageTypes: ["TicketCreated"]);
        }
        await anomalyConfigurator.ConsumeAnomalyChanges();
    }
}