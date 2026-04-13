using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.Job;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Models;
using System.Security.Claims;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobservice;
        private readonly IImageService _imageService;

        public JobController(IJobService jobservice, IImageService imageService)
        {
            _jobservice = jobservice;
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllJobs()
        {
            try
            {
                var jobs = await _jobservice.GetAllJobsAsync();
                return Ok(ApiResponse<IEnumerable<JobResponse>>.SuccessResponse("Jobs retrieved successfully", jobs));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpGet("new")]
        public async Task<IActionResult> GetNewJobs()
        {
            try
            {
                var jobs = await _jobservice.GetNewJobsAsync();
                return Ok(ApiResponse<IEnumerable<JobResponse>>.SuccessResponse("New jobs retrieved successfully", jobs));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }

        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetJobsByCategory(string category)
        {
            try
            {
                var jobs = await _jobservice.GetJobsByCategoryAsync(category);
                return Ok(ApiResponse<IEnumerable<JobResponse>>.SuccessResponse($"Jobs in category '{category}' retrieved successfully", jobs));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpGet("type/{jobType}")]
        public async Task<IActionResult> GetJobsByType(string jobType)
        {
            try
            {
                var jobs = await _jobservice.GetJobsByTypeAsync(jobType);
                return Ok(ApiResponse<IEnumerable<JobResponse>>.SuccessResponse($"Jobs of type '{jobType}' retrieved successfully", jobs));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchJobs([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                    return BadRequest(ApiResponse<object>.FailResponse("Search term is required"));

                var jobs = await _jobservice.SearchJobsAsync(q);
                return Ok(ApiResponse<IEnumerable<JobResponse>>.SuccessResponse($"Search results for '{q}'", jobs));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(string id)
        {
            try
            {
                var job = await _jobservice.GetJobByIdAsync(id);
                if (job == null)
                    return NotFound(ApiResponse<object>.FailResponse("Job not found"));
                return Ok(ApiResponse<JobResponse>.SuccessResponse("Job retrieved successfully", job));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateJob([FromBody] CreateJob dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid job data"));

                var userId = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User not authenticated"));

                var job = await _jobservice.CreateJobAsync(dto, userId);
                return Created($"/api/job/{job.Id}", ApiResponse<JobResponse>.SuccessResponse("Job created successfully", job));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateJob(string id, [FromBody] UpdateJob dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid job data"));

                var userId = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User not authenticated"));

                var job = await _jobservice.UpdateJobAsync(id, dto, userId);
                if (job == null)
                    return NotFound(ApiResponse<object>.FailResponse("Job not found or user not authorized"));

                return Ok(ApiResponse<JobResponse>.SuccessResponse("Job updated successfully", job));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteJob(string id)
        {
            try
            {
                var userId = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User not authenticated"));
                var success = await _jobservice.DeleteJobAsync(id, userId);
                if (!success)
                    return NotFound(ApiResponse<object>.FailResponse("Job not found or user not authorized"));
                return Ok(ApiResponse<object>.SuccessResponse("Job deleted successfully", new { }));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("upload-logo")]
        [Authorize]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<object>.FailResponse("No file uploaded"));

                var logoUrl = await _imageService.UploadImageAsync(file, "job-logos");
                return Ok(ApiResponse<object>.SuccessResponse("Logo uploaded successfully", new { url = logoUrl }));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.FailResponse($"Upload failed: {ex.Message}"));
            }
        }
    }
}