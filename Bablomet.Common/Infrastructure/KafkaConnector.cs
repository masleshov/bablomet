using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Bablomet.Common.Infrastructure;

public class KafkaConnector : IDisposable
{
    private static readonly string ConnectionString = $"{EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_HOST)}:{EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_PORT)}";
    private readonly IProducer<Null, string> _producer;
    private readonly IConsumer<Null, string> _consumer;

    private readonly Dictionary<string, BufferBlock<string>> _topicCallbackQueues;

    public KafkaConnector()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = ConnectionString
        };
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = ConnectionString,
            GroupId = EnvironmentGetter.GetVariable(EnvironmentVariables.HOST),
            EnableAutoCommit = true,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Latest
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        _consumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();

        _topicCallbackQueues = new Dictionary<string, BufferBlock<string>>();
    }

    public static async Task CreateTopics(params string[] topics)
    {
        if (topics == null || topics.Length == 0) return;

        topics = topics.Select(t => t.Replace("+", "--")).ToArray();
        
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = ConnectionString }).Build();
        var existing = adminClient.GetMetadata(TimeSpan.FromSeconds(10)).Topics
            .Select(topic => topic.Topic)
            .ToHashSet();
        var toCreate = topics.Where(topic => !existing.Contains(topic)).ToArray();
        foreach (var topic in toCreate)
        {
            try
            {
                await adminClient.CreateTopicsAsync(new[]
                {
                    new TopicSpecification
                    {
                        Name = topic,
                        ReplicationFactor = 1,
                        NumPartitions = 1
                    }
                });
            }
            catch (CreateTopicsException e)
            {
                // Console.WriteLine($"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }
    }

    private static async Task CreateTopicIfNotExists(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentNullException(nameof(topic));

        topic = topic.Replace("+", "--");

        using (var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = ConnectionString }).Build())
        {
            try
            {
                await adminClient.CreateTopicsAsync(new TopicSpecification[] { 
                    new TopicSpecification { Name = topic, ReplicationFactor = 1, NumPartitions = 1 } });
            }
            catch (CreateTopicsException e)
            {
                // Console.WriteLine($"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }
    }

    public async Task Send(string topic, string message)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentNullException(nameof(topic));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

        await CreateTopicIfNotExists(topic);
        await _producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }

    public void StartListen(Dictionary<string, Func<string, Task>> topicCallbacks, CancellationToken token)
    {
        Task.Run(async () =>
        {
            if (topicCallbacks == null || topicCallbacks.Count == 0) return;

            var topics = topicCallbacks.Keys
                .Select(t => t.Replace("+", "--"))
                .ToArray();

            await CreateTopics(topics);
            _consumer.Subscribe(topics);

            foreach (var topicCallback in topicCallbacks)
            {
                var topic = topicCallback.Key.Replace("+", "--");
                if (_topicCallbackQueues.TryGetValue(topic, out var queue))
                {
                    continue;
                }

                _topicCallbackQueues[topic] = queue = new BufferBlock<string>();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        var message = await queue.ReceiveAsync(token);

                        try
                        {
                            await topicCallback.Value(message);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace ?? string.Empty);
                        }
                    }
                }, token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            while (!token.IsCancellationRequested)
            {
                var result = _consumer.Consume(token);
                if (!_topicCallbackQueues.TryGetValue(result.Topic, out var queue))
                {
                    throw new NullReferenceException($"No callback queue found for the topic {result.Topic}");
                }

                await queue.SendAsync(result.Message.Value, token);
            }
        }, token);
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
        
        _consumer.Unsubscribe();
        _consumer.Close();
        _consumer.Dispose();
    }
}