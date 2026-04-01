using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Namotion.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;

namespace Saunter.AttributeProvider
{
    internal class AttributeOperationBuilder : IAttributeOperationBuilder
    {
        public AsyncApiOperationDescriptor Build(MemberInfo member, OperationAttribute operationAttribute, string channelId, IReadOnlyList<string> messageIds)
        {
            return new AsyncApiOperationDescriptor(
                operationAttribute.Action,
                channelId,
                operationAttribute.Title,
                operationAttribute.Summary ?? member.GetXmlDocsSummary(),
                operationAttribute.Description ?? (member.GetXmlDocsRemarks() != string.Empty ? member.GetXmlDocsRemarks() : null),
                operationAttribute.BindingsRef,
                messageIds.ToArray(),
                operationAttribute.Tags ?? Array.Empty<string>(),
                CreateOperationReply(operationAttribute, messageIds));
        }

        private static AsyncApiOperationReplyDescriptor? CreateOperationReply(OperationAttribute operationAttribute, IReadOnlyList<string> messageIds)
        {
            if (string.IsNullOrWhiteSpace(operationAttribute.Reply)
                && string.IsNullOrWhiteSpace(operationAttribute.ReplyAddressLocation))
            {
                return null;
            }

            return new AsyncApiOperationReplyDescriptor(
                operationAttribute.Reply,
                operationAttribute.ReplyAddressLocation,
                operationAttribute.ReplyAddressDescription)
            {
                MessageIds = messageIds.ToList(),
            };
        }
    }
}
