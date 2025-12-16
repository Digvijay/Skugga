using Microsoft.AspNetCore.Http.HttpResults;
using Skugga.Performance.E2E.Domain;

namespace Skugga.Performance.E2E.Api;

public record UserResponse(string Role);

public class UserHandler(IUserRepository repo)
{
    public Results<Ok<UserResponse>, NotFound> GetUser(int id)
    {
        var role = repo.GetUserRole(id);
        if (role == null) return TypedResults.NotFound();
        return TypedResults.Ok(new UserResponse(role));
    }
}
