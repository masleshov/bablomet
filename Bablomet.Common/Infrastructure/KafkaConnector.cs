using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Bablomet.Common.Infrastructure;

public class KafkaConnector<TKey, TValue> : IDisposable
{
    private static readonly string ConnectionString = $"{EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_HOST)}:{EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_PORT)}";
    protected readonly IProducer<TKey, TValue> Producer;
    protected readonly IConsumer<TKey, TValue> Consumer;

    private readonly Dictionary<string, BufferBlock<Message<TKey, TValue>>> _topicCallbackQueues;

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
        Producer = new ProducerBuilder<TKey, TValue>(producerConfig)
            .SetKeySerializer(new MessagePackKafkaSerializer<TKey>())
            .Build();
        Consumer = new ConsumerBuilder<TKey, TValue>(consumerConfig)
            .SetKeyDeserializer(new MessagePackKafkaSerializer<TKey>())
            .Build();

        _topicCallbackQueues = new Dictionary<string, BufferBlock<Message<TKey, TValue>>>();
    }

    public static async Task CreateTopics(params string[] topics)
    {
        if (topics == null || topics.Length == 0) return;

        topics = topics.Select(t => t.Replace("+", "--")).ToArray();
        
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = ConnectionString }).Build();
        var existing = adminClient.GetMetadata(TimeSpan.FromSeconds(10)).Topics
            .Select(topic => topic.Topic)
            .ToHashSet();
        var toCreate = topics
            .Where(topic => !existing.Contains(topic))
            .Select(topic => new TopicSpecification
            {
                Name = topic,
                // ReplicationFactor = 1,
                // NumPartitions = 5000
            })
            .ToArray();
        foreach (var topic in toCreate)
        {
            try
            {
                await adminClient.CreateTopicsAsync(new[] { topic });
            }
            catch (CreateTopicsException e)
            {
                Console.WriteLine($"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
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

    public async Task Send(string topic, TKey key, TValue message)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentNullException(nameof(topic));
        if (message == null) throw new ArgumentNullException(nameof(message));

        // await CreateTopicIfNotExists(topic);
        await Producer.ProduceAsync(topic, new Message<TKey, TValue> { Key = key, Value = message });
    }

    public void StartListen(Dictionary<string, Func<Message<TKey, TValue>, Task>> topicCallbacks, CancellationToken token)
    {
        if (topicCallbacks == null || topicCallbacks.Count == 0) throw new ArgumentNullException(nameof(topicCallbacks));

        StartInternalListen( topicCallbacks, token);
    }

    protected void StartInternalListen(Dictionary<string, Func<Message<TKey, TValue>, Task>> topicCallbacks, CancellationToken token)
    {

        if (topicCallbacks == null || topicCallbacks.Count == 0) return;

        var topics = topicCallbacks.Keys.ToArray();
        for (int i = 0; i < topics.Length; i++)
        {
            topics[i] = topics[i].Replace("+", "--");
        }

        Task.Run(async () =>
        {
            Consumer.Subscribe(topics);

            foreach (var topic in topics)
            {
                if (_topicCallbackQueues.ContainsKey(topic)) continue;

                var queue = new BufferBlock<Message<TKey, TValue>>();
                _topicCallbackQueues[topic] = queue;

                _ = Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        var message = await queue.ReceiveAsync(token);
                        try
                        {
                            await topicCallbacks[topic](message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace ?? string.Empty);
                        }
                    }
                }, token);
            }

            while (!token.IsCancellationRequested)
            {
                var result = Consumer.Consume(token);
                if (_topicCallbackQueues.TryGetValue(result.Topic, out var queue))
                {
                    await queue.SendAsync(result.Message, token);
                }
            }
        }, token);
    }

    public void Dispose()
    {
        Producer.Flush();
        Producer.Dispose();
        
        Consumer.Unsubscribe();
        Consumer.Close();
        Consumer.Dispose();
    }
}