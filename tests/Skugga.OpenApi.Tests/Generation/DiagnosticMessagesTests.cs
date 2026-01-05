using System.Linq;
using Xunit;

namespace Skugga.OpenApi.Tests.Generation
{
    /// <summary>
    /// Tests for improved diagnostic error messages with guidance.
    /// Verifies that helpful, actionable error messages with fix suggestions are generated.
    /// </summary>
    public class DiagnosticMessagesTests
    {
        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_EmptySource_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.EmptySource;

            Assert.Equal("SKUGGA_OPENAPI_002", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
            Assert.Contains("DOPPELGANGER", descriptor.HelpLinkUri.ToUpper());
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_SpecNotFound_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.SpecNotFound;

            Assert.Equal("SKUGGA_OPENAPI_003", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_ParseError_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.ParseError;

            Assert.Equal("SKUGGA_OPENAPI_004", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_MockGenerationError_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.MockGenerationError;

            Assert.Equal("SKUGGA_OPENAPI_005", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_NoPathsDefined_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.NoPathsDefined;

            Assert.Equal("SKUGGA_OPENAPI_008", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_MissingOperationId_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.MissingOperationId;

            Assert.Equal("SKUGGA_OPENAPI_009", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_NoSuccessResponse_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.NoSuccessResponse;

            Assert.Equal("SKUGGA_OPENAPI_007", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_UnsupportedSchemaType_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.UnsupportedSchemaType;

            Assert.Equal("SKUGGA_OPENAPI_010", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_InvalidExampleValue_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.InvalidExampleValue;

            Assert.Equal("SKUGGA_OPENAPI_011", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void DiagnosticHelper_UnexpectedError_HasExpectedProperties()
        {
            var descriptor = Skugga.OpenApi.Generator.DiagnosticHelper.UnexpectedError;

            Assert.Equal("SKUGGA_OPENAPI_001", descriptor.Id);
            Assert.NotNull(descriptor.HelpLinkUri);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void AllDiagnostics_HaveHelpLinks()
        {
            var diagnostics = new[]
            {
                Skugga.OpenApi.Generator.DiagnosticHelper.EmptySource,
                Skugga.OpenApi.Generator.DiagnosticHelper.SpecNotFound,
                Skugga.OpenApi.Generator.DiagnosticHelper.ParseError,
                Skugga.OpenApi.Generator.DiagnosticHelper.MockGenerationError,
                Skugga.OpenApi.Generator.DiagnosticHelper.NoPathsDefined,
                Skugga.OpenApi.Generator.DiagnosticHelper.MissingOperationId,
                Skugga.OpenApi.Generator.DiagnosticHelper.NoSuccessResponse,
                Skugga.OpenApi.Generator.DiagnosticHelper.UnsupportedSchemaType,
                Skugga.OpenApi.Generator.DiagnosticHelper.InvalidExampleValue,
                Skugga.OpenApi.Generator.DiagnosticHelper.UnexpectedError
            };

            foreach (var diagnostic in diagnostics)
            {
                Assert.NotNull(diagnostic.HelpLinkUri);
                Assert.Contains("github.com", diagnostic.HelpLinkUri);
                Assert.Contains("DOPPELGANGER", diagnostic.HelpLinkUri.ToUpper());
            }
        }
    }
}
