using System;
using RabbitMQ.Next.Abstractions.Methods;

namespace RabbitMQ.Next.Transport.Methods.Channel
{
    internal class FlowOkMethodFormatter : IMethodFormatter<FlowOkMethod>
    {
        public Span<byte> Write(Span<byte> destination, FlowOkMethod method)
            => destination.Write(method.Active);
    }
}