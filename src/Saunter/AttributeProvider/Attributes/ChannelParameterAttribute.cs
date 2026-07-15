using System;

namespace Saunter.AttributeProvider.Attributes
{
    /// <summary>
    /// Describes a <c>{parameter}</c> segment of a channel address declared with <see cref="ChannelAttribute"/>.
    /// Apply one attribute per parameter alongside the channel attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class ChannelParameterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a <see cref="ChannelParameterAttribute"/> for a string-typed parameter.
        /// </summary>
        /// <param name="name">The parameter name as it appears in the channel address (without braces).</param>
        public ChannelParameterAttribute(string name)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("ChannelParameterAttribute name cannot be null, empty, or whitespace.", nameof(name))
                : name;
            Type = typeof(string);
        }

        /// <summary>
        /// Initializes a <see cref="ChannelParameterAttribute"/> with an explicit parameter type.
        /// </summary>
        /// <param name="name">The parameter name as it appears in the channel address (without braces).</param>
        /// <param name="type">The CLR type of the parameter; enum types contribute their names as the parameter's enum values.</param>
        public ChannelParameterAttribute(string name, Type type)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("ChannelParameterAttribute name cannot be null, empty, or whitespace.", nameof(name))
                : name;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// The parameter name as it appears in the channel address (without braces).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The CLR type of the parameter. Defaults to <see cref="string"/>; enum types contribute
        /// their names as the parameter's enum values.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// An optional description of the parameter.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// A runtime expression that locates the parameter value, e.g. <c>$message.header#/id</c>
        /// (AsyncAPI <c>parameter.location</c>).
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// The value to use for the parameter when none is supplied (AsyncAPI <c>parameter.default</c>).
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Example values for the parameter (AsyncAPI <c>parameter.examples</c>).
        /// </summary>
        public string[] Examples { get; set; } = Array.Empty<string>();
    }
}
