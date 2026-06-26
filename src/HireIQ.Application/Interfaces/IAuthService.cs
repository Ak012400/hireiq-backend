using HireIQ.Application.DTOs;

namespace HireIQ.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO?> RegisterAsync(RegisterDTO dto, CancellationToken ct = default);
    Task<AuthResponseDTO?> LoginAsync(LoginDTO dto, CancellationToken ct = default);
}
