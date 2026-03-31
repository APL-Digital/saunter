using System;
using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class ChannelUnionTests
    {
        public static IEnumerable<object[]> GetOnUnionConflictData()
        {
            yield return new object[]
            {
                new AsyncApiChannelDescriptor("conflict", "foo", null, null, null, null, [], [], []),
                new AsyncApiChannelDescriptor("conflict", "bar", null, null, null, null, [], [], []),
            };
        }

        [Theory]
        [MemberData(nameof(GetOnUnionConflictData))]
        public void AsyncApiChannelUnion_OnUnion_Conflict(AsyncApiChannelDescriptor source, AsyncApiChannelDescriptor additional)
        {
            var channelUnion = new AsyncApiChannelUnion();
            var actual = () => channelUnion.Union(source, additional);

            Should.Throw<InvalidOperationException>(actual);
        }

        [Fact]
        public void AsyncApiChannelUnion_OnUnion_SuccessMerge()
        {
            var source = new AsyncApiChannelDescriptor(
                "orders",
                "foo",
                null,
                null,
                "description",
                null,
                [],
                ["one"],
                [new AsyncApiParameterDescriptor("tenant_id", "description", "tenant_id", [])]);

            var additional = new AsyncApiChannelDescriptor(
                "orders",
                "foo",
                null,
                "summary",
                null,
                null,
                [],
                ["two"],
                []);

            var channelUnion = new AsyncApiChannelUnion();
            var actual = channelUnion.Union(source, additional);

            actual.ShouldNotBeNull();
            actual.Address.ShouldBe("foo");
            actual.Description.ShouldBe("description");
            actual.Summary.ShouldBe("summary");
            actual.MessageIds.ShouldContain("one");
            actual.MessageIds.ShouldContain("two");
            actual.Parameters.ShouldContainKey("tenant_id");
        }

        [Fact]
        public void AsyncApiChannelUnion_OnUnion_DuplicateMembers_SourceShouldWin()
        {
            var source = new AsyncApiChannelDescriptor(
                "orders",
                "foo",
                null,
                null,
                null,
                null,
                [],
                ["one"],
                [new AsyncApiParameterDescriptor("tenant_id", "source", null, [])]);

            var additional = new AsyncApiChannelDescriptor(
                "orders",
                "foo",
                null,
                null,
                null,
                null,
                [],
                ["one"],
                [new AsyncApiParameterDescriptor("tenant_id", "additional", null, [])]);

            var channelUnion = new AsyncApiChannelUnion();
            var actual = channelUnion.Union(source, additional);

            actual.MessageIds.ShouldContain("one");
            actual.Parameters.Single(parameter => parameter.Name == "tenant_id").Description.ShouldBe("source");
        }

        [Fact]
        public void AsyncApiChannelUnion_OnUnion_IgnoresNullServerReferences()
        {
            var source = new AsyncApiChannelDescriptor("orders", "foo", null, null, null, null, [], [], []);
            var additional = new AsyncApiChannelDescriptor("orders", "foo", null, null, null, null, ["valid"], [], []);

            var channelUnion = new AsyncApiChannelUnion();
            var actual = channelUnion.Union(source, additional);

            actual.Servers.ShouldContain("valid");
        }

        [Fact]
        public void AsyncApiChannelUnion_OnUnion_DuplicateServersAndTags_SourceShouldWin()
        {
            var source = new AsyncApiChannelDescriptor("orders", "foo", null, null, null, null, ["shared"], [], []);
            source.Tags.Add(new AsyncApiTag { Name = "shared", Description = "source" });
            var additional = new AsyncApiChannelDescriptor("orders", "foo", null, null, null, null, ["shared"], [], []);
            additional.Tags.Add(new AsyncApiTag { Name = "shared", Description = "additional" });

            var channelUnion = new AsyncApiChannelUnion();
            var actual = channelUnion.Union(source, additional);

            actual.Servers.Single().ShouldBe("shared");
            actual.Tags.Single().Description.ShouldBe("source");
        }
    }
}
