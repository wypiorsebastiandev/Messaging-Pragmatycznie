using Microsoft.Extensions.Hosting;
using TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging;

internal sealed class TranslationsConsumerService(IMessageConsumer messageConsumer) : BackgroundService
{
    public const string RequestTranslationV2Queue = "request-translation-v2-queue";

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await messageConsumer.ConsumeMessage<RequestTranslationV2>(queue: RequestTranslationV2Queue, cancellationToken: cancellationToken);
    }
}