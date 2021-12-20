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
            foreach (var tp in topicPartitions)
            {
                var offset = consumer.Position(tp);
                if(offset != Offset.Unset)
                    _offsets.Add(tp, offset.Value);
            }
        }

        public void PartitionRevoked(IConsumer<string, string> consumer, List<TopicPartitionOffset> topicPartitionOffsets)
        {
            foreach (var tp in topicPartitionOffsets)
                _offsets.Remove(tp.TopicPartition);
        }

        public void PartitionLost(IConsumer<string, string> consumer, List<TopicPartitionOffset> topicPartitionOffsets)
        {
            foreach (var tp in topicPartitionOffsets)
                _offsets.Remove(tp.TopicPartition);
        }
    }
}