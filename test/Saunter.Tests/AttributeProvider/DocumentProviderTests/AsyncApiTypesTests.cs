using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.Options;
using Saunter.Tests.MarkerTypeTests;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.DocumentProviderTests
{
    public class AsyncApiTypesTests
    {
        [Fact]
        public void GetDocument_GeneratesDocumentWithMultipleMessagesPerChannel()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(o =>
            {
                o.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = GetType().FullName,
                        Version = "1.0.0"
                    },
                };
                o.AssemblyMarkerTypes = new[] { typeof(AnotherSamplePublisher), typeof(SampleConsumer) };
            });

            using var serviceprovider = services.BuildServiceProvider();

            var documentProvider = serviceprovider.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = serviceprovider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;
            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            document.Channels.ShouldContainKey("asw.sample_service.anothersample");
            document.Operations.ShouldContainKey("AnotherSampleMessagePublisher");
            document.Operations.ShouldContainKey("SampleMessageConsumer");
        }
    }
}
