using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.VisualBasic.CompilerServices;

namespace retriable_consumer
{
    public class Program
    {
        public static readonly string BOOTSTRAP_SERVER_CST = "KAFKA_BOOTSTRAP_SERVER";
        public static readonly string TOPIC_CST = "KAFKA_TOPIC";
        public static readonly string PARTITION_CST = "KAFKA_PARTITION";
        public static readonly string NUMBER_RETRY_CST = "KAFKA_NUMBER_RETRY";
        public static readonly string GROUP_ID_CST = "KAFKA_GROUP_ID";
        public static readonly string RESET_BEHAVIOR = "KAFKA_RESET_STRATEGY";
        public static readonly string MAX_POLL_INTERVAL_CST = "KAFKA_MAX_POLL_INTERVAL_MS";
        public static readonly string EXTERNAL_SERVICE_URL = "KAFKA_EXTERNAL_SERVICE_URL";
        public static readonly string COMMIT_INTERVAL = "KAFKA_COMMIT_INTERVAL_MS";

        
        public static async Task Main(string[] args)
        {
            var topicName = GetVariableOrDefault(TOPIC_CST, "my_topic");
            var partitionNumber = Int32.Parse(GetVariableOrDefault(PARTITION_CST, "1"));
            var bootstrap = GetVariableOrDefault(BOOTSTRAP_SERVER_CST, "localhost:9092");
            var numberRetry = Int32.Parse(GetVariableOrDefault(NUMBER_RETRY_CST, "10"));
            var groupId = GetVariableOrDefault(GROUP_ID_CST, "my_grouo");
            var reset = GetVariableOrDefault(RESET_BEHAVIOR, "latest");
            var maxPollInterval = Int32.Parse(GetVariableOrDefault(MAX_POLL_INTERVAL_CST, "60000"));
            var externalServiceUrl = GetVariableOrDefault(EXTERNAL_SERVICE_URL, "http://localhost:8080/service");
            var commitInterval = Int32.Parse(GetVariableOrDefault(COMMIT_INTERVAL, "1000"));
            
            AdminClientConfig config = new AdminClientConfig() {
                BootstrapServers = bootstrap
            };
            AdminClientBuilder adminClientBuilder = new AdminClientBuilder(config);
            
            await KafkaUtils.CreateTopic(adminClientBuilder.Build(), topicName, partitionNumber);

            RetriableConsumer consumer = new RetriableConsumer(bootstrap, topicName, numberRetry, groupId, reset, maxPollInterval, externalServiceUrl, commitInterval);
            consumer.Start();

            Console.CancelKeyPress += (o, a) => consumer.Stop();
        }

        public static string GetVariableOrDefault(string envVar, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            return value ?? defaultValue;
        }
    }
}