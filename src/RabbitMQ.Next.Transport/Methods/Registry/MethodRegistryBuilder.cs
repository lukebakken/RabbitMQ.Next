using System;
using System.Collections.Generic;

namespace RabbitMQ.Next.Transport.Methods.Registry
{
    internal class MethodRegistryBuilder : IMethodRegistryBuilder
    {
        private readonly List<IMethodRegistration> items;

        public MethodRegistryBuilder()
        {
            this.items = new List<IMethodRegistration>();
        }

        public IMethodRegistryBuilder Register<TMethod>(uint methodId, Action<IMethodRegistrationBuilder<TMethod>> registration)
            where TMethod : struct, IMethod
        {
            var info = new MethodRegistration<TMethod>(methodId);
            registration(info);
            this.items.Add(info);

            return this;
        }

        public IMethodRegistry Build() => new MethodRegistry(this.items);
    }
}