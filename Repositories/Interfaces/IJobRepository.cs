using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IJobRepository
    {
        Task<(IEnumerable<Job> Items, int TotalCount)> GetAllJobsAsync(int page, int pageSize);
        Task<IEnumerable<Job>> GetNewJobsAsync();
        Task<IEnumerable<Job>> GetJobsByCategoryAsync(string category);
        Task<IEnumerable<Job>> GetJobsByTypeAsync(string jobType);
        Task<IEnumerable<Job>> SearchJobsAsync(string searchTerm);
        Task<Job?> GetByIdAsync(string id);
        Task<Job> CreateAsync(Job job);
        Task<Job?> UpdateAsync(Job job);
        Task<bool> DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}