using System.Linq;
using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class SchemaMapperTests
    {
        [Fact]
        public void Map_ProjectsDescriptorGraphToByteBardSchema()
        {
            var mapper = new AsyncApiSchemaMapper();
            var descriptor = new AsyncApiSchemaDescriptor
            {
                Id = "order",
                Type = AsyncApiSchemaValueType.Object,
            };
            descriptor.Required.Add("id");
            descriptor.Properties["id"] = new AsyncApiSchemaDescriptor
            {
                Id = "guid",
                Type = AsyncApiSchemaValueType.String,
                Format = "guid",
            };
            descriptor.Properties["child"] = new AsyncApiSchemaDescriptor
            {
                Nullable = true,
                AllOf =
                {
                    new AsyncApiSchemaDescriptor
                    {
                        Reference = "#/components/schemas/order"
                    }
                }
            };

            var schema = mapper.Map(descriptor);

            schema.Title.ShouldBe("order");
            schema.Type.ShouldBe(SchemaType.Object);
            schema.Required.ShouldContain("id");
            schema.Properties["id"].Format.ShouldBe("guid");
            schema.Properties["child"].Nullable.ShouldBeTrue();
            schema.Properties["child"].AllOf.Single().ShouldBeOfType<AsyncApiJsonSchemaReference>()
                .Reference.Reference.ShouldBe("#/components/schemas/order");
        }
    }
}
