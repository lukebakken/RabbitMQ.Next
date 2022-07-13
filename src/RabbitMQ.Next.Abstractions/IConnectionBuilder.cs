using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Methods;
using RabbitMQ.Next.Serialization;

namespace RabbitMQ.Next;

public interface IConnectionBuilder
{
    IConnectionBuilder Auth(IAuthMechanism mechanism);

    IConnectionBuilder VirtualHost(string vhost);

    IConnectionBuilder Endpoint(string host, int port, bool ssl = false);

    IConnectionBuilder ConfigureMethodRegistry(Action<IMethodRegistryBuilder> builder);
        
    IConnectionBuilder ConfigureSerialization(Action<ISerializationBuilder> builder);

    IConnectionBuilder ClientProperty(string key, object value);

    IConnectionBuilder Locale(string locale);

    IConnectionBuilder MaxFrameSize(int sizeBytes);

    Task<IConnection> ConnectAsync(CancellationToken cancellation = default);
}