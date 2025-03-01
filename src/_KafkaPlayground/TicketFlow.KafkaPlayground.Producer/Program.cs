using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketFlow.KafkaPlayground.Shared;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.Serialization;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddKafka(producerConfig: new ProducerConfig
    {
        EnableDeliveryReports = true
    })
    .AddSerialization();

var app = builder.Build();

const string ticketsTopic = "tickets";

var publisher = app.Services.GetService<KafkaPublisher>();
var publishTasks = new List<Task>();

var topologyInitializer = app.Services.GetService<KafkaTopologyInitializer>();
await topologyInitializer!.CreateTopicAsync(ticketsTopic, numberOfPartitions: 3);

foreach (var num in Enumerable.Range(1, 10))
{
    var ticketId = Guid.Parse("00000000-0000-0000-0000-" + num.ToString("D12"));

    #region PublishNonBlocking
    // publisher!.PublishNonBlockingAsync(
    //     message: new TicketCreated(
    //         ticketId,
    //         ticketId,
    //         "ProblematicUser_123",
    //         "ProblematicUser_123@devmentors.io",
    //         "Hello TicketFlow!",
    //         "Guys, there is a problem...",
    //         "General",
    //         "en"),
    //     topic: ticketsTopic,
    //     messageId: ticketId.ToString(),
    //     deliveryHandler: report =>
    //     {
    //         Console.WriteLine($"[DeliveryReport] Key={report.Key}, Value={report.Value}, Error={report.Error}");
    //     });
    #endregion

    #region PublishBlocking
    publishTasks.Add(
        publisher!.PublishBlockingAsync(
        message: new TicketCreated(
            ticketId,
            ticketId,
            "ProblematicUser_123",
            "ProblematicUser_123@devmentors.io",
            "Hello TicketFlow!",
            "Guys, there is a problem...",
            "General",
            "en"),
        topic: ticketsTopic,
        messageId: ticketId.ToString(),
        CancellationToken.None));
    #endregion
    
    Console.WriteLine($"[Published] Key={ticketId}");
}

await Task.WhenAll(publishTasks);

app.Run();