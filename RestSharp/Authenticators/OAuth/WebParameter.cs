using System;
using System.Diagnostics;


namespace RestSharp.Authenticators.OAuth
{
	[DebuggerDisplay("{Name}:{Value}")]
	[Serializable]
	internal class WebParameter : WebPair
	{
		public WebParameter(string name, string value) : base(name, value)
		{
		}
	}
}