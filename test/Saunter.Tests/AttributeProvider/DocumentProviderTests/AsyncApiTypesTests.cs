using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
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

        [Fact]
        public void GetDocument_ThrowsWithDetailedOperationConflict()
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
                o.AssemblyMarkerTypes = new[] { typeof(ConflictingPublishOne), typeof(ConflictingPublishTwo) };
            });

            using var serviceprovider = services.BuildServiceProvider();

            var documentProvider = serviceprovider.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = serviceprovider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            var actual = () => documentProvider.GetDocument(null, options);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Existing definition:");
        }

        [Fact]
        public void GetDocument_ThrowsWithDetailedOperationConflictAgainstPreconfiguredOperation()
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
                    Operations =
                    {
                        ["Publish"] = new AsyncApiOperationDescriptor(ByteBard.AsyncAPI.Models.AsyncApiAction.Send, "existingChannel", null, null, null, null, [], [], null)
                    }
                };
                o.AssemblyMarkerTypes = new[] { typeof(PreconfiguredConflictPublisher) };
            });

            using var serviceprovider = services.BuildServiceProvider();

            var documentProvider = serviceprovider.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = serviceprovider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            var actual = () => documentProvider.GetDocument(null, options);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("preconfigured document operation");
        }

        [AsyncApi]
        private class ConflictingPublishOne
        {
            [Channel("orders.created", "orders.created")]
            [SendOperation]
            [Message(typeof(ConflictPayload))]
            public void Publish()
            {
            }
        }

        [AsyncApi]
        private class ConflictingPublishTwo
        {
            [Channel("orders.updated", "orders.updated")]
            [SendOperation]
            [Message(typeof(ConflictPayload))]
            public void Publish()
            {
            }
        }

        private class ConflictPayload
        {
            public string Id { get; set; } = string.Empty;
        }

        [AsyncApi]
        private class PreconfiguredConflictPublisher
        {
            [Channel("orders.preconfigured", "orders.preconfigured")]
            [SendOperation]
            [Message(typeof(ConflictPayload))]
            public void Publish()
            {
            }
        }
    }
}
