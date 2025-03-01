using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Executor;

namespace TicketFlow.Services.Tickets.Core.Messaging.Examples;

public class MyLoggingStep : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.WithinTransaction;
    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("I am inside MyExecutionStep with properties:");
        Console.WriteLine(messageProperties.ToString());
        
        await next();
    }
}