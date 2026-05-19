using System;
using RestSharp.Validation;
using Xunit;

namespace RestSharp.Tests
{
	public class ValidationTests
	{
		[Fact]
		public void IsBetween_Valid_Value_Does_Not_Throw()
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
		public void IsValidLength_Null_Does_Not_Throw()
		{
			Validate.IsValidLength(null, 10);
		}

		[Fact]
		public void IsValidLength_Valid_Length_Does_Not_Throw()
		{
			Validate.IsValidLength("hello", 10);
		}

		[Fact]
		public void IsValidLength_Exact_Max_Does_Not_Throw()
		{
			Validate.IsValidLength("hello", 5);
		}

		[Fact]
		public void IsValidLength_Too_Long_Throws()
		{
			Assert.Throws<ArgumentException>(() => Validate.IsValidLength("hello world", 5));
		}
	}
}
