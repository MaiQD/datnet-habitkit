using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace HabitKitClone.Services;

public class UserContextService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public UserContextService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User?.Identity?.IsAuthenticated ?? false;
    }
}
