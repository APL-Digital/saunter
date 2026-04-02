#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Shouldly;
using Xunit;

namespace Saunter.Tests
{
    public class PackagingTests
    {
        [Fact]
        public void SaunterPackage_IncludesAnalyzerProjectReferenceForPacking()
        {
            var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Saunter/Saunter.csproj"));
            var document = XDocument.Load(projectPath);

            var analyzerReference = document
                .Descendants("ProjectReference")
                .FirstOrDefault(element => (string?)element.Attribute("Include") == @"..\Saunter.Analyzers\Saunter.Analyzers.csproj");

            analyzerReference.ShouldNotBeNull();
            ((string?)analyzerReference!.Attribute("OutputItemType")).ShouldBe("Analyzer");
            ((string?)analyzerReference.Attribute("ReferenceOutputAssembly")).ShouldBe("false");
            ((string?)analyzerReference.Attribute("PrivateAssets")).ShouldBe("all");

            var analyzerAsset = document
                .Descendants("None")
                .FirstOrDefault(element =>
                    ((string?)element.Attribute("Include"))?.Contains("$(SaunterAnalyzersAssemblyName).dll", StringComparison.Ordinal) == true
                    && ((string?)element.Attribute("PackagePath")) == "analyzers/dotnet/cs");

            analyzerAsset.ShouldNotBeNull();
            ((string?)analyzerAsset!.Attribute("Pack")).ShouldBe("true");
        }
    }
}
