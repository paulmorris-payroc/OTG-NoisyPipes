using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoisyPipes.Configuration;
using NoisyPipes.Services.Interfaces;
using NoisyPipes.Generators.Interfaces;

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

        [HttpGet("generate")]
        public async Task<IActionResult> GenerateReport()
        {
            _logger.LogInformation("Starting report generation...");

            try
            {
                var projects = await _projectFetcher.FetchProjectsAndPipelinesAsync(_appSettings.Organization);
                var html = _reportGenerator.Generate(projects);

                // Return the HTML directly to the browser
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
