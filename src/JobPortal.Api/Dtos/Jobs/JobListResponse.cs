namespace JobPortal.Api.Dtos.Jobs;

public class JobListResponse
{
    public List<JobResponse> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}