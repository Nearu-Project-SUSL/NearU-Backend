using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using  NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class BusRouteRepository : IBusRouteRepository
    {
        private readonly ApplicationDbContext _context;

        public BusRouteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BusRoute>> GetAllAsync()
        {
            return await _context.BusRoutes
                .AsNoTracking()
                .OrderBy(r => r.DepartureTime)
                .ToListAsync();
        }

        public async Task<BusRoute?> GetByIdAsync(int id)
        {
            return await _context.BusRoutes
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<BusRoute> AddAsync(BusRoute route)
        {
            _context.BusRoutes.Add(route);
            await _context.SaveChangesAsync();
            return route;
        }

        public async Task<BusRoute?> UpdateAsync(BusRoute route)
        {
            var existing = await _context.BusRoutes.FindAsync(route.Id);
            if (existing == null) return null;

            existing.RouteName = route.RouteName;
            existing.StartPoint = route.StartPoint;
            existing.EndPoint = route.EndPoint;
            existing.DepartureTime = route.DepartureTime;
            existing.ArrivalTime = route.ArrivalTime;
            existing.BusNumber = route.BusNumber;
            existing.Notes = route.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.BusRoutes.FindAsync(id);
            if (existing == null) return false;

            _context.BusRoutes.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}