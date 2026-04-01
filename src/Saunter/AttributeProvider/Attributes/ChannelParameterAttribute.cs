using System;

namespace Saunter.AttributeProvider.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class ChannelParameterAttribute : Attribute
    {
        public ChannelParameterAttribute(string name)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name))
                : name;
            Type = typeof(string);
        }

        public ChannelParameterAttribute(string name, Type type)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name))
                : name;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Name { get; }

        public Type Type { get; }

        public string? Description { get; set; }

        public string? Location { get; set; }

        public string? DefaultValue { get; set; }

        public string[] Examples { get; set; } = Array.Empty<string>();
    }
}
