using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NearU_Backend_Revised.Repositories
{
    public class PhotographerRepository : IPhotographerRepository
    {
        private readonly ApplicationDbContext _context;

        public PhotographerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Photographer>> GetAllAsync(string? keyword, string? location, bool? isActive)
        {
            IQueryable<Photographer> query = _context.Photographers
                .Include(p => p.Packages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(keyword) || 
                                         (p.Bio != null && p.Bio.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                location = location.Trim().ToLower();
                query = query.Where(p => p.LocationName.ToLower().Contains(location));
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Photographer?> GetByIdAsync(Guid id)
        {
            return await _context.Photographers
                .Include(p => p.Packages)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PhotographyPackage?> GetPackageByIdAsync(Guid packageId)
        {
            return await _context.PhotographyPackages.FirstOrDefaultAsync(pp => pp.Id == packageId);
        }

        public async Task AddPhotographerAsync(Photographer photographer)
        {
            await _context.Photographers.AddAsync(photographer);
        }

        public async Task AddPackageAsync(PhotographyPackage package)
        {
            await _context.PhotographyPackages.AddAsync(package);
        }

        public void UpdatePhotographer(Photographer photographer)
        {
            _context.Photographers.Update(photographer);
        }

        public void UpdatePackage(PhotographyPackage package)
        {
            _context.PhotographyPackages.Update(package);
        }

        public void DeletePhotographer(Photographer photographer)
        {
            _context.Photographers.Remove(photographer);
        }

        public void DeletePackage(PhotographyPackage package)
        {
            _context.PhotographyPackages.Remove(package);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
