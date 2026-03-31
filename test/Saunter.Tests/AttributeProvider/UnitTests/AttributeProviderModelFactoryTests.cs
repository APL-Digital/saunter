using System;
using Saunter.AttributeProvider;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.UnitTests
{
    public class AttributeProviderModelFactoryTests
    {
        [Fact]
        public void GetReferenceKey_ThrowsArgumentNullExceptionForNullReference()
        {
            var actual = () => AttributeProviderModelFactory.GetReferenceKey(null!);

            Should.Throw<ArgumentNullException>(actual)
                .ParamName.ShouldBe("reference");
        }
    }
}
