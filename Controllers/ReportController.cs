using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoisyPipes.Configuration;
using NoisyPipes.Services.Interfaces;
using NoisyPipes.Generators.Interfaces;
using NoisyPipes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NoisyPipes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IAdoApiClient _adoApiClient;
        private readonly IProjectFetcher _projectFetcher;
        private readonly IReportGenerator _reportGenerator;
        private readonly AppSettings _appSettings;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IAdoApiClient adoApiClient,
            IProjectFetcher projectFetcher,
            IReportGenerator reportGenerator,
            IOptions<AppSettings> appSettings,
            ILogger<ReportController> logger)
        {
            _adoApiClient = adoApiClient;
            _projectFetcher = projectFetcher;
            _reportGenerator = reportGenerator;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        // ✅ New: Just serve the HTML template immediately
        [HttpGet("generate")]
        public IActionResult Generate()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "ReportTemplate.html");
                if (!System.IO.File.Exists(filePath))
                    return NotFound("Report template not found.");

                var htmlContent = System.IO.File.ReadAllText(filePath);
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load HTML template.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ✅ Still returns live Azure DevOps pipeline data
        [HttpGet("/api/pipelines")]
        public async Task<IActionResult> GetProjectData()
        {
            try
            {
                List<ProjectInfo> projects = await _projectFetcher.FetchProjectsAndPipelinesAsync(_appSettings.Organization);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch pipeline data.");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
