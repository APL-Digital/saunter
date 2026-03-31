using System;
using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Descriptors;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.UnitTests
{
    public class AsyncApiDocumentValidatorTests
    {
        [Fact]
        public void Validate_ThrowsWhenChannelReferencesUnknownServer()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Channels =
                {
                    ["orders"] = new AsyncApiChannelDescriptor("orders", "orders", null, null, null, null, ["missing"], [], [])
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Add the server");
        }

        [Fact]
        public void Validate_ThrowsWhenReplyReferencesUnknownChannel()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Channels =
                {
                    ["orders"] = new AsyncApiChannelDescriptor("orders", "orders", null, null, null, null, [], [], [])
                },
                Operations =
                {
                    ["publishOrder"] = new AsyncApiOperationDescriptor(AsyncApiAction.Send, "orders", null, null, null, null, [], [], new AsyncApiOperationReplyDescriptor("missing", null, null))
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Add the reply channel");
        }

        [Fact]
        public void Validate_ThrowsWhenOperationBindingReferenceIsUnknown()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Channels =
                {
                    ["orders"] = new AsyncApiChannelDescriptor("orders", "orders", null, null, null, null, [], [], [])
                },
                Operations =
                {
                    ["publishOrder"] = new AsyncApiOperationDescriptor(AsyncApiAction.Send, "orders", null, null, null, "missingBinding", [], [], null)
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("operation binding");
        }

        [Fact]
        public void Validate_ThrowsWhenMessageCorrelationReferenceIsUnknown()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Components = new AsyncApiComponentsDescriptor
                {
                    Messages =
                    {
                        ["orderCreated"] = new AsyncApiMessageDescriptor("orderCreated", "orderCreated", "Order Created", null, null, null, null, "missingCorrelation", null, null, null, null, [])
                    }
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("correlation id");
        }
    }
}
