using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Services
{

    public class WaterService : TokenService<WaterDto>, IWaterService
    {
        public WaterService(
            HttpClient client,
            ApplicationInfo applicationInfo,
            IWebHostEnvironment environment,
            ILogger<WaterService> logger) : base(client, applicationInfo, environment, logger)
        {
            ApiPath = "water";
        }

        public async Task<WaterDto> UpdateWaterAsync(
            WaterDto waterDto)
        {
            var jsonContent = JsonSerializer.Serialize(waterDto, _jsonOptions);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://slipways-api:8095/api/water/{waterDto.Id}"),
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var result = await _client.SendAsync(request);

            if (!result.IsSuccessStatusCode)
            {
                //_logger.LogWarning($"Failed to load update Water {waterDto.Longname}. Status Code: '{(int)result.StatusCode}: {result.ReasonPhrase}' '{jsonContent}'");
                return null;
            }
            var resultObj = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WaterDto>(resultObj, _jsonOptions);
        }
    }
}
