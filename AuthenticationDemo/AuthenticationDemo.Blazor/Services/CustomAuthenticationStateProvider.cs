using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthenticationDemo.Blazor.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly TokenHolder _tokenHolder;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(TokenHolder tokenHolder)
        {
            _tokenHolder = tokenHolder;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!string.IsNullOrEmpty(_tokenHolder.Token) && !string.IsNullOrEmpty(_tokenHolder.UserEmail))
            {
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, _tokenHolder.UserEmail),
                }, "apiauth");

                _currentUser = new ClaimsPrincipal(identity);
            }
            else
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }

            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        public void MarkUserAsAuthenticated(string email)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email),
            }, "apiauth");

            _currentUser = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void MarkUserAsLoggedOut()
        {
            _tokenHolder.Token = null;
            _tokenHolder.UserEmail = null;
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}