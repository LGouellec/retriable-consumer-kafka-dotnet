using System.Collections.Generic;
using Confluent.Kafka;

namespace retriable_consumer
{
    public class RetriableConsumerRebalanceListener
    {
        private readonly IDictionary<TopicPartition, long> _offsets;

        public RetriableConsumerRebalanceListener(IDictionary<TopicPartition,long> offsets)
        {
            _offsets = offsets;
        }

        public void PartitionAssigned(IConsumer<string, string> consumer, List<TopicPartition> topicPartitions)
        {
            throw new System.NotImplementedException();
        }

        public void PartitionRevoked(IConsumer<string, string> consumer, List<TopicPartitionOffset> topicPartitionOffsets)
        {
            throw new System.NotImplementedException();
        }

        public void PartitionLost(IConsumer<string, string> consumer, List<TopicPartitionOffset> topicPartitionOffsets)
        {
            throw new System.NotImplementedException();
        }
    }
}