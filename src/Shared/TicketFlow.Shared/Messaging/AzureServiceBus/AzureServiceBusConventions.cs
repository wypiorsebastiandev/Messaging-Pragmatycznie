namespace TicketFlow.Shared.Messaging.AzureServiceBus;

public static class AzureServiceBusConventions
{
    private const string Delimiter = "||";
    
    public static string ForTopicAndSubscription(string topic, string subscription) => $"{topic}{Delimiter}{subscription}";

    public static bool TryGetTopicAndSubscription(string topicAndSubscription, out string topic, out string subscription)
    {
        var parts = topicAndSubscription.Split(Delimiter);
        topic = string.Empty;
        subscription = string.Empty;
        
        if (parts.Length == 2)
        {
            topic = parts[0];
            subscription = parts[1];
            return true;
        }
        
        return false;
    }
}