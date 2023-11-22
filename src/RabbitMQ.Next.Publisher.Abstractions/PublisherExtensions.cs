using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Next.Publisher;

public static class PublisherExtensions
{
    public static Task PublishAsync<TContent>(
        this IPublisher publisher,
        string routingKey,
        TContent content,
        Action<IMessageBuilder> propertiesBuilder = null,
        CancellationToken cancellation = default)
        => publisher.PublishAsync(
            routingKey,
            content,
            (state, message) =>
            {
                message.SetRoutingKey(state);
            },
            cancellation);
}