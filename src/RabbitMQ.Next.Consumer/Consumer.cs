using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions;
using RabbitMQ.Next.Abstractions.Channels;
using RabbitMQ.Next.Abstractions.Messaging;
using RabbitMQ.Next.Consumer.Abstractions;
using RabbitMQ.Next.Serialization.Abstractions;
using RabbitMQ.Next.Transport.Methods.Basic;

namespace RabbitMQ.Next.Consumer
{
    internal class Consumer : IConsumer
    {
        private readonly IConnection connection;
        private readonly ISerializer serializer;
        private readonly List<Func<DeliveredMessage, IMessageProperties, IContentAccessor, ValueTask<bool>>> handlers;
        private readonly ConsumerInitializer initializer;
        private readonly Func<IAcknowledgement, IAcknowledger> acknowledgerFactory;
        private readonly UnprocessedMessageMode onUnprocessedMessage;
        private readonly UnprocessedMessageMode onPoisonMessage;
        private readonly ContentAccessor contentAccessor;

        private IChannel channel;
        private IAcknowledger acknowledger;
        private bool isCancelled;

        public Consumer(
            IConnection connection,
            ISerializer serializer,
            List<Func<DeliveredMessage, IMessageProperties, IContentAccessor, ValueTask<bool>>> handlers,
            ConsumerInitializer initializer,
            Func<IAcknowledgement, IAcknowledger> acknowledgerFactory,
            UnprocessedMessageMode onUnprocessedMessage,
            UnprocessedMessageMode onPoisonMessage)
        {
            this.connection = connection;
            this.serializer = serializer;
            this.handlers = handlers;
            this.initializer = initializer;
            this.acknowledgerFactory = acknowledgerFactory;
            this.onUnprocessedMessage = onUnprocessedMessage;
            this.onPoisonMessage = onPoisonMessage;

            this.contentAccessor = new ContentAccessor(serializer);
        }


        public ValueTask DisposeAsync()
            => this.CancelConsumeAsync();

        public async Task ConsumeAsync(CancellationToken cancellation)
        {
            this.channel = await this.connection.OpenChannelAsync(new []
            {
                new DeliverFrameHandler(this.connection.MethodRegistry, this.HandleMessageAsync)
            }, cancellation);

            if (this.acknowledgerFactory != null)
            {
                var ack = new Acknowledgement(this.channel);
                this.acknowledger = this.acknowledgerFactory(ack);
            }

            await this.initializer.InitConsumerAsync(this.channel, cancellation);

            cancellation.Register(() => this.CancelConsumeAsync());

            await this.channel.Completion;
        }

        private async ValueTask CancelConsumeAsync(Exception ex = null)
        {
            this.isCancelled = true;

            await this.initializer.CancelAsync(this.channel);

            if (this.acknowledger != null)
            {
                await this.acknowledger.DisposeAsync();
            }

            await this.channel.CloseAsync(ex);

            this.acknowledger = null;
            this.channel = null;
        }

        private async ValueTask<bool> HandleMessageAsync(DeliverMethod method, IMessageProperties properties, ReadOnlySequence<byte> payload)
        {
            if (this.isCancelled)
            {
                return true;
            }

            var message = new DeliveredMessage(method.Exchange, method.RoutingKey, method.Redelivered, method.ConsumerTag, method.DeliveryTag);
            this.contentAccessor.Set(payload);

            try
            {
                for (var i = 0; i < this.handlers.Count; i++)
                {
                    var handled = await this.handlers[i].Invoke(message, properties, this.contentAccessor);

                    if (handled)
                    {
                        await this.AckAsync(message);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                await this.ProcessPoisonMessageAsync(message, properties, payload, ex);
                return true;
            }
            finally
            {
                this.contentAccessor.Reset();
            }

            await this.ProcessUnprocessedMessageAsync(message, properties, payload);
            return true;
        }

        private ValueTask AckAsync(DeliveredMessage message)
        {
            if (this.acknowledger == null)
            {
                return default;
            }

            return this.acknowledger.AckAsync(message.DeliveryTag);
        }

        private ValueTask NackAsync(DeliveredMessage message, UnprocessedMessageMode mode)
        {
            if (this.acknowledger == null)
            {
                return default;
            }

            var requeue = (mode | UnprocessedMessageMode.Requeue) == UnprocessedMessageMode.Requeue;
            return this.acknowledger.NackAsync(message.DeliveryTag, requeue);
        }

        private async ValueTask ProcessPoisonMessageAsync(DeliveredMessage message, IMessageProperties properties, ReadOnlySequence<byte> payload, Exception ex)
        {
            await this.NackAsync(message, this.onPoisonMessage);
            if ((this.onPoisonMessage | UnprocessedMessageMode.StopConsumer) == UnprocessedMessageMode.StopConsumer)
            {
                await this.CancelConsumeAsync(new PoisonMessageException(message, new DetachedMessageProperties(properties), this.MakeDetachedContent(payload), ex));
            }
        }

        private async ValueTask ProcessUnprocessedMessageAsync(DeliveredMessage message, IMessageProperties properties, ReadOnlySequence<byte> payload)
        {
            await this.NackAsync(message, this.onUnprocessedMessage);
            if ((this.onUnprocessedMessage | UnprocessedMessageMode.StopConsumer) == UnprocessedMessageMode.StopConsumer)
            {
                await this.CancelConsumeAsync(new UnprocessedMessageException(message, new DetachedMessageProperties(properties), this.MakeDetachedContent(payload)));
            }
        }

        private IContentAccessor MakeDetachedContent(ReadOnlySequence<byte> payload)
        {
            var detachedPayload = new byte[payload.Length];
            payload.CopyTo(detachedPayload);
            var content = new ContentAccessor(this.serializer);
            content.Set(new ReadOnlySequence<byte>(detachedPayload));
            return content;
        }
    }
}