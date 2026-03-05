using Microsoft.EntityFrameworkCore;
using NearU_Backend.Server.Data;
using NearU_Backend.Server.Models;
using NearU_Backend.Server.Repositories.Interfaces;

namespace NearU_Backend.Server.Repositories;

/// <summary>
/// Repository implementation for Role operations.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly NearUDbContext _context;

    public RoleRepository(NearUDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }
}
