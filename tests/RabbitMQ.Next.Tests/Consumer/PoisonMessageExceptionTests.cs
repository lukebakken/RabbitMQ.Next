using System;
using NSubstitute;
using RabbitMQ.Next.Abstractions.Messaging;
using RabbitMQ.Next.Consumer.Abstractions;
using Xunit;

namespace RabbitMQ.Next.Tests.Consumer
{
    public class PoisonMessageExceptionTests
    {
        [Fact]
        public void PoisonMessageException()
        {
            var message = new DeliveredMessage();
            var properties = Substitute.For<IMessageProperties>();
            var content = Substitute.For<IContentAccessor>();
            var ex = new Exception();

            var exception = new PoisonMessageException(message, properties, content, ex);

            Assert.Equal(message, exception.DeliveredMessage);
            Assert.Equal(properties, exception.Properties);
            Assert.Equal(content, exception.Content);
            Assert.Equal(ex, exception.InnerException);
        }
    }
}