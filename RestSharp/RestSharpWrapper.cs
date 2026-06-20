// This project now consumes RestSharp via NuGet package.
// All RestSharp types (RestClient, RestRequest, RestResponse, etc.)
// are provided by the RestSharp NuGet package (v112+).
//
// This wrapper project exists to maintain the solution structure
// and can be used to add any project-specific extensions.

global using RestSharp;
global using RestSharp.Authenticators;
global using RestSharp.Serializers;
