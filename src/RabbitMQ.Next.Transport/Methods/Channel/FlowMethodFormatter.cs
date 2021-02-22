using System;
using RabbitMQ.Next.Abstractions.Methods;

namespace RabbitMQ.Next.Transport.Methods.Channel
{
    internal class FlowMethodFormatter : IMethodFormatter<FlowMethod>
    {
        public Span<byte> Write(Span<byte> destination, FlowMethod method)
            => destination.Write(method.Active);
    }
}