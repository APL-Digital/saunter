using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter
{
    public class AsyncApiServerDescriptor
    {
        public string? Host { get; set; }

        public string? PathName { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public string? Description { get; set; }

        public string? Protocol { get; set; }

        public string? ProtocolVersion { get; set; }

        public string? ExternalDocs { get; set; }

        public string? ExternalDocsDescription { get; set; }

        public string? BindingsRef { get; set; }

        public IList<AsyncApiTag> Tags { get; set; } = new List<AsyncApiTag>();

        public IDictionary<string, AsyncApiServerVariableDescriptor> Variables { get; set; } = new Dictionary<string, AsyncApiServerVariableDescriptor>();

        public IList<AsyncApiSecurityScheme> Security { get; set; } = new List<AsyncApiSecurityScheme>();

        public AsyncApiBindings<IServerBinding> Bindings { get; set; } = new AsyncApiBindings<IServerBinding>();
    }

    public class AsyncApiServerVariableDescriptor
    {
        public string? Default { get; set; }

        public string? Description { get; set; }

        public IList<string> Enum { get; set; } = new List<string>();

        public IList<string> Examples { get; set; } = new List<string>();
    }
}
