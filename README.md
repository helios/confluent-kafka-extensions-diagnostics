# HeliosOpenTelemetry.Kafka.Confluent

![GitHub Actions Badge](https://github.com/helios/confluent-kafka-extensions-diagnostics/actions/workflows/continuous.integration.yml/badge.svg)
[![NuGet Badge](https://buildstats.info/nuget/HeliosOpenTelemetry.Kafka.Confluent)](https://www.nuget.org/packages/HeliosOpenTelemetry.Kafka.Confluent/)

The `HeliosOpenTelemetry.Kafka.Confluent` package enables instrumentation of the `Confluent.Kafka` library
via [Activity API](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs).

## Installation

```powershell
Install-Package HeliosOpenTelemetry.Kafka.Confluent
```

## Usage

### Producer

Producer instrumentation is done via wrapper class and, for this reason, the producer usage is not needed to be rewritten. However,
to enable producer instrumentation, `BuildWithInstrumentation` method should be called on the producer builder instead of `Build`.
After that, all produce calls (sync and async) will be instrumented.

```csharp
using Confluent.Kafka;
using HeliosOpenTelemetry.Kafka.Confluent;


using var producer =
    new ProducerBuilder<Null, string>(new ProducerConfig(new ClientConfig { BootstrapServers = "localhost:9092" }))
        .SetKeySerializer(Serializers.Null)
        .SetValueSerializer(Serializers.Utf8)
        .BuildWithInstrumentation();

await producer.ProduceAsync("topic", new Message<Null, string> { Value = "Hello World!" });

```

### Consumer

Unfortunately, consumer interface of `Confluent.Kafka` library is not very flexible. Therefore, the instrumentation is implemented
via an extension method on the consumer itself. For this reason, the consumer usage should be rewritten as follows:

```csharp
using Confluent.Kafka;
using HeliosOpenTelemetry.Kafka.Confluent;

using var consumer = new ConsumerBuilder<Ignore, string>(
        new ConsumerConfig(new ClientConfig { BootstrapServers = "localhost:9092" })
        {
            GroupId = "group", AutoOffsetReset = AutoOffsetReset.Earliest
        })
    .SetValueDeserializer(Deserializers.Utf8)
    .Build();

consumer.Subscribe("topic");

try
{
    while (true)
    {
        try
        {
            consumer.ConsumeWithInstrumentation((result) =>
            {
                Console.WriteLine(result.Message.Value);
            });
        }
        catch (ConsumeException e)
        {
            Console.WriteLine($"Error occured: {e.Error.Reason}");
        }
    }
}
catch (OperationCanceledException)
{
    consumer.Close();
}
```
