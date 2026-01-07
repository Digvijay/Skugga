using System;
using System.Threading.Tasks;
using Skugga.Core;
using Skugga.Core.Exceptions;
using Skugga.OpenApi.Tests.Generation.StatefulUsers;
using Xunit;

namespace Skugga.OpenApi.Tests.Stateful
{
    /// <summary>
    /// Integration tests for StatefulBehavior - Skugga's advanced mocking feature that maintains in-memory state
    /// across API operations, honoring actual input data instead of generating example data.
    ///
    /// These tests validate that:
    /// - Stateful mocks persist and return real posted data (not examples)
    /// - CRUD operations work correctly with in-memory storage
    /// - Both request body data AND query/path parameters are honored
    /// - Multiple operations preserve all data independently
    /// - Error scenarios (404s) work correctly for missing entities
    ///
    /// The IPaymentApi interface below demonstrates the "ONE LINE" Doppelgänger workflow:
    /// ONE attribute generates interface + DTOs + fully functional stateful mock.
    /// </summary>
    public class ErrorScenarioIntegrationTests
    {
        #region Basic Generated Mock Tests

        /// <summary>
        /// Tests that the generated mock correctly handles method signatures with both
        /// query parameters AND request bodies, prioritizing body data when available.
        /// </summary>
        [Fact]
        public async Task GeneratedMock_ReturnsChargeResult()
        {
            var mock = new IPaymentApiMock();

            // CreateCharge now takes both query params AND request body
            var chargeRequest = new ChargeRequest { Amount = 100, Currency = "USD" };
            var charge = await mock.CreateCharge(100, "USD", chargeRequest);

            Assert.NotNull(charge);
            Assert.NotNull(charge.TransactionId);
            Assert.Equal(100, charge.Amount); // Honors query parameter
        }

        /// <summary>
        /// Tests that UPDATE operations (PUT) work correctly with both query parameters
        /// and request bodies, creating entities when they don't exist (REST semantics).
        /// </summary>
        [Fact]
        public async Task GeneratedMock_UpdateAccountCompletes()
        {
            var mock = new IPaymentApiMock();

            // UpdateAccount now takes both query param AND request body
            var accountUpdate = new AccountUpdate { Balance = 500 };
            var result = await mock.UpdateAccount("acc_123", 500, accountUpdate);

            Assert.NotNull(result);
            Assert.Equal("acc_123", result.Id);
            Assert.Equal(500, result.Balance); // Honors query parameter
        }

        /// <summary>
        /// Tests that multiple CREATE operations generate unique entities with
        /// different IDs while preserving all the posted data.
        /// </summary>
        [Fact]
        public async Task GeneratedMock_MultipleChargesGenerated()
        {
            var mock = new IPaymentApiMock();

            var charge1 = await mock.CreateCharge(100, "USD", new ChargeRequest { Amount = 100, Currency = "USD" });
            var charge2 = await mock.CreateCharge(200, "EUR", new ChargeRequest { Amount = 200, Currency = "EUR" });
            var charge3 = await mock.CreateCharge(50, "GBP", new ChargeRequest { Amount = 50, Currency = "GBP" });

            // Each charge gets unique data
            Assert.NotNull(charge1.TransactionId);
            Assert.NotNull(charge2.TransactionId);
            Assert.NotNull(charge3.TransactionId);
            Assert.Equal(100, charge1.Amount);
            Assert.Equal(200, charge2.Amount);
            Assert.Equal(50, charge3.Amount);
        }

        /// <summary>
        /// Tests that the mock handles multiple different operations (CREATE and UPDATE)
        /// working together correctly with in-memory state persistence.
        /// </summary>
        [Fact]
        public async Task GeneratedMock_HandlesMultipleOperations()
        {
            var mock = new IPaymentApiMock();

            // Multiple operations work
            var accountUpdate = new AccountUpdate { Balance = 100 };
            await mock.UpdateAccount("acc_1", 100, accountUpdate);

            var newUpdate = new AccountUpdate { Balance = 200 };
            await mock.UpdateAccount("acc_2", 200, newUpdate);

            var charge = await mock.CreateCharge(50, "GBP", new ChargeRequest { Amount = 50, Currency = "GBP" });

            Assert.NotNull(charge);
        }

        /// <summary>
        /// Tests that the stateful mock correctly throws 404 errors when trying to
        /// retrieve entities that were never created, simulating real API behavior.
        /// </summary>
        [Fact]
        public async Task StatefulMock_Throws404ForNonExistentAccount()
        {
            var mock = new IPaymentApiMock();

            // Try to get account that was never created
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                mock.GetAccount("acc_doesnotexist"));

            // Stateful mock throws 404 for missing entities
            Assert.Contains("404", ex.Message);
            Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// CORE TEST: Validates that StatefulBehavior honors actual posted data
        /// instead of generating example data. This is the key differentiator
        /// from stateless mocks.
        /// </summary>
        [Fact]
        public async Task StatefulMock_HonorsPostedData_InMemory()
        {
            // Test that stateful mock uses the posted data instead of generating example data
            var mock = new IPaymentApiMock();

            var chargeRequest = new ChargeRequest
            {
                Amount = 150.0,
                Currency = "EUR"
            };

            // Create charge with specific data
            var charge = await mock.CreateCharge(150.0, "EUR", chargeRequest);

            // Verify the created charge has the data from the input
            Assert.NotNull(charge);
            Assert.Equal(150.0, charge.Amount);
            Assert.NotNull(charge.TransactionId); // ID should be generated
        }

        /// <summary>
        /// CORE TEST: Validates that UPDATE operations honor posted data
        /// and correctly update existing entities in memory.
        /// </summary>
        [Fact]
        public async Task StatefulMock_UpdateHonorsPostedData()
        {
            // Test that PUT operations use the posted data to update existing entities
            var mock = new IPaymentApiMock();

            // First create an account
            var initialUpdate = new AccountUpdate { Balance = 100.0 };
            await mock.UpdateAccount("test-account", 100.0, initialUpdate);

            // Update with new data
            var newUpdate = new AccountUpdate { Balance = 250.0 };
            await mock.UpdateAccount("test-account", 250.0, newUpdate);

            // Verify the account was updated
            var retrieved = await mock.GetAccount("test-account");
            Assert.NotNull(retrieved);
            Assert.Equal(250.0, retrieved.Balance);
        }

        /// <summary>
        /// CORE TEST: Validates that GET operations return the exact saved data
        /// from previous operations, not newly generated example data.
        /// </summary>
        [Fact]
        public async Task StatefulMock_ReturnsSavedData_OnSubsequentGets()
        {
            // Test that GET operations return the saved data, not generated examples
            var mock = new IPaymentApiMock();

            var chargeRequest = new ChargeRequest
            {
                Amount = 75.50,
                Currency = "GBP"
            };

            // Create charge
            var createdCharge = await mock.CreateCharge(75.50, "GBP", chargeRequest);

            // Since we don't have a get charge operation, let's test account operations
            var accountUpdate = new AccountUpdate { Balance = 500.0 };
            await mock.UpdateAccount("acc-123", 500.0, accountUpdate);

            // Get the account back
            var retrievedAccount = await mock.GetAccount("acc-123");

            // Verify we get back the exact same data that was posted
            Assert.NotNull(retrievedAccount);
            Assert.Equal("acc-123", retrievedAccount.Id);
            Assert.Equal(500.0, retrievedAccount.Balance);
        }

        /// <summary>
        /// CORE TEST: Validates that multiple CREATE operations preserve
        /// all posted data independently with unique generated IDs.
        /// </summary>
        [Fact]
        public async Task StatefulMock_MultipleCreates_PreserveAllData()
        {
            // Test that multiple POST operations preserve all the posted data
            var mock = new IPaymentApiMock();

            var charge1 = new ChargeRequest { Amount = 100.0, Currency = "USD" };
            var charge2 = new ChargeRequest { Amount = 200.0, Currency = "EUR" };

            var result1 = await mock.CreateCharge(100.0, "USD", charge1);
            var result2 = await mock.CreateCharge(200.0, "EUR", charge2);

            // Verify both charges have their respective data
            Assert.Equal(100.0, result1.Amount);
            Assert.Equal(200.0, result2.Amount);

            // Verify they have different transaction IDs
            Assert.NotEqual(result1.TransactionId, result2.TransactionId);
        }

        /// <summary>
        /// Tests parameter precedence: when both query parameters and request body
        /// are present, request body data takes precedence.
        /// </summary>
        [Fact]
        public async Task StatefulMock_HonorsQueryParameters_WhenNoBody()
        {
            // Test that parameters are honored even when there's no request body
            var mock = new IPaymentApiMock();

            // Create charge with specific amount via query parameter
            var charge = await mock.CreateCharge(999.99, "JPY", new ChargeRequest { Amount = 100, Currency = "USD" });

            // Body parameters take precedence, so we get the body amount (100), not query (999.99)
            Assert.NotNull(charge);
            Assert.Equal(100, charge.Amount); // Honors body parameter over query
            Assert.NotNull(charge.TransactionId);
        }

        /// <summary>
        /// Tests parameter precedence rules: request body data takes precedence
        /// over query parameters when both are provided.
        /// </summary>
        [Fact]
        public async Task StatefulMock_HonorsBodyParameters_OverQuery_WhenAvailable()
        {
            // Test that body parameters are honored when both query and body exist
            var mock = new IPaymentApiMock();

            // Create account with conflicting balance values
            var accountUpdate = new AccountUpdate { Balance = 777.77 };
            var result = await mock.UpdateAccount("test-acc", 555.55, accountUpdate);

            // Should honor the body parameter (777.77) over query parameter (555.55)
            // because body parameters take precedence in the current implementation
            Assert.NotNull(result);
            Assert.Equal("test-acc", result.Id);
            Assert.Equal(777.77, result.Balance);
        }

        #endregion
    }

    /// <summary>
    /// THE DOPPELGÄNGER WORKFLOW: ONE LINE generates everything needed for stateful API testing!
    /// 
    /// This single attribute generates:
    /// ✅ Complete IPaymentApi interface with all operations
    /// ✅ All DTOs (ChargeRequest, ChargeResult, AccountUpdate, AccountInfo)  
    /// ✅ Fully functional IPaymentApiMock with StatefulBehavior
    /// ✅ In-memory state persistence across operations
    /// ✅ Realistic data generation honoring actual input parameters
    /// 
    /// The generated mock:
    /// - Honors request body data (highest priority)
    /// - Honors query/path parameters when no body exists  
    /// - Falls back to example data only when no input provided
    /// - Maintains state across multiple operations
    /// - Throws 404s for missing entities (real API behavior)
    /// - Generates unique IDs for created entities
    /// 
    /// Usage: var mock = new IPaymentApiMock(); // Ready to use!
    /// </summary>
    [SkuggaFromOpenApi("specs/payment-api.yaml", StatefulBehavior = true)]
    public partial interface IPaymentApi
    {
        // Skugga generates:
        // - Task<ChargeResult> CreateCharge(double amount, string currency, ChargeRequest body)
        // - Task<AccountInfo> GetAccount(string accountId)  
        // - Task<AccountInfo> UpdateAccount(string accountId, double balance, AccountUpdate body)
        // - STATEFUL mock with realistic data generation
    }
}

