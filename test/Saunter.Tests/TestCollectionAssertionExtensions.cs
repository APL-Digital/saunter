using System.Collections.Generic;
using System.Linq;
using Saunter.AttributeProvider.Descriptors;
using Shouldly;

namespace Saunter.Tests
{
    internal static class TestCollectionAssertionExtensions
    {
        public static void ShouldContainKey(this IReadOnlyList<AsyncApiParameterDescriptor> parameters, string key)
        {
            parameters.Select(parameter => parameter.Name).ShouldContain(key);
        }
    }
}
