namespace TicketFlow.Services.Translations.Core.Translations;

public interface ITranslationsService
{
    Task<string> TranslateAsync(string text, string translateTo, CancellationToken cancellationToken = default);
}