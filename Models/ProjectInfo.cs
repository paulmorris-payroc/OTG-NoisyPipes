using NoisyPipes.Models;
using System.Collections.Generic;

namespace NoisyPipes.Models
{
    public class ProjectInfo
    {
        public string ProjectName { get; set; }
        public List<PipelineInfo> Pipelines { get; set; } = new List<PipelineInfo>();
        public bool Collapsed { get; set; } = false;
    }
}
