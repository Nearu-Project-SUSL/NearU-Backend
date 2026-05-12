using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class JobRepository : IJobRepository
    {
        private static readonly TimeSpan NewJobWindow = TimeSpan.FromHours(24);
        private readonly ApplicationDbContext _context;

        public JobRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Job> Items, int TotalCount)> GetAllJobsAsync(int page, int pageSize)
        {
            // Lightweight count — no JOIN needed
            var totalCount = await _context.Jobs.CountAsync();

            // Data query — only fetches the requested page with user info
            var items = await _context.Jobs
                .Include(j => j.PostedByUser)
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Job>> GetNewJobsAsync()
        {
            var cutoff = DateTime.UtcNow - NewJobWindow;

            return await _context.Jobs
                .Include(j => j.PostedByUser)
                .Where(j => j.IsNew && j.CreatedAt >= cutoff)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> GetJobsByCategoryAsync(string category)
        {
            return await _context.Jobs
                .Include(j => j.PostedByUser)
                .Where(j => j.Category == category)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> GetJobsByTypeAsync(string jobType)
        {
            return await _context.Jobs
                .Include(j => j.PostedByUser)
                .Where(j => j.JobType == jobType)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> SearchJobsAsync(string searchTerm)
        {
            var pattern = $"%{searchTerm}%";
            return await _context.Jobs
                .Include(j => j.PostedByUser)
                .Where(j => EF.Functions.ILike(j.Title, pattern) ||
                            EF.Functions.ILike(j.Company, pattern) ||
                            EF.Functions.ILike(j.Location, pattern) ||
                            EF.Functions.ILike(j.Description, pattern))
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<Job?> GetByIdAsync(string id)
        {
            return await _context.Jobs
                .Include(j => j.PostedByUser)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<Job> CreateAsync(Job job)
        {
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<Job?> UpdateAsync(Job job)
        {
            _context.Jobs.Update(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return false;

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.Jobs.AnyAsync(j => j.Id == id);
        }
    }
}
