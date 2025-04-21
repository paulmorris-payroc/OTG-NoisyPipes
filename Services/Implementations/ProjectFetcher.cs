using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoisyPipes.Models;
using NoisyPipes.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoisyPipes.Configuration;
using NoisyPipes.Services.Interfaces;

namespace NoisyPipes.Services.Implementations
{
    public class ProjectFetcher : IProjectFetcher
    {
        private readonly IAdoApiClient _adoApiClient;
        private readonly AppSettings _settings;
        private readonly ILogger<ProjectFetcher> _logger;

        public ProjectFetcher(
            IAdoApiClient adoApiClient,
            IOptions<AppSettings> settings,
            ILogger<ProjectFetcher> logger)
        {
            _adoApiClient = adoApiClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<List<ProjectInfo>> FetchProjectsAndPipelinesAsync(string organization)
        {
            var result = new List<ProjectInfo>();
            var projects = await _adoApiClient.GetProjectsAsync();

            foreach (var project in projects)
            {
                _logger.LogInformation("Fetching pipelines for project: {ProjectName}", project.Name);
                var pipelines = await _adoApiClient.GetPipelinesAsync(project.Name);

                var projectInfo = new ProjectInfo
                {
                    ProjectName = project.Name,
                    Pipelines = new List<PipelineInfo>()
                };

                foreach (var pipeline in pipelines)
                {
                    var runs = await _adoApiClient.GetLatestRunsAsync(project.Name, pipeline.Id);
                    var latestRun = runs.FirstOrDefault();

                    var pipelineInfo = new PipelineInfo
                    {
                        Id = pipeline.Id,
                        Name = pipeline.Name,
                        Folder = pipeline.Folder,
                        Url = $"https://dev.azure.com/{_settings.Organization}/{project.Name}/_build?definitionId={pipeline.Id}&view=ms.vss-pipelineanalytics-web.new-build-definition-pipeline-analytics-view-cardmetrics",
                        LastRunDate = latestRun?.CreatedDate,
                        LastRunState = latestRun?.State,
                        LastRunResult = latestRun?.Result,
                        NumberOfRuns = runs.Count
                    };

                    projectInfo.Pipelines.Add(pipelineInfo);
                }

                result.Add(projectInfo);
            }

            return result;
        }
    }
}
