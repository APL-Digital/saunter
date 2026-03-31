using ByteBard.AsyncAPI.Bindings.AMQP;
using ByteBard.AsyncAPI.Models;
using Saunter;

namespace MassTransitStreetlights.AsyncApi;

internal static class StreetlightsAsyncApiDocument
{
    public static AsyncApiDocumentDescriptor Create()
    {
        return new AsyncApiDocumentDescriptor
        {
            Asyncapi = "3.0.0",
            Info = new AsyncApiInfoDescriptor
            {
                Title = "Streetlights RabbitMQ API",
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
                ["rabbitmq"] = new AsyncApiServerDescriptor
                {
                    Host = "rabbitmq.example.org:5671",
                    Protocol = "amqps",
                    Description = "RabbitMQ broker for the Streetlights example.",
                    Security =
                    {
                        new AsyncApiSecuritySchemeReference("#/components/securitySchemes/user-password")
                    },
                    Tags =
                    {
                        new AsyncApiTag { Name = "env:example", Description = "This environment is meant for the example Streetlights deployment." },
                        new AsyncApiTag { Name = "visibility:private", Description = "This resource is private and only available to certain users." }
                    }
                }
            },
            Components = new AsyncApiComponentsDescriptor
            {
                SecuritySchemes =
                {
                    ["user-password"] = new AsyncApiSecurityScheme { Type = SecuritySchemeType.UserPassword, Description = "Provide your username and password for RabbitMQ authentication." }
                }
            }
        };
    }
}
