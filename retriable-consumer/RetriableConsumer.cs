using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Confluent.Kafka;

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
        public string ExternalServiceUrl { get; }
        public int CommitInterval { get; }

        private readonly ExternalService service;
        private Thread thread;
        private CancellationTokenSource source = new ();
        private IConsumer<string, string> consumer;
        private IDictionary<TopicPartition, long> offsets = new Dictionary<TopicPartition, long>();
        private bool isPaused = false;

        public RetriableConsumer(string bootstrap, string topicName, int numberRetry, string groupId, string reset,
            int maxPollInterval, string externalServiceUrl, int commitInterval)
        {
            Bootstrap = bootstrap;
            TopicName = topicName;
            NumberRetry = numberRetry;
            GroupId = groupId;
            Reset = reset;
            MaxPollInterval = maxPollInterval;
            ExternalServiceUrl = externalServiceUrl;
            CommitInterval = commitInterval;
        }

        public void Start()
        {
            ConsumerConfig config = new ConsumerConfig();
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
        }

        public void Stop()
        {
            source.Cancel();
            
            thread.Join();
        }

        private void Run()
        {
            long timeoutMs = 1000;
            consumer.Subscribe(TopicName);
            
            while (!source.IsCancellationRequested)
            {
                // TODO : commit with interval ms
                var record = consumer.Consume(TimeSpan.FromMilliseconds(timeoutMs));
                if(isPaused)
                    consumer.Resume(consumer.Assignment);
                
                if (record != null)
                {
                    int retries = 0;
                    bool messageDelivered = false;
                    do
                    {
                        try
                        {
                            service.Call();
                            messageDelivered = true;
                        }
                        catch (Exception e)
                        {
                            ++retries;
                            if (NumberRetry < 0) // infinite retry
                            {
                                consumer.Pause(consumer.Assignment);
                                isPaused = true;
                                Rewind(consumer);
                            }
                        }
                    } while (!messageDelivered && retries <= NumberRetry);
                    
                    if (NumberRetry > 0) // all except infinite retry
                        UpdateOffsetPerPartition(record);
                }
            }
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
        }
    }
}