using System;
using System.Buffers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Next.Abstractions.Channels;
using RabbitMQ.Next.Abstractions.Messaging;
using RabbitMQ.Next.Abstractions.Methods;
using RabbitMQ.Next.Buffers;
using RabbitMQ.Next.Transport.Methods.Basic;

namespace RabbitMQ.Next.Channels
{
    internal class MethodSender
    {
        private readonly ObjectPool<FrameBuilder> frameBuilderPool;
        private readonly ChannelWriter<IMemoryBlock> socketWriter;
        private readonly SemaphoreSlim senderSync;

        private readonly IMethodFormatter<PublishMethod> publishMethodFormatter;
        private readonly IMethodRegistry registry;

        public MethodSender(ChannelWriter<IMemoryBlock> socketWriter, IMethodRegistry registry, ObjectPool<FrameBuilder> frameBuilderPool)
        {
            this.socketWriter = socketWriter;
            this.registry = registry;
            this.publishMethodFormatter = registry.GetFormatter<PublishMethod>();
            this.frameBuilderPool = frameBuilderPool;
            this.senderSync = new SemaphoreSlim(1,1);
        }

        public ValueTask SendAsync<TRequest>(TRequest request, CancellationToken cancellation = default)
            where TRequest : struct, IOutgoingMethod
        {
            var frameBuilder = this.frameBuilderPool.Get();
            var formatter = this.registry.GetFormatter<TRequest>();
            frameBuilder.WriteMethodFrame(request, formatter);

            return this.TransmitFrameAsync(frameBuilder, cancellation);
        }

        public ValueTask PublishAsync<TState>(
            TState state, string exchange, string routingKey,
            IMessageProperties properties, Action<TState, IBufferWriter<byte>> contentBody,
            PublishFlags flags = PublishFlags.None, CancellationToken cancellation = default)
        {
            var frameBuilder = this.frameBuilderPool.Get();
            var publishMethod = new PublishMethod(exchange, routingKey, (byte)flags);

            frameBuilder.WriteMethodFrame(publishMethod, this.publishMethodFormatter);
            frameBuilder.WriteContentFrame(state, properties, contentBody);

            return this.TransmitFrameAsync(frameBuilder, cancellation);
        }

        private async ValueTask TransmitFrameAsync(FrameBuilder frame, CancellationToken cancellation)
        {
            await this.senderSync.WaitAsync(cancellation);

            try
            {
                await frame.WriteToAsync(this.socketWriter);
            }
            finally
            {
                this.senderSync.Release();
                this.frameBuilderPool.Return(frame);
            }
        }
    }
}