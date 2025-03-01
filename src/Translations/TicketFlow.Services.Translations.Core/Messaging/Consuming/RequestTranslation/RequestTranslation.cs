using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;

public sealed record RequestTranslation(string Text, Guid TicketId) : IMessage;