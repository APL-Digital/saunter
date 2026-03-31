#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using MassTransitMinimal.Consumers;
using MassTransitMinimal.Producers;
using Microsoft.Extensions.DependencyInjection;
using Saunter.Options;
using Saunter.SharedKernel.Interfaces;
using Shouldly;
using Xunit;

namespace Saunter.Tests.Examples.MassTransitMinimal
{
    public class MassTransitMinimalSnapshotTests
    {
        [Fact]
        public void GeneratedDocument_MatchesHappyPathSnapshot()
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AssemblyMarkerTypes = new[] { typeof(OrderSubmittedPublisher), typeof(OrderSubmittedConsumer) };
                options.Middleware.UiTitle = "MassTransit Minimal";
                options.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = "MassTransit Minimal",
                        Version = "1.0.0",
                        Description = "Minimal MassTransit + Saunter example that relies on annotation defaults and inference.",
                    },
                    Servers =
                    {
                        ["inmemory"] = new AsyncApiServerDescriptor
                        {
                            Host = "localhost",
                            Protocol = "in-memory",
                            Description = "MassTransit in-memory transport used by this sample.",
                        }
                    }
                };
            });

            using var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AsyncApiOptions>>().Value;
            var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
            var writer = serviceProvider.GetRequiredService<IAsyncApiDocumentWriter>();

            var json = writer.WriteJson(provider.GetDocument(null, options));
            var normalizedActual = NormalizeJson(json);
            var snapshotPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../test/Saunter.Tests/Snapshots/MassTransitMinimal.asyncapi.json"));
            var normalizedExpected = NormalizeJson(File.ReadAllText(snapshotPath));

            normalizedActual.ShouldBe(normalizedExpected);
        }

        private static string NormalizeJson(string json)
        {
            var node = JsonNode.Parse(json);
            node.ShouldNotBeNull();

            var normalized = NormalizeNode(node!);
            return normalized.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private static JsonNode NormalizeNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var normalized = new JsonObject();
                foreach (var property in obj.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    normalized[property.Key] = property.Value is null ? null : NormalizeNode(property.Value);
                }

                return normalized;
            }

            if (node is JsonArray array)
            {
                var normalized = new JsonArray();
                foreach (var item in array)
                {
                    normalized.Add(item is null ? null : NormalizeNode(item));
                }

                return normalized;
            }

            return node.DeepClone();
        }
    }
}
