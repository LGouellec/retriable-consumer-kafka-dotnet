using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace retriable_consumer
{
    public class RetriableConsumer
    {
        public string Bootstrap { get; }
        public string TopicName { get; }
        public int NumberRetry { get; }
        public string GroupId { get; }
        public string Reset { get; }
        public int MaxPollInterval { get; }
        public int CommitInterval { get; }

        private readonly ExternalService service;
        private Thread thread;
        private CancellationTokenSource source = new ();
        private IConsumer<string, string> consumer;
        private IDictionary<TopicPartition, long> offsets = new Dictionary<TopicPartition, long>();
        private bool isPaused = false;
        private readonly ILogger logger;
        private ConsumerConfig config;

        public RetriableConsumer(string bootstrap, string topicName, int numberRetry, string groupId, string reset,
            int maxPollInterval, string externalServiceUrl, int commitInterval, int durationSleepIntervalMs,
            bool simulateExternalService, int percentageFailure)
        {
            Bootstrap = bootstrap;
            TopicName = topicName;
            NumberRetry = numberRetry;
            GroupId = groupId;
            Reset = reset;
            MaxPollInterval = maxPollInterval;
            CommitInterval = commitInterval;
            logger = Program.LoggerFactory.CreateLogger<RetriableConsumer>();
            service = new ExternalService(externalServiceUrl, durationSleepIntervalMs, simulateExternalService, percentageFailure);
        }

        public void Start()
        {
            config = new ConsumerConfig();
            config.BootstrapServers = Bootstrap;
            config.GroupId = GroupId;
            config.MaxPollIntervalMs = MaxPollInterval;
            config.AutoOffsetReset =
                Reset.ToUpper().Equals("EARLIEST") ? AutoOffsetReset.Earliest : AutoOffsetReset.Latest;
            config.EnableAutoCommit = false;
            config.EnableAutoOffsetStore = false;

            RetriableConsumerRebalanceListener consumerRebalanceListener =
                new RetriableConsumerRebalanceListener(offsets);
            ConsumerBuilder<string, string> consumerBuilder = new ConsumerBuilder<string, string>(config);
            consumerBuilder.SetPartitionsAssignedHandler(consumerRebalanceListener.PartitionAssigned);
            consumerBuilder.SetPartitionsRevokedHandler(consumerRebalanceListener.PartitionRevoked);
            consumerBuilder.SetPartitionsLostHandler(consumerRebalanceListener.PartitionLost);
            
            consumer = consumerBuilder.Build();
            
            thread = new Thread(Run);
            thread.Start();
        }

        public void Stop()
        {
            logger.LogInformation("Stopping retriable consumer ...");
            source.Cancel();
            thread.Join();
            logger.LogInformation("Retriable consumer is stopped !");
        }

        private void Run()
        {
            long timeoutMs = 250;
            DateTime lastCommit = DateTime.Now;
            consumer.Subscribe(TopicName);
            logger.LogInformation($"Subscribe to {TopicName} ....");

            while (!source.IsCancellationRequested)
            {
                if (lastCommit.Add(TimeSpan.FromMilliseconds(CommitInterval)) < DateTime.Now)
                {
                    try
                    {
                        consumer.Commit(offsets.Select(c => new TopicPartitionOffset(c.Key, c.Value)));
                        lastCommit = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error committing offsets {e.Message}");
                        RecreateConsumer();
                    }
                }

                var record = consumer.Consume(TimeSpan.FromMilliseconds(timeoutMs));
                if (record != null)
                    logger.LogInformation(
                        $"Received offset {record.Offset} from topic/partition {record.TopicPartition}, key = {record.Message.Key}, value = {record.Message.Value}");

                if (isPaused)
                {
                    consumer.Resume(consumer.Assignment);
                    logger.LogInformation(
                        $"Resume consumer with assignment {string.Join(",", consumer.Assignment)}");
                    isPaused = false;
                }

                if (record != null)
                {
                    int retries = 0;
                    bool messageDelivered = false;
                    do
                    {
                        try
                        {
                            service.Call(record);
                            logger.LogInformation(
                                $"Message offset {record.Offset} from topic/partition {record.TopicPartition} is delivered");
                            messageDelivered = true;
                        }
                        catch (Exception e)
                        {
                            StringBuilder sb = new();
                            sb.Append(
                                $"Message offset {record.Offset} from topic/partition {record.TopicPartition} is not delivered. ");

                            ++retries;
                            if (NumberRetry < 0) // infinite retry
                            {
                                consumer.Pause(consumer.Assignment);
                                isPaused = true;
                                Rewind(consumer);
                                sb.Append(
                                    $"Consumer is paused assignment, rewind their offsets and will be retry in some milliseconds.");
                            }
                            else if (NumberRetry == 0)
                            {
                                sb.Append($"Consumer is configured to never retry. So message won't be delivered.");
                            }
                            else
                            {
                                sb.Append(
                                    $"Consumer is configured with a max retry {NumberRetry}. Retrying deliver this message ... Retry n° {retries} !");
                            }

                            logger.LogWarning(sb.ToString());

                        }
                    } while (!messageDelivered && retries <= NumberRetry);

                    if (NumberRetry >= 0 || !isPaused)
                        UpdateOffsetPerPartition(record);
                }
            }

            consumer.Unsubscribe();
            consumer.Close();
            consumer.Dispose();
        }

        private void Rewind(IConsumer<string, string> consumer)
        {
            if (!offsets.Any())
            {
                if (Reset.ToUpper().Equals("EARLIEST"))
                    SeekConsumer(consumer, Offset.Beginning);
                else
                    SeekConsumer(consumer, Offset.End);
            }
            else
            {
                foreach(var kv in offsets)
                    consumer.Seek(new TopicPartitionOffset(kv.Key, new Offset(kv.Value)));
            }
        }

        private void SeekConsumer(IConsumer<string, string> consumer, Offset offset)
        {
            foreach(var tp in consumer.Assignment)
                consumer.Seek(new TopicPartitionOffset(tp, offset));
        }

        private void UpdateOffsetPerPartition(ConsumeResult<string, string> @record)
        {
            if (offsets.ContainsKey(record.TopicPartition))
                offsets[record.TopicPartition] = record.Offset + 1;
            else
                offsets.Add(record.TopicPartition, record.Offset +1 );
            
            logger.LogDebug($"Update offset/partition in metadata hashmap [{record.Offset},{record.TopicPartition}]");
        }

        private void RecreateConsumer()
        {
            consumer.Close();
            consumer.Dispose();
            
            RetriableConsumerRebalanceListener consumerRebalanceListener =
                new RetriableConsumerRebalanceListener(offsets);
            ConsumerBuilder<string, string> consumerBuilder = new ConsumerBuilder<string, string>(config);
            consumerBuilder.SetPartitionsAssignedHandler(consumerRebalanceListener.PartitionAssigned);
            consumerBuilder.SetPartitionsRevokedHandler(consumerRebalanceListener.PartitionRevoked);
            consumerBuilder.SetPartitionsLostHandler(consumerRebalanceListener.PartitionLost);
                        
            consumer = consumerBuilder.Build();
                        
            consumer.Subscribe(TopicName);
        }
    }
}