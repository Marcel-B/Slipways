using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Services
{

    public abstract class TokenService<DTO> : ITokenService<DTO> where DTO : class
    {
        private IWebHostEnvironment _environment;
        private ILogger<TokenService<DTO>> _logger;
        protected JsonSerializerOptions _jsonOptions;
        protected HttpClient _client { get; set; }

        private ApplicationInfo _applicationInfo;
        protected const string ApplicationJson = "application/json";
        protected string ApiPath { get; set; }

        protected TokenService(
            HttpClient client,
            ApplicationInfo applicationInfo,
            IWebHostEnvironment environment,
            ILogger<TokenService<DTO>> logger)
        {
            _client = client;
            _applicationInfo = applicationInfo;
            _environment = environment;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                IgnoreNullValues = true
            };
        }

        public async Task<DTO> DeleteAsync(
            Guid id)
        {
            var url = $"{_applicationInfo.ApiEndpoint}/api/{ApiPath}/{id}";
            if (_environment.IsDevelopment())
                url = $"{_applicationInfo.ApiEndpoint}/api/{ApiPath}/{id}";
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(url),
                };

                var result = await _client.SendAsync(request);

                if (!result.IsSuccessStatusCode)
                    return null;

                return await JsonSerializer.DeserializeAsync<DTO>(await result.Content.ReadAsStreamAsync(), _jsonOptions);
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(6661, $"Error occurred while deleting '{id}'", e);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(6662, $"Error occurred while deleting '{id}'", e);
            }
            catch (UriFormatException e)
            {
                _logger.LogError(6663, $"Error occurred while deleting '{id}'", e);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(6664, $"Error occurred while deleting '{id}'", e);
            }
            catch (JsonException e)
            {
                _logger.LogError(6665, $"Error occurred while deleting '{id}'", e);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, $"Unexpected error occurred while deleting '{id}'", e);
            }
            return null;
        }

        public async Task<DTO> InsertAsync(
            DTO item)
        {
            string json = string.Empty;
            try
            {
                json = JsonSerializer.Serialize(item, _jsonOptions);

                var content = new StringContent(json, Encoding.UTF8, ApplicationJson);

                var response = await _client.PostAsync($"api/{ApiPath}", content);

                if (!response.IsSuccessStatusCode)
                {
                    return default;
                }

                var responseContent = await response.Content.ReadAsStreamAsync();
                var obj = await JsonSerializer.DeserializeAsync<DTO>(responseContent);
                return obj;
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(6661, $"Error occurred while inserting new item\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(6662, $"Error occurred while inserting new item\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (UriFormatException e)
            {
                _logger.LogError(6663, $"Error occurred while inserting new item\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(6664, $"Error occurred while inserting new item\nItem: ''\nUrl: {_client.BaseAddress}\nMaybe API is not running", e);
            }
            catch (JsonException e)
            {
                _logger.LogError(6665, $"Error occurred while inserting new item\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, $"Unexpected error occurred while inserting new item\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            return null;
        }

        public async Task<DTO> UpdateAsync(
            Guid id,
            DTO item)
        {
            var json = string.Empty;
            try
            {
                json = JsonSerializer.Serialize(item, _jsonOptions);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"{_applicationInfo.ApiEndpoint}/api/{ApiPath}/{id}"),
                    Content = new StringContent(json, Encoding.UTF8, ApplicationJson),
                };

                var result = await _client.SendAsync(request);

                if (!result.IsSuccessStatusCode)
                    return null;

                return await JsonSerializer.DeserializeAsync<DTO>(await result.Content.ReadAsStreamAsync(), _jsonOptions);
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(6661, $"Error occurred while updating '{id}'\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(6662, $"Error occurred while updating '{id}'\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (UriFormatException e)
            {
                _logger.LogError(6663, $"Error occurred while updating '{id}'\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(6664, $"Error occurred while updating '{id}'\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (JsonException e)
            {
                _logger.LogError(6665, $"Error occurred while updating '{id}'\nItem: ''\nUrl: {_client.BaseAddress}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, $"Unexpected error occurred while updating '{id}'\nItem: '{json}'\nUrl: {_client.BaseAddress}", e);
            }
            return null;
        }
    }
}
