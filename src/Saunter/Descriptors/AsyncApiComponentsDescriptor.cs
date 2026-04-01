using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel.Descriptors;

namespace Saunter
{
    public class AsyncApiComponentsDescriptor
    {
        public IDictionary<string, AsyncApiSchemaDescriptor> Schemas { get; set; } = new Dictionary<string, AsyncApiSchemaDescriptor>();

        public IDictionary<string, AsyncApiMessageDescriptor> Messages { get; set; } = new Dictionary<string, AsyncApiMessageDescriptor>();

        public IDictionary<string, AsyncApiParameterDescriptor> Parameters { get; set; } = new Dictionary<string, AsyncApiParameterDescriptor>();

        public IDictionary<string, AsyncApiBindings<IOperationBinding>> OperationBindings { get; set; } = new Dictionary<string, AsyncApiBindings<IOperationBinding>>();

        public IDictionary<string, AsyncApiBindings<IMessageBinding>> MessageBindings { get; set; } = new Dictionary<string, AsyncApiBindings<IMessageBinding>>();

        public IDictionary<string, AsyncApiBindings<IChannelBinding>> ChannelBindings { get; set; } = new Dictionary<string, AsyncApiBindings<IChannelBinding>>();

        public IDictionary<string, AsyncApiOperationTrait> OperationTraits { get; set; } = new Dictionary<string, AsyncApiOperationTrait>();

        public IDictionary<string, AsyncApiCorrelationId> CorrelationIds { get; set; } = new Dictionary<string, AsyncApiCorrelationId>();

        public IDictionary<string, AsyncApiSecurityScheme> SecuritySchemes { get; set; } = new Dictionary<string, AsyncApiSecurityScheme>();
    }
}
