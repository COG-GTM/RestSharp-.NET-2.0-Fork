using System;
using RestSharp.Validation;
using Xunit;

namespace RestSharp.Tests
{
	public class ValidationTests
	{
		[Fact]
		public void IsBetween_Valid_Does_Not_Throw()
		{
			Validate.IsBetween(5, 1, 10);
		}

		[Fact]
		public void IsBetween_At_Min_Does_Not_Throw()
		{
			Validate.IsBetween(1, 1, 10);
		}

		[Fact]
		public void IsBetween_At_Max_Does_Not_Throw()
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
		public void IsValidLength_Valid_Does_Not_Throw()
		{
			Validate.IsValidLength("hello", 10);
		}

		[Fact]
		public void IsValidLength_Exact_Max_Does_Not_Throw()
		{
			Validate.IsValidLength("hello", 5);
		}

		[Fact]
		public void IsValidLength_Null_Does_Not_Throw()
		{
			Validate.IsValidLength(null, 5);
		}

		[Fact]
		public void IsValidLength_Too_Long_Throws()
		{
			Assert.Throws<ArgumentException>(() => Validate.IsValidLength("hello world", 5));
		}

		[Fact]
		public void IsValidLength_Empty_String_Does_Not_Throw()
		{
			Validate.IsValidLength("", 0);
		}

		[Fact]
		public void Require_Argument_Valid_Does_Not_Throw()
		{
			Require.Argument("param", "value");
		}

		[Fact]
		public void Require_Argument_Null_Throws()
		{
			Assert.Throws<ArgumentException>(() => Require.Argument("param", null));
		}

		[Fact]
		public void Require_Argument_Object_Does_Not_Throw()
		{
			Require.Argument("param", new object());
		}
	}
}
