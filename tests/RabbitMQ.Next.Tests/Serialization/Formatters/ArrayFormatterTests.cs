using System;
using System.Buffers;
using RabbitMQ.Next.Serialization.Formatters;
using Xunit;

namespace RabbitMQ.Next.Tests.Serialization.Formatters
{
    public class ArrayFormatterTests
    {
        [Theory]
        [InlineData(1, new byte[0])]
        [InlineData(5, new byte[] { 1, 2, 3, 4, 5 })]
        [InlineData(5, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        public void CanFormat(int initialBufferSize, byte[] data)
        {
            var formatter = new ArrayFormatter();
            var bufferWriter = new ArrayBufferWriter<byte>(initialBufferSize);

            formatter.Format(data, bufferWriter);

            Assert.Equal(data, bufferWriter.WrittenMemory.ToArray());
        }

        [Theory]
        [InlineData(new byte[0], new byte[0])]
        [InlineData(new byte[] { 1, 2, 3, 4, 5 }, new byte[] { 1, 2 }, new byte[] { 3, 4, 5 })]
        public void CanParse(byte[] expected, params byte[][] contentparts)
        {
            var formatter = new ArrayFormatter();
            var sequence = Helpers.MakeSequence(contentparts);

            var result = formatter.Parse(sequence);

            Assert.Equal(expected, result);
        }
    }
}