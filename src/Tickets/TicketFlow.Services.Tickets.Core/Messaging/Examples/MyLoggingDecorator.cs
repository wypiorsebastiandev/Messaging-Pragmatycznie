using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Examples;

public class MyLoggingDecorator<TMessage>(IMessageHandler<TMessage> handler, ILogger<IMessageHandler<TMessage>> logger) 
    : IMessageHandler<TMessage> where TMessage : class, IMessage
{
    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Before handling message: {message.GetType().Name} || {message}");
        await handler.HandleAsync(message, cancellationToken);
        logger.LogInformation($"After handling message: {message.GetType().Name}|| {message}");
    }
}