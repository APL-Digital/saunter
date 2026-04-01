using System.Linq;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Attributes;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.UnitTests
{
    public class AttributeOperationBuilderTests
    {
        [Fact]
        public void Build_ReturnsDescriptorWithReplyMetadata()
        {
            var builder = new AttributeOperationBuilder();
            var member = typeof(OperationFixture).GetMethod(nameof(OperationFixture.Publish))!;
            var attribute = new SendOperationAttribute
            {
                OperationId = "publishOrder",
                Reply = "orders.reply",
                ReplyAddressLocation = "$message.header#/replyTo",
                ReplyAddressDescription = "Dynamic reply target"
            };
            var operation = builder.Build(member, attribute, "orders", ["orderCreated"]);

            operation.ChannelId.ShouldBe("orders");
            operation.MessageIds.Single().ShouldBe("orderCreated");
            operation.Reply.ShouldNotBeNull();
            operation.Reply.ChannelId.ShouldBe("orders.reply");
            operation.Reply.AddressLocation.ShouldBe("$message.header#/replyTo");
            operation.Reply.AddressDescription.ShouldBe("Dynamic reply target");
            operation.Reply.MessageIds.ShouldBe(["orderCreated"]);
        }

        private class OperationFixture
        {
            public void Publish()
            {
            }
        }
    }
}
