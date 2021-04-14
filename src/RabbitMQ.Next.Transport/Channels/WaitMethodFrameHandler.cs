using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions;
using RabbitMQ.Next.Abstractions.Channels;
using RabbitMQ.Next.Abstractions.Exceptions;
using RabbitMQ.Next.Abstractions.Methods;
using RabbitMQ.Next.Transport.Methods;

namespace RabbitMQ.Next.Transport.Channels
{
    internal class WaitMethodFrameHandler : IFrameHandler
    {
        private readonly Action cancellationHandler;
        private readonly IMethodRegistry registry;
        private uint expectedMethodId;
        private TaskCompletionSource<IIncomingMethod> waitingTask;

        public WaitMethodFrameHandler(IMethodRegistry registry)
        {
            this.registry = registry;
            this.cancellationHandler = () =>
            {
                this.waitingTask?.SetCanceled();
            };
        }

        public Task<IIncomingMethod> WaitAsync<TMethod>(CancellationToken cancellation = default)
            where TMethod : struct, IIncomingMethod
        {
            // todo: validate state, should probably throw if in wait state already
            var methodId = this.registry.GetMethodId<TMethod>();
            this.waitingTask = new TaskCompletionSource<IIncomingMethod>();
            this.expectedMethodId = methodId;
            if (cancellation != default)
            {
                cancellation.Register(this.cancellationHandler);
            }

            return this.waitingTask.Task;
        }

        bool IFrameHandler.Handle(ChannelFrameType frameType, ReadOnlySequence<byte> payload)
        {
            if (this.waitingTask == null)
            {
                return false;
            }

            payload = payload.Read(out uint methodId);

            var task = this.waitingTask;
            if (methodId == this.expectedMethodId)
            {
                this.waitingTask = null;
                this.expectedMethodId = 0;
                task.SetResult(this.registry.GetParser(methodId).ParseMethod(payload));

                return true;
            }

            if (methodId == (uint) MethodId.ChannelClose)
            {
                var channelClose = this.registry.GetParser<Methods.Channel.CloseMethod>().Parse(payload);
                task.SetException(new ChannelException(channelClose.StatusCode, channelClose.Description, channelClose.FailedMethodId));
            }

            return false;
        }
    }
}