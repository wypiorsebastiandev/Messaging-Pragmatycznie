namespace TicketFlow.Services.Translations.Core.Translations;

internal sealed class NoopTranslationsService : ITranslationsService
{
    public async Task<string> TranslateAsync(string text, string translateFrom, string translateTo,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10_000, cancellationToken);
        return "This is Noop translation";
    }
}