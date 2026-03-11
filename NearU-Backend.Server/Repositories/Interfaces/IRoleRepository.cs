using NearU_Backend.Server.Models;

namespace NearU_Backend.Server.Repositories.Interfaces;

/// <summary>
/// Repository interface for Role operations.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetByNameAsync(string name);
    Task<IEnumerable<Role>> GetAllAsync();
}
