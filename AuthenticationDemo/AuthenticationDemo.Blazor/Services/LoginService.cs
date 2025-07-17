using AuthenticationDemo.Blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AuthenticationDemo.Blazor.Services
{
    public class LoginService
    {
        private readonly ApiService _apiService;
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly TokenHolder _tokenHolder;
        private readonly IConfiguration _configuration;

        public LoginService(ApiService apiService, NavigationManager navigationManager, AuthenticationStateProvider authenticationStateProvider, TokenHolder tokenHolder, IConfiguration configuration)
        {
            _apiService = apiService;
            _navigationManager = navigationManager;
            _authenticationStateProvider = authenticationStateProvider;
            _tokenHolder = tokenHolder;
            _configuration = configuration;
        }

        public async Task<string?> HandleLogin(LoginModel model)
        {
            var clientId = _configuration["ClientInfo:ClientId"];
            var clientSecret = _configuration["ClientInfo:ClientSecret"];
            var scope = _configuration["ClientInfo:Scope"];
            var apiBaseUrl = _configuration["Api:BaseUrl"];

            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", model.Email },
                { "password", model.Password },
                { "scope", scope },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
            var response = await httpClient.PostAsync("connect/token", new FormUrlEncodedContent(tokenRequest));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var accessToken = doc.RootElement.GetProperty("access_token").GetString();
                _tokenHolder.Token = accessToken;
                _tokenHolder.UserEmail = model.Email;
                ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(model.Email);
                _navigationManager.NavigateTo("/");
                return null;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return error ?? "An unknown error occurred.";
            }
        }

        public async Task Logout()
        {
            await _apiService.PostAsync<object>("api/Account/logout", null);
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
            _navigationManager.NavigateTo("/login");
        }
    }
}