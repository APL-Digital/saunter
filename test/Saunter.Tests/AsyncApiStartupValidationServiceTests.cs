using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;
using Shouldly;
using Xunit;

namespace Saunter.Tests
{
    public class AsyncApiStartupValidationServiceTests
    {
        [Fact]
        public async Task StartAsync_WhenValidationEnabled_ThrowsForMisconfiguredDocument()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Production";
            builder.Services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AssemblyMarkerTypes = new[] { typeof(DuplicateOperationIdApi) };
                options.NamedApis["startup-conflict"] = new AsyncApiDocumentDescriptor();
                options.ValidateOnStartup = true;
            });

            await using var app = builder.Build();

            var exception = await Should.ThrowAsync<InvalidOperationException>(app.StartAsync());
            exception.Message.ShouldContain("is produced by multiple operations");
        }

        [Fact]
        public async Task StartAsync_WhenValidationDisabled_StartsDespiteMisconfiguredDocument()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Production";
            builder.WebHost.UseSetting("urls", "http://127.0.0.1:0");
            builder.Services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AssemblyMarkerTypes = new[] { typeof(DuplicateOperationIdApi) };
                options.NamedApis["startup-conflict"] = new AsyncApiDocumentDescriptor();
                options.ValidateOnStartup = false;
            });

            await using var app = builder.Build();

            await app.StartAsync();
            await app.StopAsync();
        }

        [Fact]
        public async Task StartAsync_DefaultsToValidatingInDevelopment()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Development",
            });
            builder.Services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AssemblyMarkerTypes = new[] { typeof(DuplicateOperationIdApi) };
                options.NamedApis["startup-conflict"] = new AsyncApiDocumentDescriptor();
            });

            await using var app = builder.Build();

            await Should.ThrowAsync<InvalidOperationException>(app.StartAsync());
        }

        [AsyncApi("startup-conflict")]
        private class DuplicateOperationIdApi
        {
            [Channel("orders.a")]
            [SendOperation(OperationId = "duplicate")]
            public void PublishA(string message) { _ = message; }

            [Channel("orders.b")]
            [SendOperation(OperationId = "duplicate")]
            public void PublishB(string message) { _ = message; }
        }
    }
}
