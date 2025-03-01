using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketFlow.KafkaPlayground.Consumer;
using TicketFlow.KafkaPlayground.Shared;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddKafka(consumerConfig: new ConsumerConfig
    {
        GroupId = "consumer-1",
        AutoOffsetReset = AutoOffsetReset.Earliest
    })
    .AddKafkaConsumer<TicketCreated>("tickets")
    .AddTransient<IMessageHandler<TicketCreated>, TicketCreatedKafkaHandler>()
    .AddSerialization();

var app = builder.Build();

Console.WriteLine("Consumer started!");
app.Run();