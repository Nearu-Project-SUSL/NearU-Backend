using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IPhotographerRepository
    {
        Task<List<Photographer>> GetAllAsync(string? keyword, string? location, bool? isActive);
        Task<Photographer?> GetByIdAsync(Guid id);
        Task<PhotographyPackage?> GetPackageByIdAsync(Guid packageId);
        Task AddPhotographerAsync(Photographer photographer);
        Task AddPackageAsync(PhotographyPackage package);
        void UpdatePhotographer(Photographer photographer);
        void UpdatePackage(PhotographyPackage package);
        void DeletePhotographer(Photographer photographer);
        void DeletePackage(PhotographyPackage package);
        Task<bool> SaveChangesAsync();
    }
}
