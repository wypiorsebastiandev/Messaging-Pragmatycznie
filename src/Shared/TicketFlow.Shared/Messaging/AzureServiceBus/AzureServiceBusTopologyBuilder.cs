using Azure;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Messaging.Partitioning;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Shared.Messaging.AzureServiceBus;

internal class AzureServiceBusTopologyBuilder(
    AzureServiceBusOptions options,
    ILogger<AzureServiceBusTopologyBuilder> logger,
    ResiliencyOptions resiliencyOptions) : ITopologyBuilder
{
    public async Task CreateTopologyAsync(
        string publisherSource,
        string consumerDestination,
        TopologyType topologyType,
        string filter = "",
        Dictionary<string, object>? consumerCustomArgs = default,
        PartitioningOptions? partitioningOptions = default,
        CancellationToken cancellationToken = default)
    {
        var administrationClient = new ServiceBusAdministrationClient(options.ConnectionString);

        consumerCustomArgs ??= new Dictionary<string, object>();

        switch (topologyType)
        {
            case TopologyType.Direct:
                await CreateDirect(publisherSource, consumerDestination, filter, consumerCustomArgs,
                    administrationClient);
                break;
            case TopologyType.PublishSubscribe:
                if (partitioningOptions == null)
                {
                    await CreatePubSub(publisherSource, consumerDestination, filter, consumerCustomArgs,
                        administrationClient);
                }
                else
                {
                    await CreatePubSubWithPartitioning(publisherSource, consumerDestination, filter, consumerCustomArgs,
                        administrationClient, partitioningOptions);
                }

                break;
            case TopologyType.PublisherToPublisher:
                await CreatePubToPub(publisherSource, consumerDestination, filter, administrationClient);
                break;
            default:
                throw new NotImplementedException($"{nameof(topologyType)} is not supported!");
        }
    }

    private async Task CreateDirect(
        string publisherSource,
        string consumerDestination,
        string filter,
        Dictionary<string, object> consumerCustomArgs,
        ServiceBusAdministrationClient administrationClient)
    {
        if (string.IsNullOrEmpty(publisherSource)) // simple queue
        {
            await administrationClient.CreateQueueIfNotExistsAsync(
                consumerDestination,
                CancellationToken.None);
        }
        else // topic with subscription
        {
            await administrationClient.CreateTopicIfNotExistsAsync(publisherSource, CancellationToken.None);

            if (!string.IsNullOrEmpty(filter))
            {
                var correlationFilter = new CorrelationRuleFilter();
                correlationFilter.ApplicationProperties[AzureServiceBusMessagePublisher.RoutingKeyPropertyName] =
                    filter;

                await administrationClient.CreateSubscriptionIfNotExistsAsync(
                    new CreateSubscriptionOptions(publisherSource, consumerDestination)
                    {
                        MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                        AutoDeleteOnIdle = consumerCustomArgs.ContainsKey("x-expires") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                    },
                    new CreateRuleOptions("RoutingKeyFilter", correlationFilter));
            }
            else
            {
                await administrationClient.CreateSubscriptionIfNotExistsAsync(
                    new CreateSubscriptionOptions(publisherSource, consumerDestination)
                    {
                        MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                        AutoDeleteOnIdle = consumerCustomArgs.ContainsKey("x-expires") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                    });
            }
        }
    }

    private async Task CreatePubSub(
        string publisherSource,
        string consumerDestination,
        string filter,
        Dictionary<string, object> consumerCustomArgs,
        ServiceBusAdministrationClient administrationClient)
    {
        await administrationClient.CreateTopicIfNotExistsAsync(publisherSource, CancellationToken.None);

        if (string.IsNullOrEmpty(consumerDestination))
        {
            return;
        }
        
        if (!string.IsNullOrEmpty(filter))
        {
            var correlationFilter = new CorrelationRuleFilter();
            correlationFilter.ApplicationProperties[AzureServiceBusMessagePublisher.RoutingKeyPropertyName] =
                filter;

            await administrationClient.CreateSubscriptionIfNotExistsAsync(
                new CreateSubscriptionOptions(publisherSource, consumerDestination)
                {
                    MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                    AutoDeleteOnIdle = consumerCustomArgs.ContainsKey("x-expires") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                },
                new CreateRuleOptions("RoutingKeyFilter", correlationFilter));
        }
        else
        {
            await administrationClient.CreateSubscriptionIfNotExistsAsync(
                new CreateSubscriptionOptions(publisherSource, consumerDestination)
                {
                    MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                    AutoDeleteOnIdle = consumerCustomArgs.ContainsKey("x-expires") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                });
        }
    }

    private async Task CreatePubSubWithPartitioning(
        string publisherSource,
        string consumerDestination,
        string filter,
        Dictionary<string, object> consumerCustomArgs,
        ServiceBusAdministrationClient administrationClient,
        PartitioningOptions? partitioningOptions = default)
    {
        await administrationClient.CreateTopicIfNotExistsAsync(publisherSource, CancellationToken.None);

        if (string.IsNullOrEmpty(consumerDestination))
        {
            return;
        }

        if (!string.IsNullOrEmpty(filter))
        {
            var correlationFilter = new CorrelationRuleFilter();
            correlationFilter.ApplicationProperties[AzureServiceBusMessagePublisher.RoutingKeyPropertyName] =
                filter;

            await administrationClient.CreateSubscriptionIfNotExistsAsync(
                new CreateSubscriptionOptions(publisherSource, consumerDestination)
                {
                    RequiresSession = true,
                    MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                    AutoDeleteOnIdle = consumerCustomArgs.ContainsKey("x-expires") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                },
                new CreateRuleOptions("RoutingKeyFilter", correlationFilter));
        }
        else
        {
            await administrationClient.CreateSubscriptionIfNotExistsAsync(
                new CreateSubscriptionOptions(publisherSource, consumerDestination)
                {
                    RequiresSession = true,
                    MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                    AutoDeleteOnIdle = consumerCustomArgs.ContainsKey("x-expires") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                });
        }
    }

    private async Task CreatePubToPub(string publisherSource, string consumerDestination, string filter,
        ServiceBusAdministrationClient administrationClient, bool forPartitioning = false)
    {
        await administrationClient.CreateTopicIfNotExistsAsync(publisherSource, CancellationToken.None);
        await administrationClient.CreateTopicIfNotExistsAsync(consumerDestination, CancellationToken.None);

        if (!string.IsNullOrEmpty(filter))
        {
            var correlationFilter = new CorrelationRuleFilter();
            correlationFilter.ApplicationProperties[AzureServiceBusMessagePublisher.RoutingKeyPropertyName] = filter;

            await administrationClient.CreateSubscriptionIfNotExistsAsync(
                new CreateSubscriptionOptions(publisherSource, consumerDestination + "-subscription")
                {
                    MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                    ForwardTo = consumerDestination,
                },
                new CreateRuleOptions("RoutingKeyFilter", correlationFilter));
        }
        else
        {
            await administrationClient.CreateSubscriptionIfNotExistsAsync(
                new CreateSubscriptionOptions(publisherSource, consumerDestination + "-subscription")
                {
                    MaxDeliveryCount = resiliencyOptions.Consumer.BrokerRetriesLimit,
                    ForwardTo = consumerDestination,
                });
        }
    }
}

internal static class AzureServiceBusAdministrationExtensions
{
    public static async Task CreateSubscriptionIfNotExistsAsync(
        this ServiceBusAdministrationClient administrationClient,
        CreateSubscriptionOptions options,
        CreateRuleOptions? ruleOptions = default,
        CancellationToken cancellationToken = default)
    {
        if (await administrationClient.SubscriptionExistsAsync(options.TopicName, options.SubscriptionName,
                cancellationToken))
        {
            return;
        }

        await administrationClient.CreateSubscriptionAsync(options, ruleOptions ?? new CreateRuleOptions(), cancellationToken);
    }

    public static async Task CreateQueueIfNotExistsAsync(
        this ServiceBusAdministrationClient administrationClient,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (await administrationClient.QueueExistsAsync(name, cancellationToken))
        {
            return;
        }
        
        await administrationClient.CreateQueueAsync(name, cancellationToken);
    }

    public static async Task CreateTopicIfNotExistsAsync(
        this ServiceBusAdministrationClient administrationClient,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (await administrationClient.TopicExistsAsync(name, cancellationToken))
        {
            return;
        }
        
        await administrationClient.CreateTopicAsync(new CreateTopicOptions(name)
        {
            SupportOrdering = true
        }, cancellationToken);
    }
    
}