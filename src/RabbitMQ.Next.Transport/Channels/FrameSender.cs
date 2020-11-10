using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions.Messaging;
using RabbitMQ.Next.Abstractions.Methods;
using RabbitMQ.Next.Transport.Buffers;
using RabbitMQ.Next.Transport.Methods;
using RabbitMQ.Next.Transport.Methods.Registry;

namespace RabbitMQ.Next.Transport.Channels
{
    internal class FrameSender : IFrameSender
    {
        private readonly ISocketWriter socketWriter;
        private readonly IMethodRegistry registry;
        private readonly ushort channelNumber;
        private readonly IBufferPoolInternal bufferPool;

        public FrameSender(ISocketWriter socketWriter, IMethodRegistry registry, ushort channelNumber, IBufferPoolInternal bufferPool)
        {
            this.socketWriter = socketWriter;
            this.registry = registry;
            this.channelNumber = channelNumber;
            this.bufferPool = bufferPool;
        }

        public async Task SendMethodAsync<TMethod>(TMethod method)
            where TMethod : struct, IOutgoingMethod
        {
            using var buffer = this.bufferPool.CreateMemory();
            var memory = buffer.Memory;

            var written = this.registry.FormatMessage(method, memory.Slice(ProtocolConstants.FrameHeaderSize));
            memory.Span.WriteFrameHeader(new FrameHeader(FrameType.Method, this.channelNumber, written));

            var result = memory.Slice(0, ProtocolConstants.FrameHeaderSize + written);
            await this.socketWriter.SendAsync(result);
        }

        public async Task SendContentHeaderAsync(MessageProperties properties, ulong contentSize)
        {
            using var buffer = this.bufferPool.CreateMemory();
            var memory = buffer.Memory;

            var written = memory.Span.Slice(ProtocolConstants.FrameHeaderSize).WriteContentHeader(properties, contentSize);
            memory.Span.WriteFrameHeader(new FrameHeader(FrameType.ContentHeader, this.channelNumber, written));

            await this.socketWriter.SendAsync(memory.Slice(0, ProtocolConstants.FrameHeaderSize + written));
        }

        public Task SendContentAsync(ReadOnlySequence<byte> contentBytes)
        {
            if (contentBytes.IsSingleSegment)
            {
                return this.SendContentSingleChunkAsync(contentBytes.First);
            }

            return this.SendContentMultiChunksAsync(contentBytes);
        }

        private async Task SendContentSingleChunkAsync(ReadOnlyMemory<byte> content)
        {
            using var headerBuffer = this.bufferPool.CreateMemory();
            var frameHeader = headerBuffer.Memory.Slice(0, ProtocolConstants.FrameHeaderSize);
            frameHeader.Span.WriteFrameHeader(new FrameHeader(FrameType.ContentBody, this.channelNumber, content.Length));

            await this.socketWriter.SendAsync((frameHeader, content), async (sender, state) =>
            {
                await sender(state.frameHeader);
                await sender(state.content);
            });
        }

        private async Task SendContentMultiChunksAsync(ReadOnlySequence<byte> contentBytes)
        {
            async Task SendChunksAsync(Memory<byte> headerBuffer, int size, List<ReadOnlyMemory<byte>> chunks)
            {
                headerBuffer.Span.WriteFrameHeader(new FrameHeader(FrameType.ContentBody, this.channelNumber, size));

                await this.socketWriter.SendAsync((headerBuffer, chunks), async (sender, state) =>
                {
                    await sender(headerBuffer);
                    for (var i = 0; i < state.chunks.Count; i++)
                    {
                        await sender(state.chunks[i]);
                    }
                });
            }

            using var frameHeaderBuffer = this.bufferPool.CreateMemory();
            var frameHeader = frameHeaderBuffer.Memory.Slice(0, ProtocolConstants.FrameHeaderSize);

            var enumerator = new SequenceEnumerator<byte>(contentBytes);
            enumerator.MoveNext();
            var currentFrameChunks = new List<ReadOnlyMemory<byte>>();
            var currentFrameSize = 0;

            do
            {
                if (currentFrameSize + enumerator.Current.Length > this.bufferPool.MaxFrameSize)
                {
                    await SendChunksAsync(frameHeader, currentFrameSize, currentFrameChunks);

                    currentFrameChunks.Clear();
                    currentFrameSize = 0;
                }

                currentFrameChunks.Add(enumerator.Current);
                currentFrameSize += enumerator.Current.Length;
            } while (enumerator.MoveNext());

            if (currentFrameSize > 0)
            {
                await SendChunksAsync(frameHeader, currentFrameSize, currentFrameChunks);
            }
        }
    }
}