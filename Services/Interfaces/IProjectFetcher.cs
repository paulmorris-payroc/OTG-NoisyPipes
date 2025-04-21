using NoisyPipes.Models;

namespace NoisyPipes.Services.Interfaces
{
    public interface IProjectFetcher
    {
        /// <summary>
        /// Fetches all projects and their pipelines, including latest run information.
        /// </summary>
        /// <param name="organization">Azure DevOps organization name.</param>
        /// <returns>A list of enriched project pipeline data.</returns>
        Task<List<ProjectInfo>> FetchProjectsAndPipelinesAsync(string organization);
    }
}
