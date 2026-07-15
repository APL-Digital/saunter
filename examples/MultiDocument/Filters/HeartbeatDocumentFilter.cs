using ByteBard.AsyncAPI.Models;
using Saunter;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options.Filters;

namespace MultiDocument.Filters;

/// <summary>
/// Adds a heartbeat channel and operation that no annotated type declares.
/// Registered via <c>options.AddDocumentFilter&lt;HeartbeatDocumentFilter&gt;()</c>.
/// </summary>
public class HeartbeatDocumentFilter : IDocumentFilter
{
    public void Apply(AsyncApiDocumentDescriptor document, DocumentFilterContext context)
    {
        document.Channels["heartbeat"] = new AsyncApiChannelDescriptor(
            "heartbeat",
            "system.heartbeat",
            null,
            "Liveness heartbeat",
            "Emitted every 30 seconds by every service instance.",
            null,
            [],
            [],
            []);

        document.Operations["EmitHeartbeat"] = new AsyncApiOperationDescriptor(
            AsyncApiAction.Send,
            "heartbeat",
            null,
            "Emit liveness heartbeat",
            null,
            null,
            [],
            [],
            null);
    }
}
