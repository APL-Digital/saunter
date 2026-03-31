using MassTransitStreetlights.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saunter;

namespace MassTransitStreetlights;

public static class Program
{
    public static void Main(string[] args)
    {
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
        app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        app.MapAsyncApiDocuments();
        app.MapAsyncApiUi();
        app.MapControllers();

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));

        logger.LogInformation("AsyncAPI doc available at: {URL}", $"{baseAddress}/asyncapi/asyncapi.json");
        logger.LogInformation("AsyncAPI UI available at: {URL}", $"{baseAddress}/asyncapi/ui/");

        app.Run();
    }
}
