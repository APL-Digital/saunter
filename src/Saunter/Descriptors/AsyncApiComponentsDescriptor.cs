using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel.Descriptors;

namespace Saunter
{
    /// <summary>
    /// Describes the AsyncAPI <c>components</c> object: reusable items referenced from elsewhere in the
    /// document. Bindings, correlation ids and traits registered here can be referenced by name from
    /// attributes via <c>BindingsRef</c>, <c>CorrelationId</c> and trait references.
    /// </summary>
    public class AsyncApiComponentsDescriptor
    {
        /// <summary>
        /// The AsyncAPI <c>components.schemas</c> map: reusable payload and header schemas, keyed by schema id.
        /// Populated automatically from message payload/header types during generation.
        /// </summary>
        public IDictionary<string, AsyncApiSchemaDescriptor> Schemas { get; set; } = new Dictionary<string, AsyncApiSchemaDescriptor>();

        /// <summary>
        /// The AsyncAPI <c>components.messages</c> map: reusable message definitions, keyed by message id.
        /// Populated automatically from message attributes and inferred payload types during generation.
        /// </summary>
        public IDictionary<string, AsyncApiMessageDescriptor> Messages { get; set; } = new Dictionary<string, AsyncApiMessageDescriptor>();

        /// <summary>
        /// The AsyncAPI <c>components.parameters</c> map: reusable channel parameters, keyed by parameter name.
        /// </summary>
        public IDictionary<string, AsyncApiParameterDescriptor> Parameters { get; set; } = new Dictionary<string, AsyncApiParameterDescriptor>();

        /// <summary>
        /// The AsyncAPI <c>components.serverBindings</c> map: reusable server bindings, keyed by the name
        /// used in <see cref="AsyncApiServerDescriptor.BindingsRef"/>.
        /// </summary>
        public IDictionary<string, AsyncApiBindings<IServerBinding>> ServerBindings { get; set; } = new Dictionary<string, AsyncApiBindings<IServerBinding>>();

        /// <summary>
        /// The AsyncAPI <c>components.operationBindings</c> map: reusable operation bindings, keyed by the name
        /// used in an operation attribute's <c>BindingsRef</c>.
        /// </summary>
        public IDictionary<string, AsyncApiBindings<IOperationBinding>> OperationBindings { get; set; } = new Dictionary<string, AsyncApiBindings<IOperationBinding>>();

        /// <summary>
        /// The AsyncAPI <c>components.messageBindings</c> map: reusable message bindings, keyed by the name
        /// used in a message attribute's <c>BindingsRef</c>.
        /// </summary>
        public IDictionary<string, AsyncApiBindings<IMessageBinding>> MessageBindings { get; set; } = new Dictionary<string, AsyncApiBindings<IMessageBinding>>();

        /// <summary>
        /// The AsyncAPI <c>components.channelBindings</c> map: reusable channel bindings, keyed by the name
        /// used in a channel attribute's <c>BindingsRef</c>.
        /// </summary>
        public IDictionary<string, AsyncApiBindings<IChannelBinding>> ChannelBindings { get; set; } = new Dictionary<string, AsyncApiBindings<IChannelBinding>>();

        /// <summary>
        /// The AsyncAPI <c>components.operationTraits</c> map: reusable operation traits, keyed by the name
        /// referenced from an operation's trait references.
        /// </summary>
        public IDictionary<string, AsyncApiOperationTrait> OperationTraits { get; set; } = new Dictionary<string, AsyncApiOperationTrait>();

        /// <summary>
        /// The AsyncAPI <c>components.correlationIds</c> map: reusable correlation id definitions, keyed by the
        /// name used in a message attribute's <c>CorrelationId</c>.
        /// </summary>
        public IDictionary<string, AsyncApiCorrelationId> CorrelationIds { get; set; } = new Dictionary<string, AsyncApiCorrelationId>();

        /// <summary>
        /// The AsyncAPI <c>components.securitySchemes</c> map: reusable security schemes, keyed by scheme name.
        /// </summary>
        public IDictionary<string, AsyncApiSecurityScheme> SecuritySchemes { get; set; } = new Dictionary<string, AsyncApiSecurityScheme>();
    }
}
