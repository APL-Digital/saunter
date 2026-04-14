using System;
using ByteBard.AsyncAPI.Bindings.Kafka;
using ByteBard.AsyncAPI.Models;
using MassTransitUseCases.AsyncApi;
using Saunter;

namespace MassTransitUseCases.AsyncApi;

internal static class CommerceAsyncApiDocument
{
    public static AsyncApiDocumentDescriptor Create()
    {
        return new AsyncApiDocumentDescriptor
        {
            Id = "urn:saunter:examples:masstransit-use-cases",
            Asyncapi = "3.0.0",
            DefaultContentType = "application/json",
            Info = new AsyncApiInfoDescriptor
            {
                Title = "MassTransit Use Cases",
                Version = "1.0.0",
                Description = "Saunter + MassTransit example that demonstrates multiple AsyncAPI 3 authoring patterns in one document.",
                TermsOfService = new Uri("https://example.com/terms"),
                Contact = new AsyncApiContactDescriptor
                {
                    Name = "Saunter Example Maintainers",
                    Email = "maintainers@example.com",
                    Url = new Uri("https://github.com/APL-Digital/saunter"),
                },
                License = new AsyncApiLicenseDescriptor
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT"),
                }
            },
            Servers =
            {
                ["inmemory"] = new AsyncApiServerDescriptor
                {
                    Host = "localhost",
                    Protocol = "in-memory",
                    Description = "MassTransit in-memory transport used when you run the example locally.",
                    Tags =
                    {
                        new AsyncApiTag { Name = "runtime", Description = "Local runtime transport for the example project." }
                    }
                },
                ["rabbitmq"] = new AsyncApiServerDescriptor
                {
                    Host = "{region}.broker.example.com:{port}",
                    Protocol = "amqps",
                    ProtocolVersion = "0-9-1",
                    Description = "Modeled broker topology for the documented use cases.",
                    Variables =
                    {
                        ["region"] = new AsyncApiServerVariableDescriptor
                        {
                            Default = "eu1",
                            Description = "Example deployment region.",
                            Enum = { "eu1", "us1" },
                            Examples = { "eu1" }
                        },
                        ["port"] = new AsyncApiServerVariableDescriptor
                        {
                            Default = "5671",
                            Description = "TLS-enabled AMQP port.",
                            Examples = { "5671" }
                        }
                    },
                    Security =
                    {
                        new AsyncApiSecuritySchemeReference("#/components/securitySchemes/rabbitmqUserPassword")
                    },
                    Tags =
                    {
                        new AsyncApiTag { Name = "broker", Description = "Primary broker used for the documented topology." },
                        new AsyncApiTag { Name = "example", Description = "Example-only infrastructure metadata." }
                    }
                }
            },
            Components = new AsyncApiComponentsDescriptor
            {
                ChannelBindings =
                {
                    ["searchIndexKafkaTopic"] = new()
                    {
                        new KafkaChannelBinding
                        {
                            Topic = "search.index.sync",
                            Partitions = 6,
                            Replicas = 3,
                        }
                    }
                },
                OperationBindings =
                {
                    ["searchIndexKafkaProducer"] = new()
                    {
                        new KafkaOperationBinding
                        {
                            ClientId = new AsyncApiJsonSchema { Type = SchemaType.String },
                        }
                    }
                },
                MessageBindings =
                {
                    ["searchIndexKafkaMessage"] = new()
                    {
                        new KafkaMessageBinding
                        {
                            Key = new AsyncApiJsonSchema { Type = SchemaType.String },
                            SchemaLookupStrategy = "TopicNameStrategy",
                        }
                    }
                },
                CorrelationIds =
                {
                    ["workflowCorrelation"] = new AsyncApiCorrelationId
                    {
                        Description = "Application-level correlation id carried in message headers.",
                        Location = "$message.header#/correlationId",
                    }
                },
                SecuritySchemes =
                {
                    ["rabbitmqUserPassword"] = new AsyncApiSecurityScheme
                    {
                        Type = SecuritySchemeType.UserPassword,
                        Description = "Username and password used to authenticate with RabbitMQ.",
                    }
                }
            }
        };
    }
}
