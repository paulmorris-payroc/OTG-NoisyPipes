using System.Text;
using System.Text.Json;
using NoisyPipes.Generators.Interfaces;
using NoisyPipes.Models;

namespace NoisyPipes.Generators.Implementations
{
    public class HtmlReportGenerator : IReportGenerator
    {
        private readonly string _templatePath;

        public HtmlReportGenerator()
        {
            // Assuming template is in /Views/ReportTemplate.html relative to app base
            _templatePath = Path.Combine(AppContext.BaseDirectory, "Views", "ReportTemplate.html");
        }

        public string Generate(List<ProjectInfo> projects)
        {
            if (!File.Exists(_templatePath))
            {
                throw new FileNotFoundException("HTML report template not found.", _templatePath);
            }

            var template = File.ReadAllText(_templatePath);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var serializedData = JsonSerializer.Serialize(projects, jsonOptions);

            // Inject the data into the template where the placeholder exists
            return template.Replace("__PIPELINE_DATA__", serializedData);
        }
    }
}
