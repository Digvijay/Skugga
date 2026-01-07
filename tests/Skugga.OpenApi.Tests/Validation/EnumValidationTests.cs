using Skugga.Core;
using Xunit;


namespace Skugga.OpenApi.Tests.Validation
{
    /// <summary>
    /// Tests for enum value validation.
    /// Validates that enum constraints are properly validated at build time.
    /// Note: These tests verify that code generation works with valid enum specs.
    /// Enum validation warnings (SKUGGA_OPENAPI_028, SKUGGA_OPENAPI_029, SKUGGA_OPENAPI_021)
    /// are tested by building specs with intentional issues and checking build output.
    /// </summary>
    public class EnumValidationTests
    {
        [Fact]
        [Trait("Category", "Validation")]
        public void EnumValidation_ValidEnums_GeneratesInterface()
        {
            // With valid enum values, interface should be generated successfully
            var interfaceType = typeof(IEnumValidationApi);
            Assert.NotNull(interfaceType);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void EnumValidation_ValidEnums_HasExpectedMethods()
        {
            // Verify the interface has the expected methods
            var interfaceType = typeof(IEnumValidationApi);

            var getUsersMethod = interfaceType.GetMethod("GetUsers");
            Assert.NotNull(getUsersMethod);

            var updateStatusMethod = interfaceType.GetMethod("UpdateProductStatus");
            Assert.NotNull(updateStatusMethod);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_ValidEnums_MockReturnsValidData()
        {
            // Valid enums should work correctly in mocks
            var mock = new IEnumValidationApiMock();
            var users = await mock.GetUsers(null, null);

            Assert.NotNull(users);
            Assert.NotEmpty(users);

            var user = users.First();
            Assert.NotNull(user);

            // Enum properties should be set to valid values
            Assert.Contains(user.Status, new[] { "active", "inactive", "pending" });
            Assert.Contains(user.Role, new[] { "admin", "user", "guest" });
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_ParameterWithEnumConstraint_AcceptsValidValue()
        {
            // Parameters with enum constraints should accept valid values
            var mock = new IEnumValidationApiMock();
            var users = await mock.GetUsers("active", "admin");

            Assert.NotNull(users);
            // Mock should handle enum parameter values correctly
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_SchemaEnumProperties_UseValidValues()
        {
            // Schema enum properties should use valid enum values
            var mock = new IEnumValidationApiMock();
            var users = await mock.GetUsers(null, null);

            var user = users.First();

            // Both status and role should be valid enum values
            Assert.True(user.Status == "active" || user.Status == "inactive" || user.Status == "pending");
            Assert.True(user.Role == "admin" || user.Role == "user" || user.Role == "guest");
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void EnumValidation_PropertyLevelEnums_InterfaceGenerated()
        {
            // Interface with property-level enums should be generated successfully
            var interfaceType = typeof(IEnumPropertiesApi);
            Assert.NotNull(interfaceType);

            var createMethod = interfaceType.GetMethod("CreateOrder");
            Assert.NotNull(createMethod);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_RequestBodyWithEnumConstraints_Accepted()
        {
            // Request body with enum-constrained properties should be accepted
            var mock = new IEnumValidationApiMock();

            // Use the generated type directly
            var statusUpdate = new Enum_StatusUpdate { Status = "available" };

            // Call the method - should accept enum-constrained request body
            var product = await mock.UpdateProductStatus(123, statusUpdate);

            Assert.NotNull(product);
            Assert.Equal("available", product.Status);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_ResponseWithEnumProperties_ReturnsValidValues()
        {
            // Response with enum properties should return valid enum values
            var mock = new IEnumValidationApiMock();

            var statusUpdate = new Enum_StatusUpdate { Status = "discontinued" };

            var product = await mock.UpdateProductStatus(123, statusUpdate);

            Assert.NotNull(product);
            // Status should be one of the allowed enum values
            Assert.Contains(product.Status, new[] { "available", "out_of_stock", "discontinued" });
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_PropertyLevelEnums_MockCreatesValidData()
        {
            // Mock with property-level enums should create valid data
            var mock = new IEnumPropertiesApiMock();

            // Use the generated type directly
            var order = new EnumProperties_Order
            {
                Status = "pending",
                Priority = "normal",
                PaymentMethod = "credit_card"
            };

            var result = await mock.CreateOrder(order);

            Assert.NotNull(result);
            // All enum properties should have valid values
            Assert.Contains(result.Status, new[] { "pending", "processing", "completed", "cancelled" });
            Assert.Contains(result.Priority, new[] { "low", "normal", "high", "urgent" });
            Assert.Contains(result.PaymentMethod, new[] { "credit_card", "debit_card", "paypal", "bank_transfer" });
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void EnumValidation_AllGeneratedTypesHaveEnumProperties()
        {
            // Verify that generated types include the enum properties we expect

            // Check User type has Status and Role enum properties
            var userType = typeof(Enum_User);
            var statusProp = userType.GetProperty("Status");
            var roleProp = userType.GetProperty("Role");
            Assert.NotNull(statusProp);
            Assert.NotNull(roleProp);
            Assert.Equal(typeof(string), statusProp.PropertyType);
            Assert.Equal(typeof(string), roleProp.PropertyType);

            // Check Product type has Status enum property
            var productType = typeof(Enum_Product);
            var productStatusProp = productType.GetProperty("Status");
            Assert.NotNull(productStatusProp);
            Assert.Equal(typeof(string), productStatusProp.PropertyType);

            // Check StatusUpdate type has Status enum property
            var statusUpdateType = typeof(Enum_StatusUpdate);
            var updateStatusProp = statusUpdateType.GetProperty("Status");
            Assert.NotNull(updateStatusProp);
            Assert.Equal(typeof(string), updateStatusProp.PropertyType);

            // Check Order type has multiple enum properties
            var orderType = typeof(EnumProperties_Order);
            var orderStatusProp = orderType.GetProperty("Status");
            var orderPriorityProp = orderType.GetProperty("Priority");
            var orderPaymentProp = orderType.GetProperty("PaymentMethod");
            Assert.NotNull(orderStatusProp);
            Assert.NotNull(orderPriorityProp);
            Assert.NotNull(orderPaymentProp);
            Assert.Equal(typeof(string), orderStatusProp.PropertyType);
            Assert.Equal(typeof(string), orderPriorityProp.PropertyType);
            Assert.Equal(typeof(string), orderPaymentProp.PropertyType);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_DifferentEnumValues_AllAccepted()
        {
            // Test that all different enum values are accepted correctly
            var mock = new IEnumValidationApiMock();

            // Test each enum value for StatusUpdate
            foreach (var status in new[] { "available", "out_of_stock", "discontinued" })
            {
                var statusUpdate = new Enum_StatusUpdate { Status = status };
                var product = await mock.UpdateProductStatus(123, statusUpdate);
                Assert.NotNull(product);
                Assert.Contains(product.Status, new[] { "available", "out_of_stock", "discontinued" });
            }
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task EnumValidation_ParametersAndResponseBodies_BothHaveEnums()
        {
            // Verify enum constraints work in both parameters and response bodies
            var mock = new IEnumValidationApiMock();

            // Parameters with enum constraints
            var users = await mock.GetUsers("active", "admin");
            Assert.NotNull(users);

            if (users.Any())
            {
                var user = users.First();
                Assert.NotNull(user.Status);
                Assert.NotNull(user.Role);

                // Both should be valid enum values
                Assert.Contains(user.Status, new[] { "active", "inactive", "pending" });
                Assert.Contains(user.Role, new[] { "admin", "user", "guest" });
            }

            // Request body and response with enum constraints
            var statusUpdate = new Enum_StatusUpdate { Status = "available" };
            var product = await mock.UpdateProductStatus(123, statusUpdate);
            Assert.NotNull(product);
            Assert.Contains(product.Status, new[] { "available", "out_of_stock", "discontinued" });
        }
    }

    #region Test Interfaces

    // Valid enum spec - should generate without warnings
    [SkuggaFromOpenApi("specs/enum-validation.json", SchemaPrefix = "Enum")]
    public partial interface IEnumValidationApi { }

    // Property-level enum spec - validates nested enum properties
    [SkuggaFromOpenApi("specs/enum-properties.json", SchemaPrefix = "EnumProperties")]
    public partial interface IEnumPropertiesApi { }

    #endregion
}
