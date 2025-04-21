using NoisyPipes.Models;

namespace NoisyPipes.Services.Interfaces
{
    public interface IAdoApiClient
    {
        /// <summary>
        /// Fetches all projects in the Azure DevOps organization.
        /// </summary>
        Task<List<AzureDevOpsProject>> GetProjectsAsync();

        /// <summary>
        /// Fetches all pipelines for a given Azure DevOps project.
        /// </summary>
        Task<List<AzureDevOpsPipeline>> GetPipelinesAsync(string projectName);

        /// <summary>
        /// Fetches the latest pipeline runs for a specific pipeline in a project.
        /// </summary>
        Task<List<AzureDevOpsPipelineRun>> GetLatestRunsAsync(string projectName, int pipelineId, int top = 1);
    }
}
