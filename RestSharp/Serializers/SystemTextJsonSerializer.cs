using System.Text.Json;

namespace RestSharp.Serializers
{
	/// <summary>
	/// Alternative JSON serializer for request bodies backed by System.Text.Json (net8.0+ only).
	/// Assign to <see cref="RestRequest.JsonSerializer"/> to use instead of the default Json.NET serializer.
	/// </summary>
	public class SystemTextJsonSerializer : ISerializer
	{
		private readonly JsonSerializerOptions _options;

		public SystemTextJsonSerializer() : this(new JsonSerializerOptions { WriteIndented = true })
		{
		}

		public SystemTextJsonSerializer(JsonSerializerOptions options)
		{
			_options = options;
			ContentType = "application/json";
		}

		public string Serialize(object obj) => System.Text.Json.JsonSerializer.Serialize(obj, obj?.GetType() ?? typeof(object), _options);

		/// <summary>
		/// Unused for JSON Serialization
		/// </summary>
		public string DateFormat { get; set; }
		/// <summary>
		/// Unused for JSON Serialization
		/// </summary>
		public string RootElement { get; set; }
		/// <summary>
		/// Unused for JSON Serialization
		/// </summary>
		public string Namespace { get; set; }
		/// <summary>
		/// Content type for serialized content
		/// </summary>
		public string ContentType { get; set; }
	}
}
