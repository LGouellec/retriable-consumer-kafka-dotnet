# Retriable consumer kafka dotnet

Inspired from https://github.com/jeanlouisboudart/retriable-consumer for .NET Clients

## Build local images

This repository contains some local docker images including :

a simple producer a retriable consumer

To build all images you just need to run :

``` bash
docker-compose build
```

## Start environment

Start the environment
To start the environment simply run the following command

```
docker-compose up -d
```

This would start :

- Zookeeper
- Kafka
- a simple producer, which produce one message per second
- a consumer with no retry (0 retry, if external service failed, message was skipped)
- a consumer with limited number of retries (max retry : 10)
- a consumer with infinite number of retries (use pause/resume Kafka Consumer API, retry the message until web service call is done)
- a consumer with limited number of retries (max retry : 20) but raising max.poll.interval.ms before end of all retry

Please observe the logs and behavior of each consumers. 

Visit http://localhost:8082/

![lag](./lag-offsets.png)


## no-retry-consumer

``` bash
docker-compose logs -f no-retry-consumer
```

This consumer will ignore failures in case of errors when calling an external system.

## limited-retries-consumer

``` bash
docker-compose logs -f limited-retries-consumer
```

This consumer will retry X times in case of errors when calling an external system.

You can increase the number of max retry on `docker-compose.yml`(env var : 'KAFKA_NUMBER_RETRY')


## infinite-retries-consumer

``` bash
docker-compose logs -f infinite-retries-consumer
```

This consumer will retry infinitely in case of errors when calling an external system. In case of failures, the consumer is paused and offset is set to the previous record. Next call to the poll(timeout) method will honour the timeout and will return an empty record, so this will act as backoff.

## max-poll-internal-raising-consumer

``` bash
docker-compose logs -f max-poll-internal-raising-consumer
```

``` yaml
KAFKA_NUMBER_RETRY: 20 # 0 = NO_RETRY, -1 = INFINITE_RETRY, > 0 = MAX_RETRY
KAFKA_GROUP_ID: "max-poll-internal-raising-consumer-group"
KAFKA_RESET_STRATEGY: "earliest"
KAFKA_MAX_POLL_INTERVAL_MS: 60000 # 1 min
KAFKA_SIMULATE_EXTERNAL: "true" # Simulate external call see retriable_consumer. ExternalService class
KAFKA_EXTERNAL_PERCENTAGE_FAILURES: 100 # 100 percent failure
DURATION_SLEEP_SERVICE_FAIL: 4000 # 4 seconds
KAFKA_COMMIT_INTERVAL_MS: 10000 # 10 seconds
```

This consumer will retry 20 times to simulate external system. Unfortunately external system return 100% an error and takes 4 seconds to respond. So for each loop, 20 * 4s = 80s for processing one record.

However, max.poll.interval.ms of consumer is configured with 60000 (1 minute) < 80 seconds for processing records.

**So what happens ?**

Before the end of loop processing, consumer background thread received a max.poll.internal.ms error internaly. When consumer will commit the offsets after the 20 try, it'll throw an exception in high-level, catched, log and recreate a new instance of consumer from scratch.

New consumer instance will reprocess previous record (because never committed), retry 20 times, etc ... So it's like an infinite retry using a loop retry processing without pause&resume API.

However, when you leave consumer group, you lost your offset in-memory dictionary and that's why you have re-process previous record indefinitely.

In conclusion, when you use a loop retry processing be carefull about 3 things :
- unitary time to call external system
- number of retry
- max.poll.internal.ms consumer configuration

**max-poll-internal-ms > number-of-retry * max(unitary-call-time)**