using System;
using System.Threading;
using Confluent.Kafka;

namespace producer
{
    public class Program
    {
        public static readonly string BOOTSTRAP_SERVER_CST = "KAFKA_BOOTSTRAP_SERVER";
        public static readonly string TOPIC_CST = "KAFKA_TOPIC";

        public static void Main(string[] args)
        {
            var topicName = GetVariableOrDefault(TOPIC_CST, "my_topic");
            var bootstrap = GetVariableOrDefault(BOOTSTRAP_SERVER_CST, "localhost:9092");

            ProducerConfig config = new ProducerConfig();
            config.BootstrapServers = bootstrap;
            config.Acks = Acks.All;
            CancellationTokenSource source = new CancellationTokenSource();
            ProducerBuilder<string, string> builder = new ProducerBuilder<string, string>(config);
            bool isStop = false;
            
            Console.CancelKeyPress += (o, a) =>
            {
                source.Cancel();
                while (!isStop)
                    Thread.Sleep(50);
            };
            
            using (var producer = builder.Build())
            {
                var random = new Random();
                
                while (!source.IsCancellationRequested)
                {
                    producer.Produce(topicName, new Message<string, string>() {
                        Key = $"Key {random.Next(Int32.MaxValue)}",
                        Value = $"Value {random.Next(Int32.MaxValue)}"
                    });
                    Thread.Sleep(1000);
                }

                producer.Flush();
                isStop = true;
            }
        }

        public static string GetVariableOrDefault(string envVar, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            return value ?? defaultValue;
        }
    }
}