using System;

namespace Saunter.AttributeProvider.Attributes
{
    /// <summary>
    /// Adds a tag with optional description and external documentation to a channel declared with
    /// <see cref="ChannelAttribute"/>. Use this instead of <see cref="ChannelAttribute.Tags"/> when the
    /// tag needs more than a name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class ChannelTagAttribute : Attribute
    {
        /// <summary>
        /// Initializes a <see cref="ChannelTagAttribute"/> with the given tag name.
        /// </summary>
        /// <param name="name">The tag name.</param>
        public ChannelTagAttribute(string name)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("ChannelTagAttribute name cannot be null, empty, or whitespace.", nameof(name))
                : name;
        }

        /// <summary>
        /// The tag name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// An optional description of the tag.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// An absolute URL for additional documentation about the tag.
        /// </summary>
        public string? ExternalDocs { get; set; }

        /// <summary>
        /// Optional description for the external docs link.
        /// </summary>
        public string? ExternalDocsDescription { get; set; }
    }
}
