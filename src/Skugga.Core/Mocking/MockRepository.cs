#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// Manages multiple mocks together, enabling shared configuration and collective verification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MockRepository is used to create and track multiple mocks in a single test.
    /// It provides a central point for verifying that all setups across all mocks
    /// were executed as expected.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var repository = new MockRepository(MockBehavior.Strict);
    /// var mock1 = repository.Create&lt;IService1&gt;();
    /// var mock2 = repository.Create&lt;IService2&gt;();
    ///
    /// // ... test execution ...
    ///
    /// // Verifies all mocks according to their individual .Verifiable() setups
    /// repository.Verify();
    ///
    /// // Verifies that every single setup on every mock was called
    /// repository.VerifyAll();
    /// </code>
    /// </example>
    public class MockRepository
    {
        private readonly List<IMockSetup> _mocks = new List<IMockSetup>();

        /// <summary>
        /// Gets the default behavior for mocks created by this repository.
        /// </summary>
        public MockBehavior Behavior { get; }

        /// <summary>
        /// Gets the default value strategy for mocks created by this repository.
        /// </summary>
        public DefaultValue DefaultValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockRepository"/> class.
        /// </summary>
        /// <param name="behavior">The default behavior for mocks created by this repository.</param>
        /// <param name="defaultValue">The default value strategy for mocks created by this repository.</param>
        public MockRepository(MockBehavior behavior = MockBehavior.Loose, DefaultValue defaultValue = DefaultValue.Empty)
        {
            Behavior = behavior;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Creates a mock and registers it with the repository.
        /// </summary>
        /// <typeparam name="T">The type to mock.</typeparam>
        /// <param name="behavior">The mock behavior (overrides repository default if specified).</param>
        /// <returns>A mocked instance of T.</returns>
        /// <remarks>
        /// This method is intercepted by the source generator at compile time.
        /// </remarks>
        public T Create<T>(MockBehavior? behavior = null) where T : class
        {
            // The source generator MUST intercept this call.
            // When intercepted, the generator will produce code that calls Mock.Create<T>()
            // and then registers the result with this repository.
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept MockRepository.Create<{typeof(T).Name}>().\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.");
        }

        /// <summary>
        /// Creates a mock and registers it with the repository.
        /// </summary>
        /// <typeparam name="T">The type to mock.</typeparam>
        /// <param name="behavior">The mock behavior.</param>
        /// <param name="defaultValue">The default value strategy.</param>
        /// <returns>A mocked instance of T.</returns>
        public T Create<T>(MockBehavior behavior, DefaultValue defaultValue) where T : class
        {
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept MockRepository.Create<{typeof(T).Name}>().\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.");

        }

        /// <summary>
        /// Creates a mock and registers it with the repository.
        /// </summary>
        /// <typeparam name="T">The type to mock.</typeparam>
        /// <param name="defaultValue">The default value strategy to use.</param>
        /// <returns>A mocked instance of T.</returns>
        public T Create<T>(DefaultValue defaultValue) where T : class
        {
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept MockRepository.Create<{typeof(T).Name}>().\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.");
        }

        /// <summary>
        /// Registers an existing mock with the repository.
        /// </summary>
        /// <param name="mock">The mock instance to register.</param>
        public void Register(object mock)
        {
            if (mock is IMockSetup setup)
            {
                if (!_mocks.Contains(setup))
                {
                    _mocks.Add(setup);
                }
            }
            else
            {
                throw new ArgumentException("Object is not a Skugga mock.", nameof(mock));
            }
        }

        /// <summary>
        /// Verifies that all layouts marked as Verifiable() on all registered mocks were actually called.
        /// </summary>
        public void Verify()
        {
            foreach (var mock in _mocks)
            {
                mock.Handler.Verify();
            }
        }

        /// <summary>
        /// Verifies that all configured setups on all registered mocks were called at least once.
        /// </summary>
        public void VerifyAll()
        {
            foreach (var mock in _mocks)
            {
                mock.Handler.VerifyAll();
            }
        }

        /// <summary>
        /// Clears all setups and invocation history for all registered mocks.
        /// </summary>
        public void Reset()
        {
            foreach (var mock in _mocks)
            {
                mock.Handler.Reset();
            }
        }

        /// <summary>
        /// Clears only the invocation history for all registered mocks, keeping all setups.
        /// </summary>
        public void ResetCalls()
        {
            foreach (var mock in _mocks)
            {
                mock.Handler.ResetCalls();
            }
        }


        /// <summary>
        /// Verifies that no other calls were made to any registered mock that haven't been already verified.
        /// </summary>
        public void VerifyNoOtherCalls()
        {
            foreach (var mock in _mocks)
            {
                mock.Handler.VerifyNoOtherCalls();
            }
        }
    }
}
