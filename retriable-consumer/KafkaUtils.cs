using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace retriable_consumer
{
    public class KafkaUtils
    {
        public static void CreateTopic(IAdminClient adminClient, string topicName, int partitionNumber)
        {
            Metadata metadata = null;
            TopicMetadata topicMetadata = null;
            int maxRetry = 100, i = 0;
            while (i < maxRetry && topicMetadata == null)
            {
                try
                {
                    metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                    topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic.Equals(topicName));
                    
                    var t = new TopicSpecification()
                    {
                        Name = topicName,
                        NumPartitions = partitionNumber,
                        ReplicationFactor = 1
                    };

                    adminClient.CreateTopicsAsync(new List<TopicSpecification>() {t})
                        .GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    // silent ignore if topic exist
                    ++i;
                    Thread.Sleep(1000);
                } 
            }
        }
    }
}