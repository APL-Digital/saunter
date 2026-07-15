using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options.Filters;

namespace MultiDocument.Filters;

/// <summary>
/// Adds an "environment" tag to every generated channel.
/// Registered via <c>options.AddAsyncApiChannelFilter&lt;EnvironmentTagChannelFilter&gt;()</c>.
/// </summary>
public class EnvironmentTagChannelFilter : IChannelFilter
{
    public void Apply(AsyncApiChannelDescriptor channel, ChannelFilterContext context)
    {
        channel.Tags.Add(new AsyncApiTag { Name = "env:development" });
    }
}
