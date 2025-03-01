using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketFlow.Services.Inquiries.Core.Data.Models;
using TicketFlow.Services.Inquiries.Core.Messaging.Publishing;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Serialization;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", false, false);

builder.Services
    .AddApp(builder.Configuration)
    .AddMessaging(builder.Configuration, x => x
        .UseRabbitMq()
        .UseResiliency())
    .AddSerialization();

var app = builder.Build();

var publisher = app.Services.GetService<IMessagePublisher>();
foreach (var number in Enumerable.Range(1, 1000))
{
    await publisher!.PublishAsync(new InquirySubmitted(
        Guid.NewGuid(),
        $"Name{number}",
        $"Email{number}",
        $"Title{number}",
        $"Message{number}",
        InquiryCategory.General.ToString(),
        "en",
        DateTimeOffset.UtcNow));
}

Console.WriteLine("Done publishing! Press any key to exit...");
Console.ReadKey();