using System;
using RabbitMQ.Next.Methods;

namespace RabbitMQ.Next.Transport.Methods.Channel
{
    internal class CloseMethodFormatter : IMethodFormatter<CloseMethod>
    {
        public int Write(Span<byte> destination, CloseMethod method)
        {
            var result = destination.Write(method.StatusCode)
                .Write(method.Description)
                .Write((uint) method.FailedMethodId);

            return destination.Length - result.Length;
        }
    }
}