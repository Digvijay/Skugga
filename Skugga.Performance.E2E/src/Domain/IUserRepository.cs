namespace Skugga.Performance.E2E.Domain;

public interface IUserRepository
{
    string GetUserRole(int userId);
}
