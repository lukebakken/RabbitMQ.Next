using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RabbitMQ.Next.Abstractions;

namespace RabbitMQ.Next.Transport.Buffers
{
    internal class BufferWriter : IBufferWriter
    {
        private const int MinChunkSize = 128;
        private readonly BufferManager manager;
        private List<ArraySegment<byte>> chunks;
        private byte[] buffer;
        private int offset;

        public BufferWriter(BufferManager manager)
        {
            this.manager = manager;

            this.buffer = manager.Rent();
            this.offset = 0;
        }

        public void Advance(int count)
        {
            this.CheckDisposed();

            if (count < 0)
            {
                throw new ArgumentException(nameof(count));
            }

            if (this.offset + count > this.buffer.Length)
            {
                throw new InvalidOperationException("Cannot advance past the end of the buffer.");
            }

            this.offset += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            this.CheckDisposed();
            this.ExpandIfRequired(sizeHint);

            return new Memory<byte>(this.buffer).Slice(this.offset);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            this.CheckDisposed();
            this.ExpandIfRequired(sizeHint);

            return new Span<byte>(this.buffer).Slice(this.offset);
        }

        public ReadOnlySequence<byte> ToSequence()
        {
            this.CheckDisposed();

            if (this.chunks == null)
            {
                return new ReadOnlySequence<byte>(this.buffer, 0, this.offset);
            }

            var first = new MemorySegment<byte>(new Memory<byte>(this.buffer, 0, this.offset));
            var last = first;

            foreach (var chunk in this.chunks)
            {
                last = last.Append(chunk);
            }

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        public void Dispose()
        {
            if (this.buffer == null)
            {
                return;
            }

            this.manager.Return(this.buffer);
            if (this.chunks != null)
            {
                foreach (var chunk in this.chunks)
                {
                    this.manager.Return(chunk.Array);
                }
            }
            this.buffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (this.buffer == null)
            {
                throw new ObjectDisposedException(nameof(BufferWriter));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExpandIfRequired(int requestedSize)
        {
            if (requestedSize == 0)
            {
                requestedSize = MinChunkSize;
            }

            if (this.offset + requestedSize <= this.buffer.Length)
            {
                return;
            }

            this.chunks ??= new List<ArraySegment<byte>>();
            this.chunks.Add(new ArraySegment<byte>(this.buffer, 0, this.offset));

            this.buffer = (requestedSize > this.manager.BufferSize) ? new byte[requestedSize] : this.manager.Rent();
            this.offset = 0;
        }
    }
}