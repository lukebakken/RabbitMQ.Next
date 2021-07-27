using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions;
using RabbitMQ.Next.Abstractions.Methods;
using RabbitMQ.Next.Buffers;
using RabbitMQ.Next.Transport;
using RabbitMQ.Next.Transport.Methods.Registry;

namespace RabbitMQ.Next
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        private const string DefaultLocale = "en-US";

        private readonly IConnectionFactory factory;
        private readonly IMethodRegistryBuilder methodRegistry = new MethodRegistryBuilder();
        private readonly List<Endpoint> endpoints = new List<Endpoint>();
        private readonly Dictionary<string, object> clientProperties = new Dictionary<string, object>();
        private IAuthMechanism authMechanism;
        private string virtualhost = ProtocolConstants.DefaultVHost;
        private string locale = DefaultLocale;
        private int frameSize = 102_400;
        private int bufferPoolSize = 100;

        internal ConnectionBuilder(IConnectionFactory factory)
        {
            this.factory = factory;

            this.clientProperties["capabilities"] = new Dictionary<string, object>
            {
                ["authentication_failure_close"] = true,
            };
        }

        public static IConnectionBuilder Default()
        {
            var builder = new ConnectionBuilder(ConnectionFactory.Default);
            builder.UseDefaults();
            return builder;
        }

        public IConnectionBuilder Auth(IAuthMechanism mechanism)
        {
            this.authMechanism = mechanism;
            return this;
        }

        public IConnectionBuilder VirtualHost(string vhost)
        {
            this.virtualhost = vhost;
            return this;
        }

        public IConnectionBuilder AddEndpoint(string host, int port, bool ssl)
        {
            this.endpoints.Add(new Endpoint(host, port, ssl));
            return this;
        }

        public IConnectionBuilder ConfigureMethodRegistry(Action<IMethodRegistryBuilder> builder)
        {
            builder?.Invoke(this.methodRegistry);
            return this;
        }

        public  IConnectionBuilder ClientProperty(string key, object value)
        {
            this.clientProperties[key] = value;
            return this;
        }

        public IConnectionBuilder Locale(string locale)
        {
            this.locale = locale;
            return this;
        }

        public IConnectionBuilder FrameSize(int sizeBytes)
        {
            if (sizeBytes < ProtocolConstants.FrameMinSize)
            {
                throw new ArgumentException("FrameSize cannot be smaller then minimal frame size", nameof(sizeBytes));
            }

            this.frameSize = sizeBytes;
            return this;
        }

        public Task<IConnection> ConnectAsync()
        {
            return this.factory.ConnectAsync(
                this.endpoints.ToArray(),
                this.virtualhost,
                this.authMechanism,
                this.locale,
                this.clientProperties,
                this.methodRegistry.Build(),
                this.frameSize);
        }
    }
}