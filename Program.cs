// Program.cs
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

// ---------------------------------------------
// Enhanced styling to mimic the look and feel of payroc.com
// with a modern, professional design.
// Note: This is an approximation since we don't have direct
// references to payroc.com's internal styles.
// ---------------------------------------------

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var organization = config["AzureDevOps:Organization"];
var pat = config["AzureDevOps:PersonalAccessToken"];

if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(pat))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[ERROR] Missing Organization or Personal Access Token in appsettings.json");
    Console.ResetColor();
    return;
}

// Customize staleness threshold in days
int daysThreshold = 30;

var client = new HttpClient();
var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}"));
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("[INFO] Azure DevOps client initialized.");
Console.ResetColor();

// 1. Fetch all projects
var projectResponse = await client.GetAsync($"https://dev.azure.com/{organization}/_apis/projects?api-version=7.0");
if (!projectResponse.IsSuccessStatusCode)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ERROR] Failed to fetch projects: {projectResponse.StatusCode}");
    Console.WriteLine(await projectResponse.Content.ReadAsStringAsync());
    Console.ResetColor();
    return;
}

var projectContent = await projectResponse.Content.ReadAsStringAsync();
var projectOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var projectList = JsonSerializer.Deserialize<AzureDevOpsProjectList>(projectContent, projectOptions);

Console.WriteLine($"[SUCCESS] Retrieved {projectList.Count} Azure DevOps Projects.\n");

var projectInfos = new List<ProjectInfo>();

foreach (var project in projectList.Value)
{
    Console.WriteLine($"Fetching pipelines for project: {project.Name}");

    // 2. For each project, fetch all pipelines
    var pipelineResponse = await client.GetAsync(
        $"https://dev.azure.com/{organization}/{project.Name}/_apis/pipelines?api-version=7.0");

    if (!pipelineResponse.IsSuccessStatusCode)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [ERROR] Failed to fetch pipelines for {project.Name}: {pipelineResponse.StatusCode}");
        Console.WriteLine(await pipelineResponse.Content.ReadAsStringAsync());
        Console.ResetColor();
        continue;
    }

    var pipelineContent = await pipelineResponse.Content.ReadAsStringAsync();
    var pipelineList = JsonSerializer.Deserialize<AzureDevOpsPipelineList>(pipelineContent, projectOptions);

    var projectInfo = new ProjectInfo
    {
        ProjectName = project.Name,
        Pipelines = new List<PipelineInfo>(),
        Collapsed = false // for collapsible feature
    };

    foreach (var pipeline in pipelineList.Value)
    {
        // 3. Fetch the latest run (and total run count)
        var runsUrl =
            $"https://dev.azure.com/{organization}/{project.Name}/_apis/pipelines/{pipeline.Id}/runs?api-version=7.0&$top=1&$orderby=createdDate desc";
        var runResponse = await client.GetAsync(runsUrl);

        DateTime? lastRunDate = null;
        string lastRunState = "";
        string lastRunResult = "";
        int totalRuns = 0;

        if (runResponse.IsSuccessStatusCode)
        {
            var runContent = await runResponse.Content.ReadAsStringAsync();
            var runList = JsonSerializer.Deserialize<AzureDevOpsPipelineRunList>(runContent, projectOptions);

            // 'runList.Count' is total runs, even if 'value' only has 1
            totalRuns = runList.Count;

            if (runList != null && runList.Value.Count > 0)
            {
                var latestRun = runList.Value[0];
                lastRunDate = latestRun.CreatedDate;
                lastRunState = latestRun.State;
                lastRunResult = latestRun.Result;
            }
        }

        projectInfo.Pipelines.Add(new PipelineInfo
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Folder = pipeline.Folder,
            Url = pipeline.Url,
            LastRunDate = lastRunDate,
            LastRunState = lastRunState,
            LastRunResult = lastRunResult,
            NumberOfRuns = totalRuns
        });
    }

    projectInfos.Add(projectInfo);
}

// 4. Generate AngularJS-based HTML report with improved styling
string htmlReport = GenerateAngularHtmlReport(projectInfos, daysThreshold);
string reportFilePath = "AzureDevOpsReport.html";
await File.WriteAllTextAsync(reportFilePath, htmlReport);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n[INFO] AngularJS HTML report generated: {reportFilePath}");
Console.ResetColor();

// Optionally open the HTML file (on Windows)
try
{
    var psi = new ProcessStartInfo
    {
        FileName = reportFilePath,
        UseShellExecute = true
    };
    Process.Start(psi);
}
catch
{
    // If not on Windows or no default app, silently ignore
}

string GenerateAngularHtmlReport(List<ProjectInfo> projects, int thresholdDays)
{
    // We'll embed the data as JSON inside a <script> so Angular can read it.
    var jsonData = JsonSerializer.Serialize(projects);

    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\" ng-app=\"AdoApp\">");
    sb.AppendLine("<head>");
    sb.AppendLine("  <meta charset=\"utf-8\" />");
    sb.AppendLine("  <title>Azure DevOps Pipeline Report</title>");
    sb.AppendLine("  <!-- AngularJS (1.x) from CDN -->");
    sb.AppendLine("  <script src=\"https://ajax.googleapis.com/ajax/libs/angularjs/1.8.2/angular.min.js\"></script>");
    sb.AppendLine("  <style>");

    // Attempting a style reminiscent of payroc.com (approximation)
    // We'll use a navy/purple top bar, modern typography, and clean cards.

    sb.AppendLine("    body {");
    sb.AppendLine("      margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, sans-serif; background-color: #F5F5F7;");
    sb.AppendLine("    }");

    // Top banner
    sb.AppendLine("    .top-banner {");
    sb.AppendLine("      background: linear-gradient(90deg, #240057 0%, #370072 100%);");
    sb.AppendLine("      color: #FFF; padding: 20px; text-align: center;");
    sb.AppendLine("    }");
    sb.AppendLine("    .top-banner h1 {");
    sb.AppendLine("      margin: 0; font-size: 2rem;");
    sb.AppendLine("    }");

    // Container
    sb.AppendLine("    .container {");
    sb.AppendLine("      max-width: 1200px; margin: 20px auto; padding: 20px;");
    sb.AppendLine("    }");

    // Filter box
    sb.AppendLine("    .filters {");
    sb.AppendLine("      background-color: #FFF; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);");
    sb.AppendLine("      padding: 16px; margin-bottom: 20px;");
    sb.AppendLine("      display: flex; flex-wrap: wrap; gap: 16px; align-items: center;");
    sb.AppendLine("    }");
    sb.AppendLine("    .filters label { font-weight: 600; margin-right: 8px; } ");

    // Project card
    sb.AppendLine("    .project {");
    sb.AppendLine("      background-color: #FFF; padding: 10px; border-radius: 8px;");
    sb.AppendLine("      box-shadow: 0 2px 4px rgba(0,0,0,0.1); margin-bottom: 30px;");
    sb.AppendLine("      transition: all 0.3s ease;");
    sb.AppendLine("    }");
    sb.AppendLine("    .project:hover {");
    sb.AppendLine("      box-shadow: 0 4px 8px rgba(0,0,0,0.15);");
    sb.AppendLine("    }");

    // Collapsible heading
    sb.AppendLine("    h2 {");
    sb.AppendLine("      cursor: pointer; margin-bottom: 0; font-size: 1.2rem; color: #240057;");
    sb.AppendLine("      display: flex; align-items: center;");
    sb.AppendLine("    }");
    sb.AppendLine("    h2 span {");
    sb.AppendLine("      margin-right: 8px; font-size: 1.5rem;");
    sb.AppendLine("      transform: translateY(-1px);");
    sb.AppendLine("    }");

    // Table
    sb.AppendLine("    table {");
    sb.AppendLine("      border-collapse: collapse; width: 100%; margin-top: 10px;");
    sb.AppendLine("    }");
    sb.AppendLine("    th, td {");
    sb.AppendLine("      border: 1px solid #dee2e6; padding: 8px; text-align: left;");
    sb.AppendLine("    }");
    sb.AppendLine("    th {");
    sb.AppendLine("      background-color: #ECEBF6; cursor: pointer; font-weight: 600;");
    sb.AppendLine("      transition: background-color 0.3s ease;");
    sb.AppendLine("    }");
    sb.AppendLine("    th:hover {");
    sb.AppendLine("      background-color: #D4D2E2;");
    sb.AppendLine("    }");
    sb.AppendLine("    .stale {");
    sb.AppendLine("      background-color: #f7d4d4;");
    sb.AppendLine("    }");
    sb.AppendLine("    td a {");
    sb.AppendLine("      color: #370072; text-decoration: none; font-weight: 600;");
    sb.AppendLine("    }");

    sb.AppendLine("    .collapsed table { display: none; }");

    // Sorting indicators
    sb.AppendLine("    .sort-indicator { margin-left: 4px; font-weight: bold; }");

    sb.AppendLine("  </style>");
    sb.AppendLine("</head>");

    sb.AppendLine("<body ng-controller=\"MainCtrl as ctrl\">");
    sb.AppendLine("  <div class=\"top-banner\">");
    sb.AppendLine("    <h1>Azure DevOps Pipeline Report</h1>");
    sb.AppendLine("  </div>");
    sb.AppendLine("  <div class=\"container\">");
    sb.AppendLine($"    <h2 style=\"color:#555;\">Stale if &gt; {thresholdDays} days</h2>");

    // Filter controls
    sb.AppendLine("    <div class=\"filters\">");
    sb.AppendLine("      <label>Search Text:</label>");
    sb.AppendLine("      <input type=\"text\" ng-model=\"searchText\" placeholder=\"Search by name or date...\" />");
    sb.AppendLine("      <label>Min # Runs:</label>");
    sb.AppendLine("      <input type=\"number\" ng-model=\"minRuns\" placeholder=\"0\" style=\"width:80px;\" />");
    sb.AppendLine("      <label><input type=\"checkbox\" ng-model=\"hideArchived\" /> Hide 'Archived'</label>");
    sb.AppendLine("      <label><input type=\"checkbox\" ng-model=\"hideDeprecated\" /> Hide 'Deprecated'</label>");
    sb.AppendLine("    </div>");

    sb.AppendLine("    <div ng-repeat=\"project in projects\" class=\"project\" ng-class=\"{ 'collapsed': project.collapsed }\">");
    // Collapsible project heading
    sb.AppendLine("      <h2 ng-click=\"project.collapsed = !project.collapsed\" >");
    sb.AppendLine("        <span ng-show=\"!project.collapsed\">&#9660;</span>");
    sb.AppendLine("        <span ng-show=\"project.collapsed\">&#9658;</span>");
    sb.AppendLine("        {{ project.ProjectName }}");
    sb.AppendLine("      </h2>");

    sb.AppendLine("      <table>");
    sb.AppendLine("        <tr>");
    // Sortable columns using 'orderByField'
    sb.AppendLine("          <th ng-click=\"setOrder('Id')\">Pipeline ID <span class=\"sort-indicator\" ng-show=\"orderByField=='Id'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('Name')\">Name <span class=\"sort-indicator\" ng-show=\"orderByField=='Name'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('Folder')\">Folder <span class=\"sort-indicator\" ng-show=\"orderByField=='Folder'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine($"          <th ng-click=\"setOrder('LastRunDate')\">Last Run Date <span class=\"sort-indicator\" ng-show=\"orderByField=='LastRunDate'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('LastRunState')\">State <span class=\"sort-indicator\" ng-show=\"orderByField=='LastRunState'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('LastRunResult')\">Result <span class=\"sort-indicator\" ng-show=\"orderByField=='LastRunResult'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('NumberOfRuns')\"># of Runs <span class=\"sort-indicator\" ng-show=\"orderByField=='NumberOfRuns'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th>URL</th>");
    sb.AppendLine("        </tr>");

    sb.AppendLine("        <tr ng-repeat=\"pipe in project.Pipelines | filter:pipelineFilter | orderBy:orderByField:reverseSort\" ng-class=\"{ 'stale': isStale(pipe) }\">");
    sb.AppendLine("          <td>{{ pipe.Id }}</td>");
    sb.AppendLine("          <td>{{ pipe.Name }}</td>");
    sb.AppendLine("          <td>{{ pipe.Folder }}</td>");
    sb.AppendLine("          <td>{{ pipe.LastRunDate ? (pipe.LastRunDate | date:'yyyy-MM-dd HH:mm:ss') : 'No Runs' }}</td>");
    sb.AppendLine("          <td>{{ pipe.LastRunState }}</td>");
    sb.AppendLine("          <td>{{ pipe.LastRunResult }}</td>");
    sb.AppendLine("          <td>{{ pipe.NumberOfRuns }}</td>");
    sb.AppendLine("          <td><a ng-href=\"{{ pipe.Url }}\" target=\"_blank\">Link</a></td>");
    sb.AppendLine("        </tr>");
    sb.AppendLine("      </table>");
    sb.AppendLine("    </div>");

    sb.AppendLine("  </div> <!-- end container -->");

    // AngularJS script + data
    sb.AppendLine("<script>");
    sb.AppendLine("  var app = angular.module('AdoApp', []);");
    sb.AppendLine("  app.controller('MainCtrl', ['$scope', function($scope) {");

    // Embedded data
    sb.AppendLine($"    $scope.projects = {jsonData};");
    sb.AppendLine("    $scope.searchText = '';");
    sb.AppendLine("    $scope.minRuns = 0;");
    sb.AppendLine("    $scope.hideArchived = false;");
    sb.AppendLine("    $scope.hideDeprecated = false;");

    // Sorting fields
    sb.AppendLine("    $scope.orderByField = 'Id';");
    sb.AppendLine("    $scope.reverseSort = false;");
    sb.AppendLine("    $scope.setOrder = function(field) {");
    sb.AppendLine("      if ($scope.orderByField === field) {");
    sb.AppendLine("        $scope.reverseSort = !$scope.reverseSort;");
    sb.AppendLine("      } else {");
    sb.AppendLine("        $scope.orderByField = field;");
    sb.AppendLine("        $scope.reverseSort = false;");
    sb.AppendLine("      }");
    sb.AppendLine("    };");

    // Our custom filter function
    sb.AppendLine("    $scope.pipelineFilter = function(pipe) {");
    sb.AppendLine("      // 1. Hide if archived");
    sb.AppendLine("      if ($scope.hideArchived) {");
    sb.AppendLine("        if ((pipe.Name && pipe.Name.toLowerCase().includes('archived')) ||");
    sb.AppendLine("            (pipe.Folder && pipe.Folder.toLowerCase().includes('archived'))) {");
    sb.AppendLine("          return false;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine("      // 2. Hide if deprecated");
    sb.AppendLine("      if ($scope.hideDeprecated) {");
    sb.AppendLine("        if ((pipe.Name && pipe.Name.toLowerCase().includes('deprecated')) ||");
    sb.AppendLine("            (pipe.Folder && pipe.Folder.toLowerCase().includes('deprecated'))) {");
    sb.AppendLine("          return false;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine("      // 3. Min # runs");
    sb.AppendLine("      if (pipe.NumberOfRuns < ($scope.minRuns || 0)) {");
    sb.AppendLine("        return false;");
    sb.AppendLine("      }");
    sb.AppendLine("      // 4. Search text in name, folder, lastRunDate, result, state...");
    sb.AppendLine("      if ($scope.searchText) {");
    sb.AppendLine("        var txt = $scope.searchText.toLowerCase();");
    sb.AppendLine("        var combined = (pipe.Name + ' ' +\n                         (pipe.Folder || '') + ' ' +\n                         (pipe.LastRunDate || '') + ' ' +\n                         (pipe.LastRunState || '') + ' ' +\n                         (pipe.LastRunResult || '')).toLowerCase();");
    sb.AppendLine("        if (!combined.includes(txt)) {");
    sb.AppendLine("          return false;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine("      return true;");
    sb.AppendLine("    };");

    // Stale check function
    sb.AppendLine("    $scope.isStale = function(pipe) {");
    sb.AppendLine($"      var threshold = {thresholdDays}; // days");
    sb.AppendLine("      if (!pipe.LastRunDate) {");
    sb.AppendLine("        return true; // never run => stale");
    sb.AppendLine("      }");
    sb.AppendLine("      var runDate = new Date(pipe.LastRunDate);");
    sb.AppendLine("      var now = new Date();");
    sb.AppendLine("      var diffDays = (now - runDate) / (1000 * 60 * 60 * 24);");
    sb.AppendLine("      return diffDays > threshold;");
    sb.AppendLine("    };");

    sb.AppendLine("  }]);");
    sb.AppendLine("</script>");

    sb.AppendLine("</body>");
    sb.AppendLine("</html>");

    return sb.ToString();
}

// Model classes
public class AzureDevOpsProjectList
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("value")] public List<AzureDevOpsProject> Value { get; set; } = new();
}

public class AzureDevOpsProject
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("lastUpdateTime")] public DateTime LastUpdateTime { get; set; }
    [JsonPropertyName("visibility")] public string Visibility { get; set; }
}

public class AzureDevOpsPipelineList
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("value")] public List<AzureDevOpsPipeline> Value { get; set; } = new();
}

public class AzureDevOpsPipeline
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("folder")] public string Folder { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; }
}

public class AzureDevOpsPipelineRunList
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("value")] public List<AzureDevOpsPipelineRun> Value { get; set; } = new();
}

public class AzureDevOpsPipelineRun
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
    [JsonPropertyName("result")] public string Result { get; set; }
    [JsonPropertyName("createdDate")] public DateTime CreatedDate { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
}

public class ProjectInfo
{
    public string ProjectName { get; set; }
    public List<PipelineInfo> Pipelines { get; set; }
    // For collapsible feature
    public bool Collapsed { get; set; }
}

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
