using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Shared.Messaging.AzureServiceBus;

public static class Extensions
{
    public static IMessagingRegisterer UseAzureServiceBus(this IMessagingRegisterer registerer)
    {
        var services = registerer.Services;
        var section = registerer.Configuration.GetSection("azureServiceBus");
        var appName = registerer.Configuration.GetValue<string>("App:AppName");
        services.Configure<AzureServiceBusOptions>(section);

        var options = new AzureServiceBusOptions();
        section.Bind(options);
        services.AddSingleton(options);

        services.AddSingleton<IMessagePublisher, AzureServiceBusMessagePublisher>();
        services.AddSingleton<IMessageConsumer, AzureServiceBusMessageConsumer>();
        services.AddTransient<ITopologyBuilder, AzureServiceBusTopologyBuilder>();
        services.AddSingleton(provider => new TopologyOptions(
            CreateTopology: provider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value.CreateTopology));

        services.AddSingleton(_ => new ServiceBusClient(options.ConnectionString));
        
        
        return registerer;
    }
}