using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Inquiries.Core.Data;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Inquiries.Core.Queries;

public class ListInquiriesHandler : IQueryHandler<ListInquiries, InquiriesListDto>
{
    private readonly InquiriesDbContext _dbContext;

    public ListInquiriesHandler(InquiriesDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    
    public async Task<InquiriesListDto> HandleAsync(ListInquiries query, CancellationToken cancellationToken = default)
    {
        var (page, limit) = query;
        if (limit > 25)
        {
            limit = 25;
        }

        var count = await _dbContext.Inquiries.CountAsync(cancellationToken);

        var data = await _dbContext.Inquiries
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);
        

        return new InquiriesListDto(
            Data: data.Select(x => new InquiriesListEntryDto(
                x.Id.ToString(),
                x.Name,
                x.Title,
                x.Email,
                x.Description,
                x.Category,
                x.Status,
                x.CreatedAt.ToString("O"),
                x.TicketId?.ToString())
            ).ToList(),
            TotalCount: count);
    }
}