using System.ComponentModel.DataAnnotations;
using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Jobs;

public class UpdateJobStatusRequest
{
    [EnumDataType(typeof(JobStatus))]
    public JobStatus Status { get; set; }
}