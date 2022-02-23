using NSubstitute;
using RabbitMQ.Next.Publisher;
using RabbitMQ.Next.Publisher.Attributes;
using Xunit;

[assembly:ApplicationId("unitTest")]

namespace RabbitMQ.Next.Tests.Publisher.Attributes
{
    public class AttributeTransformerTests
    {
        [Fact]
        public void TestAssemblyAttributes()
        {
            var attributeTransformer = new AttributeInitializer();
            var message = Substitute.For<IMessageBuilder>();

            attributeTransformer.Apply(new AssemblyAttributesData(), message);

            message.Received().ApplicationId("unitTest");
        }

        [Fact]
        public void TestRoutingKeyAttribute()
        {
            var attributeTransformer = new AttributeInitializer();
            var message = Substitute.For<IMessageBuilder>();

            attributeTransformer.Apply(new RoutingKeyAttributeData(), message);

            message.Received().RoutingKey("route");
        }

        [Fact]
        public void TestHeaderAttribute()
        {
            var attributeTransformer = new AttributeInitializer();
            var message = Substitute.For<IMessageBuilder>();

            attributeTransformer.Apply(new HeaderAttributeData(), message);

            message.Received().SetHeader("headerA", "value1");
            message.Received().SetHeader("headerB", "value2");
        }

        [Fact]
        public void TestMultipleAttributes()
        {
            var attributeTransformer = new AttributeInitializer();
            var message = Substitute.For<IMessageBuilder>();

            attributeTransformer.Apply(new MultipleAttributesData(), message);

            message.Received().RoutingKey("route");
            message.Received().Priority(7);
        }

        private class AssemblyAttributesData
        {

        }

        [RoutingKey("route")]
        private class RoutingKeyAttributeData
        {
        }

        [Header("headerA", "value1")]
        [Header("headerB", "value2")]
        private class HeaderAttributeData
        {
        }

        [RoutingKey("route")]
        [Priority(7)]
        private class MultipleAttributesData
        {
        }
    }
}