using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;

public sealed record RequestTranslation(
    string Text, 
    string TranslateFrom, /* Don't repeat detection */ 
    Guid ReferenceId /* Add flexibility */) : IMessage;