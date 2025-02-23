namespace TicketFlow.Services.Translations.Core.Translations;

internal sealed class NoopTranslationsService : ITranslationsService
{
    public async Task<string> TranslateAsync(string text, string translateFrom, string translateTo,
        CancellationToken cancellationToken = default)
    {
        throw new Exception("Opps! We got a problem over here!");
        return "This is Noop translation";
    }
}