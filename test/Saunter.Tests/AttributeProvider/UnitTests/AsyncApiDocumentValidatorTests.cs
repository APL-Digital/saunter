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
        public void Validate_ThrowsWhenReplyReferencesUnknownReplyChannelMessage()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Components = new AsyncApiComponentsDescriptor
                {
                    Messages =
                    {
                        ["orderCreated"] = new AsyncApiMessageDescriptor("orderCreated", "orderCreated", "Order Created", null, null, null, null, null, null, null, null, null, []),
                        ["orderAccepted"] = new AsyncApiMessageDescriptor("orderAccepted", "orderAccepted", "Order Accepted", null, null, null, null, null, null, null, null, null, [])
                    }
                },
                Channels =
                {
                    ["orders"] = new AsyncApiChannelDescriptor("orders", "orders", null, null, null, null, [], ["orderCreated"], []),
                    ["orders.reply"] = new AsyncApiChannelDescriptor("orders.reply", "orders.reply", null, null, null, null, [], [], [])
                },
                Operations =
                {
                    ["publishOrder"] = new AsyncApiOperationDescriptor(
                        AsyncApiAction.Send,
                        "orders",
                        null,
                        null,
                        null,
                        null,
                        ["orderCreated"],
                        [],
                        new AsyncApiOperationReplyDescriptor("orders.reply", null, null)
                        {
                            MessageIds = ["orderAccepted"]
                        })
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("reply channel message");
        }

        [Fact]
        public void Validate_ThrowsWhenReplyAddressTargetsChannelWithConcreteAddress()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Components = new AsyncApiComponentsDescriptor
                {
                    Messages =
                    {
                        ["orderCreated"] = new AsyncApiMessageDescriptor("orderCreated", "orderCreated", "Order Created", null, null, null, null, null, null, null, null, null, [])
                    }
                },
                Channels =
                {
                    ["orders"] = new AsyncApiChannelDescriptor("orders", "orders", null, null, null, null, [], ["orderCreated"], []),
                    ["orders.reply"] = new AsyncApiChannelDescriptor("orders.reply", "orders.reply", null, null, null, null, [], ["orderCreated"], [])
                },
                Operations =
                {
                    ["publishOrder"] = new AsyncApiOperationDescriptor(
                        AsyncApiAction.Send,
                        "orders",
                        null,
                        null,
                        null,
                        null,
                        ["orderCreated"],
                        [],
                        new AsyncApiOperationReplyDescriptor("orders.reply", "$message.header#/replyTo", null)
                        {
                            MessageIds = ["orderCreated"]
                        })
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("must be null or absent");
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

        [Fact]
        public void Validate_ThrowsWhenChannelReferencesUnknownMessage()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Channels =
                {
                    ["orders"] = new AsyncApiChannelDescriptor("orders", "orders", null, null, null, null, [], ["missingMessage"], [])
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("components/messages");
        }

        [Fact]
        public void Validate_ThrowsWhenOperationReferencesUnknownChannelMessage()
        {
            var validator = new AsyncApiDocumentValidator();
            var document = new AsyncApiDocumentDescriptor
            {
                Components = new AsyncApiComponentsDescriptor
                {
                    Messages =
                    {
                        ["orderCreated"] = new AsyncApiMessageDescriptor("orderCreated", "orderCreated", "Order Created", null, null, null, null, null, null, null, null, null, [])
                    }
                },
                Channels =
                {
                    ["orders"] = new AsyncApiChannelDescriptor("orders", "orders", null, null, null, null, [], ["orderCreated"], [])
                },
                Operations =
                {
                    ["publishOrder"] = new AsyncApiOperationDescriptor(AsyncApiAction.Send, "orders", null, null, null, null, ["missingMessage"], [], null)
                }
            };

            var actual = () => validator.Validate(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("unknown channel message");
        }
    }
}
