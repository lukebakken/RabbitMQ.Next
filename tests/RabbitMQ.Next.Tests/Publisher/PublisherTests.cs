using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using RabbitMQ.Next.Abstractions;
using RabbitMQ.Next.Abstractions.Channels;
using RabbitMQ.Next.Abstractions.Messaging;
using RabbitMQ.Next.Publisher.Abstractions;
using RabbitMQ.Next.Publisher.Abstractions.Transformers;
using RabbitMQ.Next.Transport.Methods.Basic;
using Xunit;

namespace RabbitMQ.Next.Tests.Publisher
{
    public class PublisherTests : PublisherTestsBase
    {
        [Theory]
        [MemberData(nameof(PublishTestCases))]
        public async Task PublishAsync(
            IReadOnlyList<IMessageTransformer> transformers, 
            string exchange, string routingKey, IMessageProperties properties, PublishFlags flags,
            PublishMethod expectedMethod, IMessageProperties expectedProperties)
        {
            var channel = Substitute.For<IChannel>();
            var connection = this.MockConnection();
            connection.CreateChannelAsync(Arg.Any<IEnumerable<IFrameHandler>>()).Returns(Task.FromResult(channel));

            var publisher = new Next.Publisher.Publisher(connection, this.MockSerializer(), transformers, null);

            await publisher.PublishAsync("test", exchange, routingKey, properties, flags);

            await channel.Received().SendAsync(
                expectedMethod,
                Arg.Is<IMessageProperties>(p => new MessagePropertiesComparer().Equals(p, expectedProperties)),
                Arg.Any<ReadOnlySequence<byte>>()
            );
        }

        [Fact]
        public async Task ShouldThrowIfConnectionClosed()
        {
            var connection = this.MockConnection();
            connection.State.Returns(ConnectionState.Closed);

            var publisher = new Next.Publisher.Publisher(connection, this.MockSerializer(), null, null);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await publisher.PublishAsync("test"));
        }

        [Fact]
        public async Task ShouldThrowIfDisposed()
        {
            var connection = this.MockConnection();

            var publisher = new Next.Publisher.Publisher(connection, this.MockSerializer(), null, null);
            await publisher.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await publisher.PublishAsync("test"));
        }

        [Fact]
        public async Task CanDisposeMultipleTimes()
        {
            var connection = this.MockConnection();

            var publisher = new Next.Publisher.Publisher(connection, this.MockSerializer(), null, null);
            await publisher.DisposeAsync();

            var ex = await Record.ExceptionAsync(async () => await publisher.DisposeAsync());

            Assert.Null(ex);
        }

        [Fact]
        public async Task CompleteShouldDispose()
        {
            var connection = this.MockConnection();

            var publisher = new Next.Publisher.Publisher(connection, this.MockSerializer(), null, null);
            await publisher.CompleteAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await publisher.PublishAsync("test"));
        }

        [Fact]
        public async Task DisposeShouldDisposeHandlers()
        {
            var connection = this.MockConnection();

            var returnedMessageHandler = Substitute.For<IReturnedMessageHandler>();

            var publisher = new Next.Publisher.Publisher(connection, this.MockSerializer(), null, new[] {returnedMessageHandler});
            await publisher.DisposeAsync();

            returnedMessageHandler.Received().Dispose();
        }

        [Fact]
        public async Task DisposeShouldCloseChannel()
        {
            var channel = Substitute.For<IChannel>();
            var connection = this.MockConnection();
            connection.CreateChannelAsync(Arg.Any<IEnumerable<IFrameHandler>>()).Returns(Task.FromResult(channel));

            var publisher = new Next.Publisher.Publisher(connection, this.MockSerializer(), null, null);
            await publisher.PublishAsync("test", "exchange");
            await publisher.DisposeAsync();

            await channel.Received().CloseAsync();
        }
    }
}