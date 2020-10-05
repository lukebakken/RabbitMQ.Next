using System;
using RabbitMQ.Next.Transport;
using RabbitMQ.Next.Transport.Methods.Channel;
using Xunit;

namespace RabbitMQ.Next.Tests.Transport.Methods.Channel
{
    public class CloseOkMethodTests
    {
        [Fact]
        public void CloseOkMethodCtor()
        {
            var method = new CloseOkMethod();

            Assert.Equal((uint)MethodId.ChannelCloseOk, method.Method);
        }

        [Fact]
        public void CloseOkMethodFormatter()
        {
            var expected = Helpers.GetFileContent("RabbitMQ.Next.Tests.Transport.Methods.Channel.CloseOkMethodPayload.dat");

            var data = new CloseOkMethod();
            Span<byte> payload = stackalloc byte[expected.Length];
            new CloseOkMethodFormatter().Write(payload, data);

            Assert.Equal(expected, payload.ToArray());
        }

        [Fact]
        public void CloseOkMethodFrameParser()
        {
            var payload = Helpers.GetFileContent("RabbitMQ.Next.Tests.Transport.Methods.Channel.CloseOkMethodPayload.dat");
            var parser = new CloseOkMethodParser();
            var data = parser.Parse(payload);
            var dataBoxed = parser.ParseMethod(payload);

            var expected = new CloseOkMethod();

            Assert.Equal(expected, data);
            Assert.Equal(expected, dataBoxed);
        }
    }
}