// IRAS.API/Controllers/JobMatchesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Matching;

namespace IRAS.API.Controllers
{
    // Module 8 — proactive matches the system found for this candidate, as opposed to
    // applications the candidate submitted themselves (see ApplicationsController).
    [ApiController]
    [Route("api/candidates/{candidateId:int}/job-matches")]
    [Authorize]
    public class JobMatchesController : ControllerBase
    {
        private readonly IJobMatchingService _service;
        public JobMatchesController(IJobMatchingService service) => _service = service;

        private IActionResult? CheckAccess(int candidateId)
        {
            var role = User.GetRole();
            if (role == "Admin") return null;
            if (role == "Candidate" && User.GetUserId() == candidateId) return null;
            return Forbid();
        }

        [HttpGet]
        public async Task<IActionResult> GetMine(int candidateId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.GetMyMatchesAsync(candidateId, ct));
        }
    }
}
