using Confluent.Kafka;

namespace HeliosOpenTelemetry.Kafka.Confluent;

/// <summary>
///     Extension methods for <see cref="IConsumer{TKey,TValue}" />.
/// </summary>
public static class ConsumerExtensions
{
    /// <summary>
    ///     Consumes a message from the topic with instrumentation.
    /// </summary>
    public static async Task ConsumeWithInstrumentation<TKey, TValue>(this IConsumer<TKey, TValue> consumer,
        Func<ConsumeResult<TKey, TValue>?, CancellationToken, Task> action, CancellationToken cancellationToken, bool metadataOnly = false)
    {
        if (consumer == null) throw new ArgumentNullException(nameof(consumer));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = consumer.Consume(cancellationToken);

        var activity = ActivityDiagnosticsHelper.StartConsumeActivity(result.TopicPartition, result.Message, metadataOnly);

        try
        {
            await action(result, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            activity?.Stop();
        }
    }

    /// <summary>
    ///     Consumes a message from the topic with instrumentation.
    /// </summary>
    public static async Task<TResult> ConsumeWithInstrumentation<TKey, TValue, TResult>(
        this IConsumer<TKey, TValue> consumer,
        Func<ConsumeResult<TKey, TValue>?, CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken, bool metadataOnly = false)
    {
        if (consumer == null) throw new ArgumentNullException(nameof(consumer));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = consumer.Consume(cancellationToken);

        var activity = ActivityDiagnosticsHelper.StartConsumeActivity(result.TopicPartition, result.Message, metadataOnly);

        try
        {
            return await action(result, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            activity?.Stop();
        }
    }

    /// <summary>
    ///     Consumes a message from the topic with instrumentation.
    /// </summary>
    public static void ConsumeWithInstrumentation<TKey, TValue>(this IConsumer<TKey, TValue> consumer,
        Action<ConsumeResult<TKey, TValue>?> action, int millisecondsTimeout, bool metadataOnly = false)
    {
        if (consumer == null) throw new ArgumentNullException(nameof(consumer));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = consumer.Consume(millisecondsTimeout);

        var activity = ActivityDiagnosticsHelper.StartConsumeActivity(result.TopicPartition, result.Message, metadataOnly);

        try
        {
            action(result);
        }
        finally
        {
            activity?.Stop();
        }
    }
}
