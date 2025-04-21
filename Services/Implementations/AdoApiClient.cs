using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoisyPipes.Configuration;
using NoisyPipes.Models;
using NoisyPipes.Services.Interfaces;
using NoisyPipes.Configuration;
using NoisyPipes.Models;
using NoisyPipes.Services.Interfaces;

namespace NoisyPipes.Services.Implementations
{
    public class AdoApiClient : IAdoApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;
        private readonly ILogger<AdoApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AdoApiClient(HttpClient httpClient, IOptions<AppSettings> settings, ILogger<AdoApiClient> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_settings.PersonalAccessToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<AzureDevOpsProject>> GetProjectsAsync()
        {
            var url = $"https://dev.azure.com/{_settings.Organization}/_apis/projects?api-version=7.0";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch projects. Status code: {StatusCode}", response.StatusCode);
                return new List<AzureDevOpsProject>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProjectListResponse>(json, _jsonOptions);

            return result?.Value ?? new List<AzureDevOpsProject>();
        }

        public async Task<List<AzureDevOpsPipeline>> GetPipelinesAsync(string projectName)
        {
            var url = $"https://dev.azure.com/{_settings.Organization}/{projectName}/_apis/pipelines?api-version=7.0";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch pipelines for project {Project}. Status code: {StatusCode}", projectName, response.StatusCode);
                return new List<AzureDevOpsPipeline>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PipelineListResponse>(json, _jsonOptions);

            return result?.Value ?? new List<AzureDevOpsPipeline>();
        }

        public async Task<List<AzureDevOpsPipelineRun>> GetLatestRunsAsync(string projectName, int pipelineId, int top = 1)
        {
            var url = $"https://dev.azure.com/{_settings.Organization}/{projectName}/_apis/pipelines/{pipelineId}/runs?api-version=7.0&$top={top}&$orderby=createdDate desc";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch runs for pipeline {PipelineId} in project {Project}. Status code: {StatusCode}", pipelineId, projectName, response.StatusCode);
                return new List<AzureDevOpsPipelineRun>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PipelineRunListResponse>(json, _jsonOptions);

            return result?.Value ?? new List<AzureDevOpsPipelineRun>();
        }

        // Internal DTOs for deserialization
        private class ProjectListResponse
        {
            public int Count { get; set; }
            public List<AzureDevOpsProject> Value { get; set; }
        }

        private class PipelineListResponse
        {
            public int Count { get; set; }
            public List<AzureDevOpsPipeline> Value { get; set; }
        }

        private class PipelineRunListResponse
        {
            public int Count { get; set; }
            public List<AzureDevOpsPipelineRun> Value { get; set; }
        }
    }
}
