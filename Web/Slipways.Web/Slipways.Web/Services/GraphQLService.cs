using com.b_velop.Slipways.Web.Contracts;
using GraphQL;
using GraphQL.Client;
using GraphQL.Client.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Services
{
    public class GraphQLService : IGraphQLService
    {
        private ILogger<GraphQLService> _logger;
        private GraphQLHttpClient _client;

        public GraphQLService(
            GraphQLHttpClient client,
            ILogger<GraphQLService> logger)
        {
            _logger = logger;
            _client = client;
        }

        private async Task<T> GetAsync<T>(
        string query,
        string name)
        {
            var result = await _client.SendQueryAsync<T>(new GraphQLRequest(query, operationName: name));

            if (result == null)
                return default;

            return result.Data;
        }

        public async Task<T> GetValuesAsync<T>(
            string method,
            string query)
        {
            try
            {
                var result = await GetAsync<T>(query, method);
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(6666, $"Error occurred while calling GraphQL\nQuery: '{query}'\nMethod: '{method}'", e);
                return default;
            }
        }
    }
}
