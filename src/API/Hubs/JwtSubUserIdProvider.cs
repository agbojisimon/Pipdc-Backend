using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace PIPDC.API.Hubs;

// Returns the JWT "sub" claim as the SignalR UserIdentifier so SignalR uses the
// same identity model as REST (sub = application user id). The client never
// supplies its own identity, and no mutable user property (name/email) is used.
// A missing sub claim yields null (anonymous identity), which is already
// rejected by the [Authorize] attribute on the hub.
public sealed class JwtSubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
