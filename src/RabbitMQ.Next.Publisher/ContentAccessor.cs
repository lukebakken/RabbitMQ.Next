using System.Buffers;
using RabbitMQ.Next.Publisher.Abstractions;
using RabbitMQ.Next.Serialization;

namespace RabbitMQ.Next.Publisher
{
    internal class ContentAccessor : IContent
    {
        private readonly ISerializer serializer;
        private ReadOnlySequence<byte> content;

        public ContentAccessor(ISerializer serializer)
        {
            this.serializer = serializer;
        }

        internal void SetPayload(ReadOnlySequence<byte> payload)
        {
            this.content = payload;
        }

        public T GetContent<T>() => this.serializer.Deserialize<T>(this.content);
    }
}