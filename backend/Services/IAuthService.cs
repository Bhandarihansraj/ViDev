using System.Threading.Tasks;
using ViDev.Api.Dtos;

namespace ViDev.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
