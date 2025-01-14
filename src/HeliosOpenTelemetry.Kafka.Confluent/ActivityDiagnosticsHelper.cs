using Confluent.Kafka;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HeliosOpenTelemetry.Kafka.Confluent;

internal static class ActivityDiagnosticsHelper
{
    private const string ActivitySourceName = "HeliosOpenTelemetry.Kafka.Confluent";
    private const string TraceParentHeaderName = "traceparent";
    private const string TraceStateHeaderName = "tracestate";

    private static ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    internal static Activity? StartProduceActivity<TKey, TValue>(TopicPartition partition,
        Message<TKey, TValue> message, bool metadataOnly)
    {
        try
        {
            Activity? activity = ActivitySource.StartActivity("Confluent.Kafka.Produce", ActivityKind.Producer,
                default(ActivityContext), ActivityTags(partition)!);

            if (activity == null)
                return null;

            SetActivityTags(activity, message, metadataOnly);

            if (message.Headers == null)
            {
                message.Headers = new Headers();
            }

            if (activity.Id != null)
                message.Headers.Add(TraceParentHeaderName, Encoding.UTF8.GetBytes(activity.Id));

            var tracestateStr = activity.Context.TraceState;
            if (tracestateStr?.Length > 0)
            {
                message.Headers.Add(TraceStateHeaderName, Encoding.UTF8.GetBytes(tracestateStr));
            }

            return activity;
        }
        catch
        {
            // ignore
            return null;
        }
    }

    internal static Activity? StartConsumeActivity<TKey, TValue>(TopicPartition partition,
        Message<TKey, TValue> message, bool metadataOnly)
    {
        try
        {
            var activity = ActivitySource.CreateActivity("Confluent.Kafka.Consume", ActivityKind.Consumer,
                default(ActivityContext), ActivityTags(partition)!);

            if (activity != null)
            {
                var traceParentHeader = message.Headers?.FirstOrDefault(x => x.Key == TraceParentHeaderName);
                var traceStateHeader = message.Headers?.FirstOrDefault(x => x.Key == TraceStateHeaderName);

                var traceParent = traceParentHeader != null
                    ? Encoding.UTF8.GetString(traceParentHeader.GetValueBytes())
                    : null;
                var traceState = traceStateHeader != null
                    ? Encoding.UTF8.GetString(traceStateHeader.GetValueBytes())
                    : null;

                if (ActivityContext.TryParse(traceParent, traceState, out var activityContext))
                {
                    activity.SetParentId(activityContext.TraceId, activityContext.SpanId, activityContext.TraceFlags);
                    activity.TraceStateString = activityContext.TraceState;
                }

                SetActivityTags(activity, message, metadataOnly);
                activity.Start();
            }


            return activity;
        }
        catch
        {
            // ignore
            return null;
        }
    }

    private static void SetActivityTags<TKey, TValue>(Activity activity, Message<TKey, TValue> message, bool metadataOnly)
    {
        if (message.Key != null)
        {
            activity.SetTag("messaging.kafka.message_key", message.Key.ToString());
        }

        if (metadataOnly || (Environment.GetEnvironmentVariable("HS_METADATA_ONLY")?.Equals("true") ?? false))
        {
            return;
        }

        if (message.Value != null)
        {
            activity.SetTag("messaging.payload", message.Value.ToString());
        }

        var headers = message.Headers;
        if (headers != null)
        {
            var headersDict = headers.ToDictionary(x => x.Key, x => System.Text.Encoding.Default.GetString(x.GetValueBytes()));
            activity.SetTag("messaging.kafka.headers", JsonSerializer.Serialize(headersDict));
        }
    }

    private static IEnumerable<KeyValuePair<string, object>> ActivityTags(TopicPartition partition)
    {
        return new[]
        {
            new KeyValuePair<string, object>("messaging.system", "kafka"),
            new KeyValuePair<string, object>("messaging.destination", partition.Topic),
            new KeyValuePair<string, object>("messaging.destination_kind", "topic"), new KeyValuePair<string, object>(
                "messaging.kafka.partition",
                partition.Partition.ToString())
        };
    }
}
