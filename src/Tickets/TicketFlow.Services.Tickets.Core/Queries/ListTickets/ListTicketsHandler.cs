using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Tickets.Core.Data;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.ListTickets;

public class ListTicketsHandler : IQueryHandler<ListTicketsQuery, TicketsListDto>
{
    private readonly TicketsDbContext _dbContext;

    public ListTicketsHandler(TicketsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<TicketsListDto> HandleAsync(ListTicketsQuery query, CancellationToken cancellationToken = default)
    {
        var (agentId, status, page, limit) = query;

        var dbQuery = _dbContext.Tickets.AsQueryable();

        if (query.AgentId is not null)
        {
            dbQuery = dbQuery.Where(x => x.AssignedTo.Equals(query.AgentId));
        }

        dbQuery = dbQuery.OrderByDescending(x => x.CreatedAt);

        var total = await dbQuery.CountAsync(cancellationToken);
        var data = await dbQuery
            .Select(x => new TicketsListEntryDto(
                x.Id.ToString(),
                x.Name,
                x.Email,
                x.Title,
                x.Description,
                x.TranslatedDescription,
                x.Category,
                x.Status,
                x.CreatedAt,
                x.Severity,
                x.AssignedTo,
                x.LanguageCode,
                x.Type,
                x.DeadlineUtc,
                x.Resolution))
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new(data, total);
    }
}