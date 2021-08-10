using System;
using RabbitMQ.Next.Publisher.Abstractions;

namespace RabbitMQ.Next.Publisher.Attributes
{
    [AttributeUsage(AttributeTargets.Class| AttributeTargets.Assembly)]
    public class ContentEncodingAttribute : MessageAttributeBase
    {
        public ContentEncodingAttribute(string contentEncoding)
        {
            if (string.IsNullOrWhiteSpace(contentEncoding))
            {
                throw new ArgumentNullException(nameof(contentEncoding));
            }

            this.ContentEncoding = contentEncoding;
        }

        public string ContentEncoding { get; }

        public override void Apply(IMessageBuilder message)
            => message.ContentEncoding(this.ContentEncoding);
    }
}