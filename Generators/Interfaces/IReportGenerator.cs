using NoisyPipes.Models;
using System.Collections.Generic;

namespace NoisyPipes.Generators.Interfaces
{
    public interface IReportGenerator
    {
        /// <summary>
        /// Generates a complete HTML report using the provided project and pipeline data.
        /// </summary>
        /// <param name="projects">List of project pipeline data to include in the report.</param>
        /// <returns>HTML string containing the full report content.</returns>
        string Generate(List<ProjectInfo> projects);
    }
}
