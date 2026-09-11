using System.ComponentModel.DataAnnotations;
using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Applications;

public class UpdateApplicationStatusRequest
{
    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus Status { get; set; }
}