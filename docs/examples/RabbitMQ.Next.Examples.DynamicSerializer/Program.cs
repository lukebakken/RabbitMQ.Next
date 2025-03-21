﻿using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Consumer;
using RabbitMQ.Next.Publisher;
using RabbitMQ.Next.Serialization.Dynamic;
using RabbitMQ.Next.Serialization.MessagePack;
using RabbitMQ.Next.Serialization.SystemJson;

namespace RabbitMQ.Next.Examples.DynamicSerializer;

internal static class Program
{
    private static async Task Main()
    {
        await using var connection = ConnectionBuilder.Default
            .UseConnectionString("amqp://guest:guest@localhost:5672/")
            .UseDynamicSerializer(serializer => serializer
                .When(m => "application/json".Equals(m.ContentType, StringComparison.InvariantCultureIgnoreCase)).UseSystemJsonSerializer()
                .When(m => "application/msgpack".Equals(m.ContentType, StringComparison.InvariantCultureIgnoreCase)).UseMessagePackSerializer()
                .When(_ => true).UseSystemJsonSerializer()
            )
            .Build();

        Console.WriteLine("Connection opened");
        await PublishMessagesAsync(connection);
        Console.WriteLine("Messages were publisher, press any key to consume the messages.");
        Console.ReadKey();
        await ConsumeMessagesAsync(connection);
        Console.WriteLine("Done");
    }

    private static async Task PublishMessagesAsync(IConnection connection)
    {
        await using var publisher = connection.Publisher("amq.fanout");

        // The message will be formatted using MessagePackSerializer, because there is corresponding registration
        await publisher.PublishAsync(new DummyDto { SomeProperty = "some message with msgpack content type"}, 
            message => message.SetContentType("application/msgpack"));
        
        // The message will be formatted using SystemJsonSerializer, because there is corresponding registration
        await publisher.PublishAsync(new DummyDto { SomeProperty = "some message with json content type"}, 
            message => message.SetContentType("application/json"));
        
        // The last two messages will be formatted using SystemJsonSerializer, because the last registered serializer accept any messages 
        await publisher.PublishAsync(new DummyDto { SomeProperty = "some message without specified content type"});
        await publisher.PublishAsync(new DummyDto { SomeProperty = "some message with unknown content type"}, 
            message => message.SetContentType("application/x-unknown"));
    }

    private static async Task ConsumeMessagesAsync(IConnection connection)
    {
        await using var consumer = connection.Consumer(
            builder => builder
                .BindToQueue("my-queue")
                .PrefetchCount(10));
        
        using var cancellation = new CancellationTokenSource(10_000); // simply cancel after 10 seconds
        await consumer.ConsumeAsync((message, content) =>
        {
            Console.WriteLine($"Message content-type: {message.ContentType}, {content.Get<DummyDto>().SomeProperty}");
        } ,cancellation.Token);
    }
}
