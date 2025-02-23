using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.InquirySubmitted;

public sealed record InquirySubmitted(Guid Id, string Name, string Email, string Title, string Description, string Category, string LanguageCode, DateTimeOffset CreatedAt) : IMessage;