using System.Linq;
using Saunter.AttributeProvider.Attributes;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    public class ChannelTagAttributeTests
    {
        [Fact]
        public void GenerateDocument_MapsRichChannelTagsFromAttributes()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(RichChannelTagsPublisher));

            var document = documentProvider.GetDocument(null, options);

            var channel = document.AssertAndGetChannel("tenant.tags.rich", "tenant.tags.rich");
            var billing = channel.Tags.Single(tag => tag.Name == "billing");
            billing.Description.ShouldBe("Billing lifecycle events");
            billing.ExternalDocs.ShouldNotBeNull();
            billing.ExternalDocs.Url.ShouldBe(new System.Uri("https://example.com/tags/billing"));
        }

        [AsyncApi]
        public class RichChannelTagsPublisher
        {
            [Channel("tenant.tags.rich", "tenant.tags.rich", Tags = new[] { "events" })]
            [ChannelTag("billing", Description = "Billing lifecycle events", ExternalDocs = "https://example.com/tags/billing")]
            [SendOperation(typeof(string))]
            public void Publish()
            {
            }
        }
    }
}
