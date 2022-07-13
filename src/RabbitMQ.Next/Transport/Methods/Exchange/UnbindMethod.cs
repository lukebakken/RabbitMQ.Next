using System.Collections.Generic;
using RabbitMQ.Next.Methods;

namespace RabbitMQ.Next.Transport.Methods.Exchange;

public readonly struct UnbindMethod : IOutgoingMethod
{
    public UnbindMethod(string destination, string source, string routingKey, IReadOnlyDictionary<string, object> arguments)
    {
        this.Destination = destination;
        this.Source = source;
        this.RoutingKey = routingKey;
        this.Arguments = arguments;
    }

    public MethodId MethodId => MethodId.ExchangeUnbind;

    public string Destination { get; }

    public string Source { get; }

    public string RoutingKey { get; }

    public IReadOnlyDictionary<string, object> Arguments { get; }
}