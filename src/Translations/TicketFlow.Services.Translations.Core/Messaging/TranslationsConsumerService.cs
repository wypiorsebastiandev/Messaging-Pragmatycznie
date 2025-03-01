using Microsoft.Extensions.Hosting;
using TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging;

internal sealed class TranslationsConsumerService(IMessageConsumer messageConsumer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await messageConsumer.ConsumeMessage<RequestTranslation>(queue: "request-translation-queue", cancellationToken: cancellationToken);
    }
}