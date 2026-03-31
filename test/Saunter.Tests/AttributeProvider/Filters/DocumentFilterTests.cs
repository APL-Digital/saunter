using ByteBard.AsyncAPI.Models;
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

        private class ExampleDocumentFilter : IDocumentFilter
        {
            public void Apply(AsyncApiDocument document, DocumentFilterContext context)
            {
                document.Channels["foo"] = new AsyncApiChannel
                {
                    Address = "foo",
                    Description = "an example channel for testing",
                };

                document.Operations["foo.operation"] = new AsyncApiOperation
                {
                    Action = AsyncApiAction.Send,
                    Channel = new AsyncApiChannelReference("#/channels/foo"),
                };
            }
        }
    }
}
