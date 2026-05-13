using System;
using RestSharp.Validation;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class ValidateTests
	{
		[Fact]
		public void IsBetween_Does_Not_Throw_For_Valid_Value()
		{
			Validate.IsBetween(5, 1, 10);
		}

		[Fact]
		public void IsBetween_At_Min_Boundary_Does_Not_Throw()
		{
			Validate.IsBetween(1, 1, 10);
		}

		[Fact]
		public void IsBetween_At_Max_Boundary_Does_Not_Throw()
		{
			Validate.IsBetween(10, 1, 10);
		}

		[Fact]
		public void IsBetween_Below_Min_Throws()
		{
			Assert.Throws<ArgumentException>(() => Validate.IsBetween(0, 1, 10));
		}

		[Fact]
		public void IsBetween_Above_Max_Throws()
		{
			Assert.Throws<ArgumentException>(() => Validate.IsBetween(11, 1, 10));
		}

		[Fact]
		public void IsValidLength_Does_Not_Throw_For_Valid_String()
		{
			Validate.IsValidLength("hello", 10);
		}

		[Fact]
		public void IsValidLength_At_Max_Does_Not_Throw()
		{
			Validate.IsValidLength("hello", 5);
		}

		[Fact]
		public void IsValidLength_Over_Max_Throws()
		{
			Assert.Throws<ArgumentException>(() => Validate.IsValidLength("hello world", 5));
		}

		[Fact]
		public void IsValidLength_Null_Does_Not_Throw()
		{
			Validate.IsValidLength(null, 5);
		}
	}

	public class RequireTests
	{
		[Fact]
		public void Argument_Does_Not_Throw_For_NonNull()
		{
			Require.Argument("param", "value");
		}

		[Fact]
		public void Argument_Throws_For_Null()
		{
			Assert.Throws<ArgumentException>(() => Require.Argument("param", null));
		}

		[Fact]
		public void Argument_Throws_With_Correct_ParamName()
		{
			var ex = Assert.Throws<ArgumentException>(() => Require.Argument("myParam", null));
			Assert.Contains("myParam", ex.ParamName);
		}
	}
}
