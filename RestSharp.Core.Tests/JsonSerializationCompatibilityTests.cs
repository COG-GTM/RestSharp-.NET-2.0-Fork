using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Newtonsoft.Json;
using SystemTextJson = System.Text.Json;

namespace RestSharp.Tests
{
    /// <summary>
    /// Tests to validate JSON serialization compatibility between Newtonsoft.Json 13.0+ and System.Text.Json.
    /// These tests document breaking changes and migration considerations from Newtonsoft.Json 4.5.1/4.0.8.
    /// </summary>
    public class JsonSerializationCompatibilityTests
    {
        #region Test Models

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DateTime BirthDate { get; set; }
            public List<string> Tags { get; set; }
            public Address Address { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class NullableTestClass
        {
            public int? NullableInt { get; set; }
            public DateTime? NullableDate { get; set; }
            public bool? NullableBool { get; set; }
        }

        public enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        public class EnumTestClass
        {
            public TestEnum EnumValue { get; set; }
            public TestEnum? NullableEnum { get; set; }
        }

        #endregion

        #region Newtonsoft.Json 13.0+ Tests

        [Fact]
        public void NewtonsoftJson_SerializeSimpleObject_ShouldWork()
        {
            // Arrange
            var person = new Person
            {
                Name = "John Doe",
                Age = 30,
                BirthDate = new DateTime(1994, 1, 15),
                Tags = new List<string> { "developer", "tester" },
                Address = new Address { Street = "123 Main St", City = "Seattle", Country = "USA" }
            };

            // Act
            var json = JsonConvert.SerializeObject(person);
            var deserialized = JsonConvert.DeserializeObject<Person>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Name.Should().Be("John Doe");
            deserialized.Age.Should().Be(30);
            deserialized.Tags.Should().HaveCount(2);
            deserialized.Address.City.Should().Be("Seattle");
        }

        [Fact]
        public void NewtonsoftJson_SerializeNullValues_ShouldWork()
        {
            // Arrange
            var obj = new NullableTestClass
            {
                NullableInt = null,
                NullableDate = null,
                NullableBool = true
            };

            // Act
            var json = JsonConvert.SerializeObject(obj);
            var deserialized = JsonConvert.DeserializeObject<NullableTestClass>(json);

            // Assert
            deserialized.NullableInt.Should().BeNull();
            deserialized.NullableDate.Should().BeNull();
            deserialized.NullableBool.Should().BeTrue();
        }

        [Fact]
        public void NewtonsoftJson_SerializeEnums_ShouldWork()
        {
            // Arrange
            var obj = new EnumTestClass
            {
                EnumValue = TestEnum.Value2,
                NullableEnum = TestEnum.Value3
            };

            // Act
            var json = JsonConvert.SerializeObject(obj);
            var deserialized = JsonConvert.DeserializeObject<EnumTestClass>(json);

            // Assert
            deserialized.EnumValue.Should().Be(TestEnum.Value2);
            deserialized.NullableEnum.Should().Be(TestEnum.Value3);
        }

        [Fact]
        public void NewtonsoftJson_CamelCaseNaming_ShouldWork()
        {
            // Arrange
            var person = new Person { Name = "Test", Age = 25 };
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            // Act
            var json = JsonConvert.SerializeObject(person, settings);

            // Assert
            json.Should().Contain("\"name\":");
            json.Should().Contain("\"age\":");
        }

        #endregion

        #region System.Text.Json Tests

        [Fact]
        public void SystemTextJson_SerializeSimpleObject_ShouldWork()
        {
            // Arrange
            var person = new Person
            {
                Name = "Jane Doe",
                Age = 28,
                BirthDate = new DateTime(1996, 5, 20),
                Tags = new List<string> { "analyst", "manager" },
                Address = new Address { Street = "456 Oak Ave", City = "Portland", Country = "USA" }
            };

            // Act
            var json = SystemTextJson.JsonSerializer.Serialize(person);
            var deserialized = SystemTextJson.JsonSerializer.Deserialize<Person>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Name.Should().Be("Jane Doe");
            deserialized.Age.Should().Be(28);
            deserialized.Tags.Should().HaveCount(2);
            deserialized.Address.City.Should().Be("Portland");
        }

        [Fact]
        public void SystemTextJson_SerializeNullValues_ShouldWork()
        {
            // Arrange
            var obj = new NullableTestClass
            {
                NullableInt = 42,
                NullableDate = null,
                NullableBool = false
            };

            // Act
            var json = SystemTextJson.JsonSerializer.Serialize(obj);
            var deserialized = SystemTextJson.JsonSerializer.Deserialize<NullableTestClass>(json);

            // Assert
            deserialized.NullableInt.Should().Be(42);
            deserialized.NullableDate.Should().BeNull();
            deserialized.NullableBool.Should().BeFalse();
        }

        [Fact]
        public void SystemTextJson_CamelCaseNaming_ShouldWork()
        {
            // Arrange
            var person = new Person { Name = "Test", Age = 25 };
            var options = new SystemTextJson.JsonSerializerOptions
            {
                PropertyNamingPolicy = SystemTextJson.JsonNamingPolicy.CamelCase
            };

            // Act
            var json = SystemTextJson.JsonSerializer.Serialize(person, options);

            // Assert
            json.Should().Contain("\"name\":");
            json.Should().Contain("\"age\":");
        }

        #endregion

        #region Compatibility Comparison Tests

        [Fact]
        public void BothSerializers_ShouldProduceSimilarOutput_WithCamelCase()
        {
            // Arrange
            var person = new Person { Name = "Test User", Age = 35 };

            var newtonsoftSettings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            var systemTextJsonOptions = new SystemTextJson.JsonSerializerOptions
            {
                PropertyNamingPolicy = SystemTextJson.JsonNamingPolicy.CamelCase
            };

            // Act
            var newtonsoftJson = JsonConvert.SerializeObject(person, newtonsoftSettings);
            var systemTextJson = SystemTextJson.JsonSerializer.Serialize(person, systemTextJsonOptions);

            // Assert - Both should contain the same property names (camelCase)
            newtonsoftJson.Should().Contain("\"name\":");
            systemTextJson.Should().Contain("\"name\":");
            newtonsoftJson.Should().Contain("\"age\":");
            systemTextJson.Should().Contain("\"age\":");
        }

        [Fact]
        public void NewtonsoftJson_Version13_ShouldBeAvailable()
        {
            // This test validates that Newtonsoft.Json 13.0+ is being used
            var version = typeof(JsonConvert).Assembly.GetName().Version;
            version.Major.Should().BeGreaterOrEqualTo(13);
        }

        #endregion
    }
}
