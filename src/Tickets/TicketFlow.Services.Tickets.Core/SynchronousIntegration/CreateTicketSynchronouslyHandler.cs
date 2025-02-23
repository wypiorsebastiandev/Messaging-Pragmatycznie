using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.SynchronousIntegration;

internal sealed class CreateTicketSynchronouslyHandler(ITicketsRepository repository) : ICommandHandler<CreateTicketSynchronously>
{
    public async Task HandleAsync(CreateTicketSynchronously command, CancellationToken cancellationToken = default)
    {
        var (id, name, email, title, description, translatedDescription, category, languageCode, _) = command;
        
        var categoryValid = Enum.TryParse<TicketCategory>(category, out var categoryParsed);
        if (categoryValid is false)
        {
            categoryParsed = TicketCategory.Other;
        }
        
        var ticket = new Ticket(id, name, email, title, description, categoryParsed, languageCode);
        ticket.SetTranslation(translatedDescription);
        
        await repository.AddAsync(ticket, cancellationToken);
    }
}