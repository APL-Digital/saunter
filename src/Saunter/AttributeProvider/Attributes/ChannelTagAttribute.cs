using System;

namespace Saunter.AttributeProvider.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class ChannelTagAttribute : Attribute
    {
        public ChannelTagAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public string? Description { get; set; }

        public string? ExternalDocs { get; set; }

        public string? ExternalDocsDescription { get; set; }
    }
}
