namespace TicketFlow.Services.Translations.Core.Translations;

internal sealed class NoopTranslationsService : ITranslationsService
{
    public async Task<string> TranslateAsync(string text, string translateFrom, string translateTo,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(3000);
        return "This is Noop translation";   
    }
}