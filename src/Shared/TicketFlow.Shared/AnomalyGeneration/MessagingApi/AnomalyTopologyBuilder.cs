using Microsoft.Extensions.Options;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Shared.AnomalyGeneration.MessagingApi;

public class AnomalyTopologyBuilder(ITopologyBuilder topologyBuilder, IOptions<AppOptions> appOptions)
{
    public const string AnomaliesTopic = "anomalies-sync";
    public static string AnomaliesAppExclusiveQueuePrefix(AppOptions opts) => $"anomalies-sync-{opts.AppName}-{opts.InstanceId}".Substring(0, 50);
    
    public async Task CreateTopologyAsync(CancellationToken cancellationToken)
    {
        await topologyBuilder.CreateTopologyAsync(
            AnomaliesTopic,
            AnomaliesAppExclusiveQueuePrefix(appOptions.Value),
            TopologyType.PublishSubscribe,
            filter: appOptions.Value.AppName,
            consumerCustomArgs: new Dictionary<string, object>
            {
                { "x-expires", 1000 * 60 * 1 }, // Delete queue if no one is listening after 1min
                { "x-dead-letter-exchange", "" } // No DLQ for anomaly syncs
            },
            cancellationToken: cancellationToken);
    }
}