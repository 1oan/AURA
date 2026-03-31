using Aura.Application.StudentRecords.Commands.Participate;
using Aura.Application.StudentRecords.Commands.UploadCsv;
using Aura.Application.StudentRecords.Queries.GetMyEligibility;
using Aura.Application.StudentRecords.Queries.GetStudentRecords;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/student-records")]
[Authorize]
public class StudentRecordsController(ISender sender) : ControllerBase
{
    [HttpPost("upload/{allocationPeriodId:guid}")]
    [Authorize(Roles = "FacultyAdmin")]
    public async Task<IActionResult> Upload(Guid allocationPeriodId, IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await sender.Send(new UploadCsvCommand(allocationPeriodId, stream));
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,FacultyAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid allocationPeriodId,
        [FromQuery] Guid? facultyId)
    {
        return Ok(await sender.Send(new GetStudentRecordsQuery(allocationPeriodId, facultyId)));
    }

    [HttpPost("participate/{allocationPeriodId:guid}")]
    [Authorize]
    public async Task<IActionResult> Participate(Guid allocationPeriodId, ParticipateCommand command)
    {
        var result = await sender.Send(command with { AllocationPeriodId = allocationPeriodId });
        return Ok(result);
    }

    [HttpGet("my-eligibility/{allocationPeriodId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetMyEligibility(Guid allocationPeriodId)
    {
        return Ok(await sender.Send(new GetMyEligibilityQuery(allocationPeriodId)));
    }
}
