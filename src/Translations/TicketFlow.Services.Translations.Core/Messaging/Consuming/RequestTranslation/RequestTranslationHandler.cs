using TicketFlow.Services.Translations.Core.Messaging.Publishing;
using TicketFlow.Services.Translations.Core.Translations;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;

internal sealed class RequestTranslationHandler(ITranslationsService translationsService, IMessagePublisher messagePublisher) 
    : IMessageHandler<RequestTranslation>
{
    public async Task HandleAsync(RequestTranslation message, CancellationToken cancellationToken = default)
    {
        var translatedText = await translationsService.TranslateAsync(
            message.Text,
            TranslationLanguage.English,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(translatedText))
        {
            var translationSkippedMessage = new TranslationSkipped(message.Text, message.TicketId);
            await messagePublisher.PublishAsync(translationSkippedMessage, destination: "translation-completed-exchange", cancellationToken: cancellationToken);
            return;
        }
        
        var translationCompletedMessage = new TranslationCompleted(message.Text, translatedText, message.TicketId);
        await messagePublisher.PublishAsync(translationCompletedMessage, destination: "translation-completed-exchange", cancellationToken: cancellationToken);
    }
}