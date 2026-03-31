using System;
using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class ChannelUnionTests
    {
        public static IEnumerable<object[]> GetOnUnionConflictData()
        {
            yield return new AsyncApiChannel[]
            {
                new() { Address = "foo", Messages = new Dictionary<string, AsyncApiMessage>() },
                new() { Address = "bar", Messages = new Dictionary<string, AsyncApiMessage>() },
            };
        }

        [Theory]
        [MemberData(nameof(GetOnUnionConflictData))]
        public void AsyncApiChannelUnion_OnUnion_Conflict(AsyncApiChannel source, AsyncApiChannel additionaly)
        {
            var channelUnion = new AsyncApiChannelUnion();
            var actual = () => channelUnion.Union(source, additionaly);

            Should.Throw<InvalidOperationException>(actual);
        }

        [Fact]
        public void AsyncApiChannelUnion_OnUnion_SuccessMerge()
        {
            var source = new AsyncApiChannel
            {
                Address = "foo",
                Description = "description",
                Messages = new Dictionary<string, AsyncApiMessage>
                {
                    ["one"] = new AsyncApiMessageReference("#/components/messages/one")
                },
                Parameters = new Dictionary<string, AsyncApiParameter>
                {
                    ["tenant_id"] = new AsyncApiParameter { Description = "description", Location = "tenant_id" }
                }
            };

            var additionaly = new AsyncApiChannel
            {
                Address = "foo",
                Summary = "summary",
                Messages = new Dictionary<string, AsyncApiMessage>
                {
                    ["two"] = new AsyncApiMessageReference("#/components/messages/two")
                }
            };

            var channelUnion = new AsyncApiChannelUnion();
            var actual = channelUnion.Union(source, additionaly);

            actual.ShouldNotBeNull();
            actual.Address.ShouldBe("foo");
            actual.Description.ShouldBe("description");
            actual.Summary.ShouldBe("summary");
            actual.Messages.ShouldContainKey("one");
            actual.Messages.ShouldContainKey("two");
            actual.Parameters.ShouldContainKey("tenant_id");
        }
    }
}
