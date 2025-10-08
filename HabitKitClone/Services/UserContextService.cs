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
        var userId = authState.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"UserContextService.GetCurrentUserIdAsync - IsAuthenticated: {authState.User?.Identity?.IsAuthenticated}, UserId: {userId}");
        return userId;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User?.Identity?.IsAuthenticated ?? false;
    }
}
