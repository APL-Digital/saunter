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
                .Message.ShouldContain("unknown server");
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
                .Message.ShouldContain("unknown channel");
        }
    }
}
