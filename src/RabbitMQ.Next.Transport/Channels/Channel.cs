using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions;
using RabbitMQ.Next.Abstractions.Buffers;
using RabbitMQ.Next.Abstractions.Channels;
using RabbitMQ.Next.Abstractions.Exceptions;
using RabbitMQ.Next.Abstractions.Messaging;
using RabbitMQ.Next.Abstractions.Methods;
using RabbitMQ.Next.Transport.Messaging;
using RabbitMQ.Next.Transport.Methods.Channel;

namespace RabbitMQ.Next.Transport.Channels
{
    internal sealed class Channel : IChannelInternal
    {
        private readonly SynchronizedChannel syncChannel;
        private readonly SemaphoreSlim senderSync;
        private readonly ChannelPool channelPool;
        private readonly IMethodRegistry registry;
        private readonly IBufferPool bufferPool;
        private readonly TaskCompletionSource<bool> channelCompletion;

        private readonly IReadOnlyList<IMethodHandler> methodHandlers;

        public Channel(ChannelPool channelPool, IMethodRegistry methodRegistry, IFrameSender frameSender, IBufferPool bufferPool, IEnumerable<IMethodHandler> handlers)
        {
            this.channelPool = channelPool;
            this.registry = methodRegistry;
            this.bufferPool = bufferPool;

            handlers ??= Array.Empty<IMethodHandler>();

            this.channelCompletion = new TaskCompletionSource<bool>();
            var pipe = new Pipe();
            this.Writer =  pipe.Writer;
            this.senderSync = new SemaphoreSlim(1,1);

            var waitFrameHandler = new WaitMethodHandler(methodRegistry, this);
            this.methodHandlers = new List<IMethodHandler>(handlers) { waitFrameHandler };
            this.ChannelNumber = channelPool.Register(this);
            this.syncChannel = new SynchronizedChannel(this.ChannelNumber, frameSender, waitFrameHandler);

            Task.Run(() => this.LoopAsync(pipe.Reader));
        }

        public ushort ChannelNumber { get; }

        public PipeWriter Writer { get; }

        public Task Completion => this.channelCompletion.Task;

        public void SetCompleted(Exception ex = null)
        {
            if (ex != null)
            {
                this.channelCompletion.SetException(ex);
            }
            else
            {
                this.channelCompletion.SetResult(true);
            }

            this.Writer.Complete();
        }

        public async Task SendAsync<TMethod>(TMethod request)
            where TMethod : struct, IOutgoingMethod
        {
            await this.senderSync.WaitAsync();

            try
            {
                this.ValidateState();
                await this.syncChannel.SendAsync(request);
            }
            finally
            {
                this.senderSync.Release();
            }
        }

        public async Task SendAsync<TMethod>(TMethod request, IMessageProperties properties, ReadOnlySequence<byte> content)
            where TMethod : struct, IOutgoingMethod
        {
            await this.senderSync.WaitAsync();

            try
            {
                this.ValidateState();
                await this.syncChannel.SendAsync(request, properties, content);
            }
            finally
            {
                this.senderSync.Release();
            }
        }

        public async Task<TMethod> WaitAsync<TMethod>(CancellationToken cancellation = default)
            where TMethod : struct, IIncomingMethod
        {
            await this.senderSync.WaitAsync(cancellation);

            try
            {
                this.ValidateState();
                return await this.syncChannel.WaitAsync<TMethod>(cancellation);
            }
            finally
            {
                this.senderSync.Release();
            }
        }

        public async Task UseSyncChannel<TState>(TState state, Func<ISynchronizedChannel, TState, Task> fn)
        {
            await this.senderSync.WaitAsync();

            try
            {
                this.ValidateState();
                await fn(this.syncChannel, state);
            }
            finally
            {
                this.senderSync.Release();
            }
        }

        public async Task<TResult> UseSyncChannel<TResult, TState>(TState state, Func<ISynchronizedChannel, TState, Task<TResult>> fn)
        {
            await this.senderSync.WaitAsync();

            try
            {
                this.ValidateState();
                return await fn(this.syncChannel, state);
            }
            finally
            {
                this.senderSync.Release();
            }
        }

        public Task CloseAsync()
            => this.CloseAsync((ushort)ReplyCode.Success, string.Empty, 0);

        public async Task CloseAsync(ushort statusCode, string description, uint failedMethodId)
        {
            await this.SendAsync<CloseMethod, CloseOkMethod>(new CloseMethod(statusCode, description, failedMethodId));
            this.SetCompleted();
            this.channelPool.Release(this.ChannelNumber);
        }

        private async Task LoopAsync(PipeReader pipeReader)
        {
            Func<ReadOnlySequence<byte>, (ChannelFrameType Type, uint Size)> headerParser = ChannelFrame.ReadHeader;
            Func<ReadOnlySequence<byte>, IIncomingMethod> methodFrameParser = this.ParseMethodFrame;
            Func<IIncomingMethod, ReadOnlySequence<byte>, ValueTask<bool>> methodHandler = this.HandleMethodAsync;

            while (!this.Completion.IsCompleted)
            {
                var header = await pipeReader.ReadAsync(ChannelFrame.FrameHeaderSize, headerParser);
                if (header == default)
                {
                    return;
                }

                // TODO: check frame type here, it should be method frame only
                var methodArgs = await pipeReader.ReadAsync(header.Size, methodFrameParser);

                if (methodArgs == default)
                {
                    return;
                }

                bool processed;
                if (this.registry.HasContent(methodArgs.Method))
                {
                    var contentHeader = await pipeReader.ReadAsync(ChannelFrame.FrameHeaderSize, headerParser);
                    if (header == default)
                    {
                        return;
                    }

                    // TODO: check frame type here, it should be content frame only
                    processed = await pipeReader.ReadAsync(contentHeader.Size,
                        (methodArgs, methodHandler),
                        (state, sequence) => state.methodHandler(state.methodArgs, sequence));
                }
                else
                {
                    processed = await methodHandler(methodArgs, default);
                }

                if (methodArgs is CloseMethod closeMethod)
                {
                    await this.syncChannel.SendAsync(new CloseOkMethod());
                    this.SetCompleted(new ChannelException(closeMethod.StatusCode, closeMethod.Description, closeMethod.Method));
                    this.channelPool.Release(this.ChannelNumber);
                }

                if (!processed)
                {
                    // todo: close channel on unexpected method
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateState()
        {
            if (this.Completion.IsCompleted)
            {
                throw new InvalidOperationException("Cannot perform operation on closed channel");
            }
        }

        private IIncomingMethod ParseMethodFrame(ReadOnlySequence<byte> payload)
        {
            payload = payload.Read(out uint methodId);
            var parser = this.registry.GetParser(methodId);

            if (payload.IsSingleSegment)
            {
                return parser.ParseMethod(payload.FirstSpan);
            }

            using var buffer = this.bufferPool.CreateMemory((int)payload.Length);
            payload.CopyTo(buffer.Memory.Span);
            return parser.ParseMethod(buffer.Memory.Span);
        }

        private async ValueTask<bool> HandleMethodAsync(IIncomingMethod method, ReadOnlySequence<byte> content)
        {
            MessageProperties properties = null;
            ReadOnlySequence<byte> contentBody = default;

            try
            {
                if (!content.IsEmpty)
                {
                    content = content.Read(out int headerSize);
                    properties = new MessageProperties(content.Slice(0, headerSize));
                    contentBody = content.Slice(headerSize);
                }

                for (var i = 0; i < this.methodHandlers.Count; i++)
                {
                    var handled = await this.methodHandlers[i].HandleAsync(method, properties, contentBody);
                    if (handled)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                properties?.Dispose();
            }

            return false;
        }
    }
}