using System;
using NSubstitute;
using RabbitMQ.Next.Transport.Buffers;
using Xunit;

namespace RabbitMQ.Next.Tests.Transport.Buffers
{
    public class MemoryOwnerTests
    {
        [Fact]
        public void MemoryRentsBuffer()
        {
            var bufferManager = Substitute.For<IBufferManager>();
            bufferManager.Rent().Returns(new byte[10]);

            var memoryOwner = new MemoryOwner(bufferManager);
            var memory = memoryOwner.Memory;

            bufferManager.Received().Rent();
            Assert.Equal(10, memory.Length);
        }

        [Fact]
        public void MemoryReturnsSameBuffer()
        {
            var bufferManager = Substitute.For<IBufferManager>();
            bufferManager.Rent().Returns(new byte[10]);

            var memoryOwner = new MemoryOwner(bufferManager);
            var memory = memoryOwner.Memory;
            var memory2 = memoryOwner.Memory;

            Assert.Equal(memory, memory2);
        }

        [Fact]
        public void DisposeReturnsBuffer()
        {
            var buffer = new byte[10];

            var bufferManager = Substitute.For<IBufferManager>();
            bufferManager.Rent().Returns(buffer);

            var memoryOwner = new MemoryOwner(bufferManager);

            memoryOwner.Dispose();

            bufferManager.Received().Release(buffer);
        }

        [Fact]
        public void DisposedThrows()
        {
            var bufferManager = Substitute.For<IBufferManager>();

            var memoryOwner = new MemoryOwner(bufferManager);
            memoryOwner.Dispose();

            Assert.Throws<ObjectDisposedException>(() => memoryOwner.Memory);
        }

        [Fact]
        public void CanDisposeMultiple()
        {
            var bufferManager = Substitute.For<IBufferManager>();

            var memoryOwner = new MemoryOwner(bufferManager);

            memoryOwner.Dispose();

            var exception = Record.Exception(() => memoryOwner.Dispose());
            Assert.Null(exception);
        }
    }
}