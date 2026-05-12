using NearU_Backend_Revised.DTOs.Job;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IJobService
    {
        Task<PagedJobResponse> GetAllJobsAsync(int page, int pageSize);
        Task<IEnumerable<JobResponse>> GetNewJobsAsync();
        Task<IEnumerable<JobResponse>> GetJobsByCategoryAsync(string category);
        Task<IEnumerable<JobResponse>> GetJobsByTypeAsync(string jobType);
        Task<IEnumerable<JobResponse>> SearchJobsAsync(string searchTerm);
        Task<JobResponse?> GetJobByIdAsync(string id);
        Task<JobResponse> CreateJobAsync(CreateJob dto, string userId);
        Task<JobResponse?> UpdateJobAsync(string id, UpdateJob dto, string userId);
        Task<bool> DeleteJobAsync(string id, string userId);
    }
}
