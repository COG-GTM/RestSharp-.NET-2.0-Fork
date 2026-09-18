using System.Threading;
using System.Threading.Tasks;

namespace RestSharp
{
    public static class RestClientExtensions
    {
        /// <summary>
        /// Executes the request asynchronously and returns the deserialized data, authenticating if needed
        /// </summary>
        /// <param name="client">The IRestClient this method extends</param>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        /// <param name="cancellationToken">Token used to cancel the request</param>
        public static async Task<T> GetAsync<T>(this IRestClient client, IRestRequest request, CancellationToken cancellationToken = default(CancellationToken)) where T : new()
        {
            var response = await client.ExecuteAsync<T>(request, cancellationToken).ConfigureAwait(false);
            return response.Data;
        }
    }
}
