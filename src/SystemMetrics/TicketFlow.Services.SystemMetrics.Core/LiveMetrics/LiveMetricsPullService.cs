﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.SystemMetrics.Generator.Data;
using TicketFlow.Services.SystemMetrics.Generator.Schedulers;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.AzureServiceBus;

namespace TicketFlow.Services.SystemMetrics.Core.LiveMetrics;

public class LiveMetricsPullService(IMessageConsumer messageConsumer, LiveMetricsHub hub, LiveMetricsOptions opts) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!hub.HasActiveClients)
                {
                    await Task.Delay(opts.PollingIntervalInMs, stoppingToken);
                    continue;
                }

                await messageConsumer.GetMessage<MetricTick>(
                    handle: async msg => await hub.PushOldestMetricTick(msg),
                    AzureServiceBusConventions.ForTopicAndSubscription(
                        RandomMetricsPublisher.MetricsExchange,
                        LiveMetricsHub.LiveMetricsQueue),
                    stoppingToken);

                await Task.Delay(opts.PollingIntervalInMs, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while pulling the message from broker: {0}", ex.Message);
            }
        }
    }
}