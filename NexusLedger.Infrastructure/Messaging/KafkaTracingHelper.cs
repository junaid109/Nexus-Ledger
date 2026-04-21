using System.Diagnostics;
using System.Text;
using Confluent.Kafka;

namespace NexusLedger.Infrastructure.Messaging;

public static class KafkaTracingHelper
{
    public static void InjectTraceHeaders(Headers headers)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            headers.Add("traceparent", Encoding.UTF8.GetBytes(activity.Id ?? ""));
            if (activity.TraceStateString != null)
            {
                headers.Add("tracestate", Encoding.UTF8.GetBytes(activity.TraceStateString));
            }
        }
    }

    public static ActivityContext ExtractTraceContext(Headers headers)
    {
        var traceparentHeader = headers.FirstOrDefault(h => h.Key == "traceparent");
        if (traceparentHeader != null)
        {
            var traceparent = Encoding.UTF8.GetString(traceparentHeader.GetValueBytes());
            if (ActivityContext.TryParse(traceparent, null, out var context))
            {
                return context;
            }
        }
        return default;
    }
}
