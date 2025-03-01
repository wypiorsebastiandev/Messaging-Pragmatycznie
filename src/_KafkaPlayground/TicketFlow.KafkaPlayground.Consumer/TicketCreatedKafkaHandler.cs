using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.KafkaPlayground.Consumer;

public class TicketCreatedKafkaHandler : IMessageHandler<TicketCreated>
{
    public Task HandleAsync(TicketCreated message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{nameof(TicketCreatedKafkaHandler)} handled message: {message}");
        return Task.CompletedTask;
    }
}