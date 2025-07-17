using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AuthenticationDemo.Blazor.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly TokenHolder _tokenHolder;

        public ApiService(HttpClient httpClient, TokenHolder tokenHolder)
        {
            _httpClient = httpClient;
            _tokenHolder = tokenHolder;
        }


        public async Task<T> GetAsync<T>(string uri)
        {
            await AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<T>(uri);
        }

        public async Task<T?> GetAsync<T>(string uri, Dictionary<string, string>? headers)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string uri, T value)
        {
            await AddAuthorizationHeader();
            return await _httpClient.PostAsJsonAsync(uri, value);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string uri, T value)
        {
            await AddAuthorizationHeader();
            return await _httpClient.PutAsJsonAsync(uri, value);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string uri)
        {
            await AddAuthorizationHeader();
            return await _httpClient.DeleteAsync(uri);
        }

        private Task AddAuthorizationHeader()
        {
            if (!string.IsNullOrEmpty(_tokenHolder.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenHolder.Token);
            }
            return Task.CompletedTask;
        }
    }
}