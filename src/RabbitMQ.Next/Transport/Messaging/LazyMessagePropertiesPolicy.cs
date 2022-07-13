using Microsoft.Extensions.ObjectPool;

namespace RabbitMQ.Next.Transport.Messaging;

internal class LazyMessagePropertiesPolicy: PooledObjectPolicy<LazyMessageProperties>
{
    public override LazyMessageProperties Create() => new();

    public override bool Return(LazyMessageProperties obj)
    {
        obj.Reset();
        return true;
    }
}