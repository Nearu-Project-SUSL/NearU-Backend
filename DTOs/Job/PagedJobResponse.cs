namespace NearU_Backend_Revised.DTOs.Job
{
    public class PagedJobResponse
    {
        public IEnumerable<JobResponse> Items { get; set; } = new List<JobResponse>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
