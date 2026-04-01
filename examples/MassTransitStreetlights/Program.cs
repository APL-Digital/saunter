using MassTransitStreetlights.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saunter;

const string baseAddress = "http://localhost:5000";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(console => console.SingleLine = true);
builder.WebHost.UseUrls(baseAddress);

builder.Services.AddStreetlightsAsyncApi();
builder.Services.AddStreetlightsMessaging();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

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
