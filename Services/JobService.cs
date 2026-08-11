using System.Text.Json;
using NearU_Backend_Revised.DTOs.Job;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Repositories;

namespace NearU_Backend_Revised.Services
{
    public class JobService : IJobService
    {
        private static readonly TimeSpan NewJobWindow = TimeSpan.FromHours(24);
        private readonly IJobRepository _repository;
        private readonly UserRepository _userRepository;
        private readonly ICacheService _cache;

        // Single cache key for the most recent full listing page (page 1, default size)
        // Per-page caching would need keyed entries — keep it simple for now.
        private const string JobListCacheKeyPrefix = "nearu:jobs:page";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);

        public JobService(IJobRepository repository, UserRepository userRepository, ICacheService cache)
        {
            _repository = repository;
            _userRepository = userRepository;
            _cache = cache;
        }

        public async Task<PagedJobResponse> GetAllJobsAsync(int page, int pageSize)
        {
            var cacheKey = $"{JobListCacheKeyPrefix}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<PagedJobResponse>(cacheKey);
            if (cached is not null)
                return cached;

            var (jobs, totalCount) = await _repository.GetAllJobsAsync(page, pageSize);
            var result = new PagedJobResponse
            {
                Items = jobs.Select(j => MapToResponse(j)),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };

            await _cache.SetAsync(cacheKey, result, CacheTtl);
            return result;
        }

        public async Task<IEnumerable<JobResponse>> GetNewJobsAsync()
        {
            var jobs = await _repository.GetNewJobsAsync();
            return jobs.Select(j => MapToResponse(j));
        }

        public async Task<IEnumerable<JobResponse>> GetJobsByCategoryAsync(string category)
        {
            var jobs = await _repository.GetJobsByCategoryAsync(category);
            return jobs.Select(j => MapToResponse(j));
        }

        public async Task<IEnumerable<JobResponse>> GetJobsByTypeAsync(string jobType)
        {
            var jobs = await _repository.GetJobsByTypeAsync(jobType);
            return jobs.Select(j => MapToResponse(j));
        }

        public async Task<IEnumerable<JobResponse>> SearchJobsAsync(string searchTerm)
        {
            var jobs = await _repository.SearchJobsAsync(searchTerm);
            return jobs.Select(j => MapToResponse(j));
        }

        public async Task<JobResponse?> GetJobByIdAsync(string id)
        {
            var job = await _repository.GetByIdAsync(id);
            if (job == null) return null;
            return MapToResponse(job);
        }

        public async Task<JobResponse> CreateJobAsync(CreateJob dto, string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            var job = new Job
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title,
                Company = dto.Company,
                Location = dto.Location,
                PayRange = dto.PayRange,
                JobType = dto.JobType,
                Category = dto.Category,
                Logo = dto.Logo,
                Description = dto.Description,
                LongDescription = dto.LongDescription,
                Requirements = dto.Requirements != null ?
                JsonSerializer.Serialize(dto.Requirements) : null,
                Tags = dto.Tags != null ?
                JsonSerializer.Serialize(dto.Tags) : null,
                IsNew = dto.IsNew,
                PostedByName = user.Username,
                PostedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(job);

            // Invalidate all paged cache entries (pages 1-5 covers typical use)
            await InvalidateJobCacheAsync();

            return MapToResponse(created);
        }

        public async Task<JobResponse?> UpdateJobAsync(string id, UpdateJob dto, string userId)
        {
            var job = await _repository.GetByIdAsync(id);
            if (job == null) return null;

            if (job.PostedByUserId != userId)
                throw new UnauthorizedAccessException("You can only update your own job postings.");

            if (dto.Title != null) job.Title = dto.Title;
            if (dto.Company != null) job.Company = dto.Company;
            if (dto.Location != null) job.Location = dto.Location;
            if (dto.PayRange != null) job.PayRange = dto.PayRange;
            if (dto.JobType != null) job.JobType = dto.JobType;
            if (dto.Category != null) job.Category = dto.Category;
            if (dto.Logo != null) job.Logo = dto.Logo;
            if (dto.Description != null) job.Description = dto.Description;
            if (dto.LongDescription != null) job.LongDescription = dto.LongDescription;
            if (dto.Requirements != null) job.Requirements = JsonSerializer.Serialize(dto.Requirements);
            if (dto.Tags != null) job.Tags = JsonSerializer.Serialize(dto.Tags);
            if (dto.IsNew.HasValue) job.IsNew = dto.IsNew.Value;

            job.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(job);
            if (updated == null) return null;

            await InvalidateJobCacheAsync();

            return MapToResponse(updated);
        }

        public async Task<bool> DeleteJobAsync(string id, string userId)
        {
            var job = await _repository.GetByIdAsync(id);
            if (job == null) return false;

            if (job.PostedByUserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own job postings.");

            var result = await _repository.DeleteAsync(id);

            await InvalidateJobCacheAsync();

            return result;
        }

        /// <summary>Removes cached pages 1–10 to cover typical browse usage.</summary>
        private async Task InvalidateJobCacheAsync()
        {
            var tasks = new List<Task>();
            for (int page = 1; page <= 10; page++)
            {
                foreach (var size in new[] { 10, 20, 50 })
                {
                    tasks.Add(_cache.RemoveAsync($"{JobListCacheKeyPrefix}:{page}:{size}"));
                }
            }
            await Task.WhenAll(tasks);
        }

        private static JobResponse MapToResponse(Job job)
        {
            var requirements = new List<string>();
            var tags = new List<string>();

            if (!string.IsNullOrEmpty(job.Requirements))
            {
                requirements = JsonSerializer.Deserialize<List<string>>(job.Requirements) ?? new List<string>();
            }
            if (!string.IsNullOrEmpty(job.Tags))
            {
                tags = JsonSerializer.Deserialize<List<string>>(job.Tags) ?? new List<string>();
            }

            return new JobResponse
            {
                Id = job.Id,
                Title = job.Title,
                Company = job.Company,
                Location = job.Location,
                PayRange = job.PayRange,
                JobType = job.JobType,
                Category = job.Category,
                Logo = job.Logo,
                Description = job.Description,
                LongDescription = job.LongDescription,
                Requirements = requirements,
                Tags = tags,
                IsNew = IsJobNew(job),
                PostedBy = new PostedByInfo
                {
                    UserId = job.PostedByUserId,
                    Name = job.PostedByName,
                    Email = job.PostedByUser?.Email ?? "",
                    MobileNumber = job.PostedByUser?.MobileNumber,
                    Avatar = null
                },
                CreatedAt = job.CreatedAt,
                PostedAt = GetRelativeTime(job.CreatedAt)
            };
        }

        private static bool IsJobNew(Job job)
        {
            return job.IsNew && DateTime.UtcNow - job.CreatedAt <= NewJobWindow;
        }

        private static string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";

            return dateTime.ToString("MMM dd, yyyy");
        }
    }
}
