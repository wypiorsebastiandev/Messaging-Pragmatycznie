using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Messaging.Publishing;

public sealed record RequestTranslation(string Text, Guid TicketId) : IMessage;