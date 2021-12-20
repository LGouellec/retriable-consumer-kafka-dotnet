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