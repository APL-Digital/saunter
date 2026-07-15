using System;
using ByteBard.AsyncAPI.Bindings;
using ByteBard.AsyncAPI.Readers.ParseNodes;
using ByteBard.AsyncAPI.Writers;

namespace Saunter.Bindings.AMQP;

/// <summary>
/// AsyncAPI AMQP 0-9-1 defines an empty server binding object reserved for future use.
/// This type exists so Saunter can serialize `bindings: { amqp: {} }` for AsyncAPI 3.0.0 documents.
/// </summary>
public sealed class AMQPServerBinding : ServerBinding<AMQPServerBinding>
{
    private static readonly FixedFieldMap<AMQPServerBinding> FixedFields = new();

    /// <inheritdoc/>
    public override string BindingKey => "amqp";

    /// <inheritdoc/>
    protected override FixedFieldMap<AMQPServerBinding> FixedFieldMap => FixedFields;

    /// <inheritdoc/>
    public override void SerializeProperties(IAsyncApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteEndObject();
    }
}
