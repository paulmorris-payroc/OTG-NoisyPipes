using System;

namespace NoisyPipes.Models
{
    public class PipelineInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Folder { get; set; }
        public string Url { get; set; }
        public DateTime? LastRunDate { get; set; }
        public string LastRunState { get; set; }
        public string LastRunResult { get; set; }
        public int NumberOfRuns { get; set; }
    }
}
