namespace TicketFlow.Shared.Messaging.AzureServiceBus;

public class AzureServiceBusOptions
{
    public string ConnectionString { get; init; }

    public bool CreateTopology { get; init; } = true;
}