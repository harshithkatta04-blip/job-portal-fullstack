using System.ComponentModel.DataAnnotations;
using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Companies;

public class UpdateCompanyStatusRequest
{
    [EnumDataType(typeof(CompanyStatus))]
    public CompanyStatus Status { get; set; }
}