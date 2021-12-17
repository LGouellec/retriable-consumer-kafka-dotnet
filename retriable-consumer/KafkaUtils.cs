using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace retriable_consumer
{
    public class KafkaUtils
    {
        public static async Task CreateTopic(IAdminClient adminClient, string topicName, int partitionNumber)
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic.Equals(topicName));
            if (topicMetadata == null)
            {
                try
                {
                    var t = new TopicSpecification()
                    {
                        Name = topicName,
                        NumPartitions = partitionNumber,
                        ReplicationFactor = 1
                    };
                    
                    await adminClient.CreateTopicsAsync(new List<TopicSpecification>() {t});
                    
                }catch(Exception e){ 
                    // silent ignore if topic exist
                }
            }
        }
    }
}