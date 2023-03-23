using Confluent.Kafka;

namespace HeliosOpenTelemetry.Kafka.Confluent;

/// <summary>
///     Extension methods for <see cref="ProducerBuilder{TKey,TValue}" />.
/// </summary>
public static class ProducerBuilderExtensions
{
    /// <summary>
    ///     Builds a new instrumented instance of producer.
    /// </summary>
    public static IProducer<TKey, TValue> BuildWithInstrumentation<TKey, TValue>(
        this ProducerBuilder<TKey, TValue> producerBuilder, bool metadataOnly = false)
    {
        if (producerBuilder == null) throw new ArgumentNullException(nameof(producerBuilder));

        return new InstrumentedProducer<TKey, TValue>(producerBuilder.Build(), metadataOnly);
    }
}
