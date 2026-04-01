using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saunter;
using StreetlightsAPI;

const string baseAddress = "http://localhost:5000";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(console => console.SingleLine = true);
builder.WebHost.UseUrls(baseAddress);

// Add Saunter to the application services.
builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(StreetlightMessageBus) };

    options.Middleware.UiTitle = "Streetlights API";

    options.AsyncApi = new AsyncApiDocumentDescriptor
    {
        Asyncapi = "3.0.0",
        Info = new AsyncApiInfoDescriptor
        {
            Title = "Streetlights API",
            Version = "1.0.0",
            Description = "The Smartylighting Streetlights API allows you to remotely manage the city lights.",
            License = new AsyncApiLicenseDescriptor
            {
                Name = "Apache 2.0",
                Url = new("https://www.apache.org/licenses/LICENSE-2.0"),
            }
        },
        Servers =
        {
            ["mosquitto"] = new AsyncApiServerDescriptor { Host = "test.mosquitto.org", Protocol = "mqtt" },
            ["webapi"] = new AsyncApiServerDescriptor { Host = "localhost:5000", Protocol = "http" },
        },
        Components = new AsyncApiComponentsDescriptor
        {
        }
    };
});

builder.Services.AddScoped<IStreetlightMessageBus, StreetlightMessageBus>();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseRouting();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod());

app.MapAsyncApiDocuments();
app.MapAsyncApiUi();
app.MapControllers();

// Print the AsyncAPI doc location
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var address in app.Urls)
    {
        logger.LogInformation("AsyncAPI doc available at: {URL}", $"{address}/asyncapi/asyncapi.json");
        logger.LogInformation("AsyncAPI UI available at: {URL}", $"{address}/asyncapi/ui/");
    }
});

app.Run();
