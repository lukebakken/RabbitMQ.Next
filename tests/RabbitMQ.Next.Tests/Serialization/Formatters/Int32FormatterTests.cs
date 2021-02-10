using System;
using System.Buffers;
using RabbitMQ.Next.Serialization.Formatters;
using Xunit;

namespace RabbitMQ.Next.Tests.Serialization.Formatters
{
    public class Int32FormatterTests
    {
        [Theory]
        [InlineData(0, new byte[] { 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000 })]
        [InlineData(1, new byte[] { 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000001 })]
        [InlineData(42, new byte[] { 0b_00000000, 0b_00000000, 0b_00000000, 0b_00101010 })]
        [InlineData(-42, new byte[] { 0b_11111111, 0b_11111111, 0b_11111111, 0b_11010110 })]
        public void CanFormat(int content, byte[] expected)
        {
            var formatter = new Int32Formatter();
            var bufferWriter = new ArrayBufferWriter<byte>(sizeof(int));

            formatter.Format(content, bufferWriter);

            Assert.Equal(expected, bufferWriter.WrittenMemory.ToArray());
        }

        [Theory]
        [InlineData(0, new byte[] { 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000 })]
        [InlineData(1, new byte[] { 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000001 })]
        [InlineData(42, new byte[] { 0b_00000000, 0b_00000000, 0b_00000000, 0b_00101010 })]
        [InlineData(-42, new byte[] { 0b_11111111, 0b_11111111, 0b_11111111, 0b_11010110 })]
        [InlineData(-42, new byte[] { 0b_11111111 }, new byte[] { 0b_11111111, 0b_11111111, 0b_11010110 })]
        [InlineData(-42, new byte[] { 0b_11111111 }, new byte[] { 0b_11111111, 0b_11111111}, new byte[] { 0b_11010110 })]
        public void CanParse(int expected, params byte[][] contentparts)
        {
            var formatter = new Int32Formatter();
            var sequence = Helpers.MakeSequence(contentparts);

            var result = formatter.Parse(sequence);

            Assert.Equal(expected, result);
        }


        [Theory]
        [InlineData(new byte[0])]
        [InlineData(new byte[] { 1 } )]
        [InlineData(new byte[] { 1, 2, 3, 4, 5 } )]
        public void ParseThrowsOnWrongContent(byte[] content)
        {
            var formatter = new Int32Formatter();
            var sequence = new ReadOnlySequence<byte>(content);

            Assert.Throws<ArgumentException>(() =>  formatter.Parse(sequence));
        }
    }
}