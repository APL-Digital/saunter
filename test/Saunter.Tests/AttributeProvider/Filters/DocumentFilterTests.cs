using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options.Filters;
using Saunter.Tests.AttributeProvider.DocumentGenerationTests;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.Filters
{
    public class DocumentFilterTests
    {
        [Fact]
        public void DocumentFilterIsAppliedToAsyncApiDocument()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, GetType());

            options.AddDocumentFilter<ExampleDocumentFilter>();

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            document.Channels.ShouldContainKey("foo");
            document.Operations.ShouldContainKey("foo.operation");
        }

        [Fact]
        public void DocumentNameIsAppliedToAsyncApiDocument()
        {
            const string documentName = "Test Document";

            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, GetType());

            options.NamedApis[documentName] = new();
            options.AddDocumentFilter<ExampleDocumentFilter>();

            var document = documentProvider.GetDocument(documentName, options);

            document.ShouldNotBeNull();
        }

        [Fact]
        public void DocumentFilterInstanceIsAppliedWithoutReinstantiation()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, GetType());

            var filterInstance = new CountingDocumentFilter { ChannelId = "instance-channel" };
            options.AddDocumentFilter(filterInstance);

            var document = documentProvider.GetDocument(null, options);

            document.Channels.ShouldContainKey("instance-channel");
            filterInstance.ApplyCount.ShouldBe(1);
        }

        [Fact]
        public void ChannelAndOperationFilterInstancesAreApplied()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(AnnotatedPublisher));

            var channelFilter = new CountingChannelFilter();
            var operationFilter = new CountingOperationFilter();
            options.AddChannelFilter(channelFilter);
            options.AddOperationFilter(operationFilter);

            _ = documentProvider.GetDocument(null, options);

            channelFilter.ApplyCount.ShouldBeGreaterThan(0);
            operationFilter.ApplyCount.ShouldBeGreaterThan(0);
        }

        [Saunter.AttributeProvider.Attributes.AsyncApi]
        private class AnnotatedPublisher
        {
            [Saunter.AttributeProvider.Attributes.Channel("filters.instance.test", "filters.instance.test")]
            [Saunter.AttributeProvider.Attributes.SendOperation(OperationId = "filtersInstanceTestPublish")]
            public void Publish(string message) { _ = message; }
        }

        private class CountingDocumentFilter : IDocumentFilter
        {
            public string ChannelId { get; set; } = "counting";
            public int ApplyCount { get; private set; }

            public void Apply(AsyncApiDocumentDescriptor document, DocumentFilterContext context)
            {
                ApplyCount++;
                document.Channels[ChannelId] = new AsyncApiChannelDescriptor(
                    ChannelId,
                    ChannelId,
                    null,
                    null,
                    null,
                    null,
                    [],
                    [],
                    []);
            }
        }

        private class CountingChannelFilter : IChannelFilter
        {
            public int ApplyCount { get; private set; }

            public void Apply(AsyncApiChannelDescriptor channel, ChannelFilterContext context)
            {
                ApplyCount++;
            }
        }

        private class CountingOperationFilter : IOperationFilter
        {
            public int ApplyCount { get; private set; }

            public void Apply(AsyncApiOperationDescriptor operation, OperationFilterContext context)
            {
                ApplyCount++;
            }
        }

        private class ExampleDocumentFilter : IDocumentFilter
        {
            public void Apply(AsyncApiDocumentDescriptor document, DocumentFilterContext context)
            {
                document.Channels["foo"] = new AsyncApiChannelDescriptor(
                    "foo",
                    "foo",
                    null,
                    null,
                    "an example channel for testing",
                    null,
                    [],
                    [],
                    []);

                document.Operations["foo.operation"] = new AsyncApiOperationDescriptor(
                    AsyncApiAction.Send,
                    "foo",
                    null,
                    null,
                    null,
                    null,
                    [],
                    [],
                    null);
            }
        }
    }
}
