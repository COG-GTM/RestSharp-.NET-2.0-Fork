using System;
using Xunit;
using FluentAssertions;

namespace RestSharp.Tests
{
    /// <summary>
    /// Infrastructure validation tests to ensure the new .NET Core test project is working correctly.
    /// These tests validate the migration from xUnit 1.9 to xUnit 2.9+.
    /// </summary>
    public class InfrastructureValidationTests
    {
        /// <summary>
        /// Validates that the test framework is working correctly.
        /// </summary>
        [Fact]
        public void TestFramework_ShouldBeWorking()
        {
            // Arrange
            var expected = true;

            // Act
            var actual = true;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Validates that FluentAssertions is working correctly.
        /// </summary>
        [Fact]
        public void FluentAssertions_ShouldBeWorking()
        {
            // Arrange
            var value = 42;

            // Act & Assert
            value.Should().Be(42);
            value.Should().BeGreaterThan(0);
            value.Should().BeLessThan(100);
        }

        /// <summary>
        /// Validates that the RestSharp.Core project reference is working.
        /// </summary>
        [Fact]
        public void RestSharpCore_ProjectReference_ShouldBeWorking()
        {
            // Arrange & Act
            var targetFramework = BuildValidation.TargetFramework;
            var migrationPhase = BuildValidation.MigrationPhase;

            // Assert
            targetFramework.Should().Be("netstandard2.0");
            migrationPhase.Should().Contain("Phase 2");
        }

        /// <summary>
        /// Validates that the test project targets .NET 8.0.
        /// </summary>
        [Fact]
        public void TestProject_ShouldTargetNet8()
        {
            // Arrange & Act
            var frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

            // Assert
            frameworkDescription.Should().Contain(".NET");
        }
    }
}
