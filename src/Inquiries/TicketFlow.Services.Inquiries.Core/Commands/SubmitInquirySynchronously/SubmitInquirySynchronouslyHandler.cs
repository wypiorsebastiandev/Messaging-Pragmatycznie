using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.Inquiries.Core.Commands.SubmitInquiry;
using TicketFlow.Services.Inquiries.Core.Data.Models;
using TicketFlow.Services.Inquiries.Core.Data.Repositories;
using TicketFlow.Services.Inquiries.Core.LanguageDetection;
using TicketFlow.Services.Inquiries.Core.Messaging.Publishing;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Commands.SubmitInquirySynchronously;

public class SubmitInquirySynchronouslyHandler(
    IInquiriesRepository repository, 
    ILanguageDetector languageDetector, 
    IHttpClientFactory httpClientFactory, 
    ILogger<SubmitInquirySynchronouslyHandler> logger) : ICommandHandler<SubmitInquirySynchronously>
{
    private const string EnglishLanguageCode = "en";
    
    public async Task HandleAsync(SubmitInquirySynchronously command, CancellationToken cancellationToken = default)
    {
        var (name, email, title, description, category) = command;
        
        var httpClient = httpClientFactory.CreateClient();
        var categoryParsed = ParseCategory(category);
        var inquiry = new Inquiry(name, email, title, description, categoryParsed);
        
        await repository.AddAsync(inquiry, cancellationToken);
        
        var languageCode = await languageDetector.GetTextLanguageCode(inquiry.Description, cancellationToken);
        var translatedDescription = string.Empty;
        
        if (languageCode is not EnglishLanguageCode)
        {
            logger.LogInformation($"Translation for inquiry with id: {inquiry.Id} has been requested.");

            var translationResponse = await httpClient.PostAsJsonAsync("http://localhost:5274/translations", new {Text = inquiry.Description}, cancellationToken);
            translationResponse.EnsureSuccessStatusCode();

            translatedDescription = await translationResponse.Content.ReadFromJsonAsync<string>(cancellationToken);
        }
        
        var inquiryReportedMessage = new CreateTicketSynchronously(
            inquiry.Id,
            inquiry.Name,
            inquiry.Email,
            inquiry.Title,
            inquiry.Description,
            translatedDescription,
            inquiry.Category.ToString(),
            languageCode,
            inquiry.CreatedAt);
        
        var ticketsResponse = await httpClient.PostAsJsonAsync("http://localhost:5112/tickets", inquiryReportedMessage, cancellationToken);
        ticketsResponse.EnsureSuccessStatusCode();
    }

    private static InquiryCategory ParseCategory(string category)
    {
        CapitalizeInput();
        var parseSucceeded = Enum.TryParse<InquiryCategory>(category, out var categoryParsed);
        
        if (!parseSucceeded)
        {
            categoryParsed = InquiryCategory.Other;
        }

        return categoryParsed;

        void CapitalizeInput()
        {
            category = category[0].ToString().ToUpper() + category.Substring(1, category.Length - 1);
        }
    }
}