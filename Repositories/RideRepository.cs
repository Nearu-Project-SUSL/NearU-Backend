using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NetTopologySuite.Geometries;

namespace NearU_Backend_Revised.Repositories
{
    public class RideRepository : IRideRepository
    {
        private readonly ApplicationDbContext _context;

        public RideRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RideRequest> CreateAsync(RideRequest ride)
        {
            _context.RideRequests.Add(ride);
            await _context.SaveChangesAsync();
            return ride;
        }

        public async Task<RideRequest?> GetByIdAsync(Guid id)
        {
            return await _context.RideRequests.FindAsync(id);
        }

        public async Task<RideRequest?> GetByIdWithLockAsync(Guid id)
        {
            return await _context.RideRequests
                .FromSqlRaw(
                    @"SELECT * FROM ""RideRequests"" WHERE ""Id"" = {0} AND ""Status"" = 'Pending' FOR UPDATE",
                    id)
                .FirstOrDefaultAsync();
        }

        public async Task<RideRequest?> UpdateAsync(RideRequest ride)
        {
            _context.RideRequests.Update(ride);
            await _context.SaveChangesAsync();
            return ride;
        }

        public async Task<IEnumerable<RideRequest>> GetPendingOlderThanAsync(DateTime cutoff)
        {
            return await _context.RideRequests
                .Where(r => r.Status == "Pending" && r.CreatedAt < cutoff)
                .ToListAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Uses EF Core's built-in IsWithinDistance which translates to
        // PostGIS ST_DWithin(geography, geography, meters) in the actual SQL query
        public async Task<bool> IsWithinUniversityRadiusAsync(
            Point pickup, Point university, double radiusMeters)
        {
            return await _context.RideRequests
                .Where(r => pickup.IsWithinDistance(university, radiusMeters))
                .AnyAsync();
        }
    }
}