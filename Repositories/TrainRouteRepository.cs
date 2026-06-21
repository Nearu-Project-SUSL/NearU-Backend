using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using  NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class TrainRouteRepository : ITrainRouteRepository
    {
        private readonly ApplicationDbContext _context;

        public TrainRouteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TrainRoute>> GetAllAsync()
        {
            return await _context.TrainRoutes
                .AsNoTracking()
                .OrderBy(r => r.DepartureTime)
                .ToListAsync();
        }

        public async Task<TrainRoute?> GetByIdAsync(int id)
        {
            return await _context.TrainRoutes
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<TrainRoute> AddAsync(TrainRoute route)
        {
            _context.TrainRoutes.Add(route);
            await _context.SaveChangesAsync();
            return route;
        }

        public async Task<TrainRoute?> UpdateAsync(TrainRoute route)
        {
            var existing = await _context.TrainRoutes.FindAsync(route.Id);
            if (existing == null) return null;

            existing.RouteName = route.RouteName;
            existing.StartStation = route.StartStation;
            existing.EndStation = route.EndStation;
            existing.DepartureTime = route.DepartureTime;
            existing.ArrivalTime = route.ArrivalTime;
            existing.TrainName = route.TrainName;
            existing.Notes = route.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.TrainRoutes.FindAsync(id);
            if (existing == null) return false;

            _context.TrainRoutes.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}