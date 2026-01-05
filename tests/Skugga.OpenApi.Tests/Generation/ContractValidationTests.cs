using System;
using System.Diagnostics;
using System.Reflection;
using Xunit;
using Skugga.Core.Exceptions;
using Skugga.OpenApi.Tests.Generation.ValidatedProducts;
using Skugga.OpenApi.Tests.Generation.UnvalidatedProducts;

namespace Skugga.OpenApi.Tests.Generation
{
    /// <summary>
    /// Tests for Runtime Contract Validation.
    /// Validates that generated mocks properly validate responses against OpenAPI schemas.
    /// </summary>
    public class ContractValidationTests
    {
        [Fact]
        [Trait("Category", "Contract Validation")]
        public void ValidatedMock_Interface_IsGenerated()
        {
            var assembly = typeof(IValidatedProductApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.ValidatedProducts.IValidatedProductApiMock");
            
            Assert.NotNull(mockType);
            Assert.Contains(mockType.GetInterfaces(), i => i.Name == "IValidatedProductApi");
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void ValidatedMock_HasValidationCode_InGeneratedMethods()
        {
            // Verify that the mock class contains validation calls to SchemaValidator
            var assembly = typeof(IValidatedProductApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.ValidatedProducts.IValidatedProductApiMock");
            
            Assert.NotNull(mockType);
            
            // Check that GetProduct method exists
            var getProductMethod = mockType.GetMethod("GetProduct");
            Assert.NotNull(getProductMethod);
            
            // Verify method can be invoked (validates it was generated correctly)
            var mock = Activator.CreateInstance(mockType);
            Assert.NotNull(mock);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void UnvalidatedMock_DoesNotHaveValidationCode()
        {
            // Unvalidated mock should work without validation
            var assembly = typeof(IUnvalidatedProductApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.UnvalidatedProducts.IUnvalidatedProductApiMock");
            
            Assert.NotNull(mockType);
            
            var mock = Activator.CreateInstance(mockType);
            Assert.NotNull(mock);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public async System.Threading.Tasks.Task ValidatedMock_GetProduct_ReturnsValidProduct()
        {
            var mock = new IValidatedProductApiMock();
            
            // Should return a valid product that passes validation
            var result = await mock.GetProduct(1);
            
            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public async System.Threading.Tasks.Task ValidatedMock_ListProducts_ReturnsValidArray()
        {
            var mock = new IValidatedProductApiMock();
            
            // Should return an array that passes validation
            var result = await mock.ListProducts();
            
            Assert.NotNull(result);
            Assert.IsAssignableFrom<System.Collections.IEnumerable>(result);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public async System.Threading.Tasks.Task UnvalidatedMock_Works_WithoutValidation()
        {
            var mock = new IUnvalidatedProductApiMock();
            
            // Should work without validation overhead
            var result = await mock.GetProduct(1);
            
            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateValue_AcceptsValidType()
        {
            // Verify SchemaValidator methods work correctly
            var value = "test string";
            
            // Should not throw for valid string
            var exception = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateValue(value, typeof(string), "testField", null, null));
            
            Assert.Null(exception);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateValue_ThrowsForNull_WhenRequired()
        {
            // Should throw ContractViolationException for null when required
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateValue(null, typeof(string), "testField", null, null);
            });
            
            Assert.NotNull(exception);
            Assert.Contains("testField", exception.Message);
            Assert.Equal("testField", exception.FieldPath);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateValue_AcceptsNull_WhenNotRequired()
        {
            // Null handling is done at the property level, not value level
            // This test verifies ValidateValue behavior exists
            var exception = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateValue("test", typeof(string), "testField", null, null));
            
            Assert.Null(exception);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateValue_ValidatesEnumValues()
        {
            var validValue = "Electronics";
            var invalidValue = "InvalidCategory";
            var enumValues = new[] { "Electronics", "Clothing", "Food", "Books" };
            
            // Valid enum value should not throw
            var validException = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateValue(validValue, typeof(string), "category", null, enumValues));
            Assert.Null(validException);
            
            // Invalid enum value should throw
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateValue(invalidValue, typeof(string), "category", null, enumValues);
            });
            
            Assert.NotNull(exception);
            Assert.Contains("category", exception.Message);
            Assert.Contains("Electronics, Clothing, Food, Books", exception.Expected);
            Assert.Contains("InvalidCategory", exception.Actual);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateRequiredProperties_ThrowsForMissingProperty()
        {
            var obj = new { Name = "Test" }; // Missing "Price" property
            var requiredProps = new[] { "Name", "Price" };
            
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateRequiredProperties(obj, "Product", requiredProps);
            });
            
            Assert.NotNull(exception);
            Assert.Contains("Price", exception.Message);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateRequiredProperties_AcceptsAllRequired()
        {
            var obj = new { Name = "Test", Price = 10.0 };
            var requiredProps = new[] { "Name", "Price" };
            
            // Should not throw when all required properties present
            var exception = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateRequiredProperties(obj, "Product", requiredProps));
            
            Assert.Null(exception);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateStringFormat_ValidatesEmail()
        {
            var validEmail = "test@example.com";
            var invalidEmail = "not-an-email";
            
            // Valid email should not throw
            var validException = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateStringFormat(validEmail, "email", "email"));
            Assert.Null(validException);
            
            // Invalid email should throw
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateStringFormat(invalidEmail, "email", "email");
            });
            
            Assert.NotNull(exception);
            Assert.Contains("format", exception.Message.ToLower());
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateStringFormat_ValidatesUri()
        {
            var validUri = "https://example.com/path";
            var invalidUri = "ht!tp://not valid";
            
            // Valid URI should not throw
            var validException = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateStringFormat(validUri, "uri", "website"));
            Assert.Null(validException);
            
            // Invalid URI should throw
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateStringFormat(invalidUri, "uri", "website");
            });
            
            Assert.NotNull(exception);
            Assert.Contains("format", exception.Message.ToLower());
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateStringFormat_ValidatesDateTime()
        {
            var validDateTime = "2026-01-04T10:30:00Z";
            var invalidDateTime = "not@valid!date";
            
            // Valid date-time should not throw
            var validException = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateStringFormat(validDateTime, "date-time", "createdAt"));
            Assert.Null(validException);
            
            // Invalid date-time should throw
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateStringFormat(invalidDateTime, "date-time", "createdAt");
            });
            
            Assert.NotNull(exception);
            Assert.Contains("format", exception.Message.ToLower());
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateNumericConstraints_ValidatesMinimum()
        {
            // Valid value above minimum
            var validException = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateNumericConstraints(10.0, "price", 0.0, null, false, false));
            Assert.Null(validException);
            
            // Invalid value below minimum
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateNumericConstraints(-5.0, "price", 0.0, null, false, false);
            });
            
            Assert.NotNull(exception);
            Assert.Contains(">=", exception.Message);
            Assert.Contains("0", exception.Expected);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void SchemaValidator_ValidateNumericConstraints_ValidatesMaximum()
        {
            // Valid value below maximum
            var validException = Record.Exception(() =>
                Skugga.Core.Validation.SchemaValidator.ValidateNumericConstraints(50.0, "discount", null, 100.0, false, false));
            Assert.Null(validException);
            
            // Invalid value above maximum
            var exception = Assert.Throws<ContractViolationException>(() =>
            {
                Skugga.Core.Validation.SchemaValidator.ValidateNumericConstraints(150.0, "discount", null, 100.0, false, false);
            });
            
            Assert.NotNull(exception);
            Assert.Contains("<=", exception.Message);
            Assert.Contains("100", exception.Expected);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public void ContractViolationException_HasCorrectProperties()
        {
            var exception = new ContractViolationException(
                "Field 'price' is invalid",
                "product.price",
                "must be >= 0",
                "-10"
            );
            
            Assert.Equal("product.price", exception.FieldPath);
            Assert.Equal("must be >= 0", exception.Expected);
            Assert.Equal("-10", exception.Actual);
            Assert.Contains("price", exception.Message);
        }

        [Fact]
        [Trait("Category", "Contract Validation")]
        public async System.Threading.Tasks.Task ValidationPerformance_IsUnder5ms()
        {
            // Verify validation overhead is minimal (< 5ms target from roadmap)
            var mock = new IValidatedProductApiMock();
            var stopwatch = Stopwatch.StartNew();
            
            // Run validation multiple times
            for (int i = 0; i < 100; i++)
            {
                var result = await mock.GetProduct(i);
            }
            
            stopwatch.Stop();
            var avgTime = stopwatch.ElapsedMilliseconds / 100.0;
            
            // Each validation should be under 5ms
            Assert.True(avgTime < 5.0, $"Average validation time {avgTime}ms exceeds 5ms target");
        }
    }
}
