namespace HireIQ.Application.Interfaces;

/// <summary>
/// Abstraction over HttpContext.User — keeps Application layer free of ASP.NET dependencies.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
