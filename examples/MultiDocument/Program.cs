using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MultiDocument.Config;
using MultiDocument.Filters;
using MultiDocument.Fleet;
using Saunter;
using Saunter.Options;

const string baseAddress = "http://localhost:5003";

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(baseAddress);

builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(FleetPublisher) };

    // Honor [JsonPropertyName] when generating payload schema property names.
    options.PropertyNameSelector = property =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        ?? System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(property.Name);

    // Custom inference: prefix inferred operation ids with the action.
    options.Inference.OperationIdGenerator = (member, action) => $"{action}.{member.Name}".ToLowerInvariant();

    // Filters post-process the generated model.
    options.AddDocumentFilter<HeartbeatDocumentFilter>();
    options.AddChannelFilter<EnvironmentTagChannelFilter>();
});

// Both publishers share the attribute document name "v1"; TypeFilter splits
// them into two hosted documents with their own routes and titles.
builder.Services.ConfigureAsyncApiDocument("fleet", document =>
{
    document.AttributeDocumentName = "v1";
    document.TypeFilter = type => type.AsType() == typeof(FleetPublisher);
    document.Middleware.UiTitle = "Fleet API";
    document.Document.Asyncapi = "3.0.0";
    document.Document.Info = new AsyncApiInfoDescriptor { Title = "Fleet API", Version = "1.0.0" };
});

builder.Services.ConfigureAsyncApiDocument("config", document =>
{
    document.AttributeDocumentName = "v1";
    document.TypeFilter = type => type.AsType() == typeof(ConfigPublisher);
    document.Middleware.UiTitle = "Config Messaging API";
    document.Document.Asyncapi = "3.0.0";
    document.Document.Info = new AsyncApiInfoDescriptor { Title = "Config Messaging API", Version = "1.0.0" };
});

var app = builder.Build();

app.MapAsyncApiDocuments();
app.MapAsyncApiUi();

app.Run();
