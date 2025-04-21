using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

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
var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("[INFO] Azure DevOps client initialized.");
Console.ResetColor();

try
{
    var projectResponse = await client.GetAsync($"https://dev.azure.com/{organization}/_apis/projects?api-version=7.0");
    if (!projectResponse.IsSuccessStatusCode)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] Failed to fetch projects: {projectResponse.StatusCode}");
        Console.ResetColor();
        return;
    }

    var projectContent = await projectResponse.Content.ReadAsStringAsync();
    var projectOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var projectList = JsonSerializer.Deserialize<AzureDevOpsProjectList>(projectContent, projectOptions);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"[SUCCESS] Retrieved {projectList.Count} Azure DevOps Projects.");
    Console.ResetColor();

    var projectInfos = new List<ProjectInfo>();

    foreach (var project in projectList.Value)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[INFO] Fetching pipelines for project: {project.Name}");
        Console.ResetColor();

        var pipelineResponse = await client.GetAsync($"https://dev.azure.com/{organization}/{project.Name}/_apis/pipelines?api-version=7.0");
        if (!pipelineResponse.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [ERROR] Failed to fetch pipelines for {project.Name}: {pipelineResponse.StatusCode}");
            Console.ResetColor();
            continue;
        }

        var pipelineContent = await pipelineResponse.Content.ReadAsStringAsync();
        var pipelineList = JsonSerializer.Deserialize<AzureDevOpsPipelineList>(pipelineContent, projectOptions);

        var projectInfo = new ProjectInfo
        {
            ProjectName = project.Name,
            Pipelines = new List<PipelineInfo>(),
            Collapsed = false
        };

        foreach (var pipeline in pipelineList.Value)
        {
            var runsUrl = $"https://dev.azure.com/{organization}/{project.Name}/_apis/pipelines/{pipeline.Id}/runs?api-version=7.0&$top=1&$orderby=createdDate desc";
            var runResponse = await client.GetAsync(runsUrl);

            DateTime? lastRunDate = null;
            string lastRunState = "";
            string lastRunResult = "";
            int totalRuns = 0;

            if (runResponse.IsSuccessStatusCode)
            {
                var runContent = await runResponse.Content.ReadAsStringAsync();
                var runList = JsonSerializer.Deserialize<AzureDevOpsPipelineRunList>(runContent, projectOptions);
                totalRuns = runList.Count;

                if (runList.Value.Count > 0)
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

    string htmlReport = GenerateAngularHtmlReport(projectInfos, daysThreshold);
    string reportFilePath = "AzureDevOpsReport.html";
    await File.WriteAllTextAsync(reportFilePath, htmlReport);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[INFO] AngularJS HTML report generated: {reportFilePath}");
    Console.ResetColor();

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
        Console.WriteLine("[WARN] Could not open the report file automatically.");
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[FATAL] {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();
}
string GenerateAngularHtmlReport(List<ProjectInfo> projects, int thresholdDays)
{
    var jsonData = JsonSerializer.Serialize(projects);
    var sb = new StringBuilder();

    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\" ng-app=\"AdoApp\">");
    sb.AppendLine("<head>");
    sb.AppendLine("  <meta charset=\"utf-8\" />");
    sb.AppendLine("  <title>Azure DevOps Pipeline Report</title>");
    sb.AppendLine("  <script src=\"https://ajax.googleapis.com/ajax/libs/angularjs/1.8.2/angular.min.js\"></script>");
    sb.AppendLine("  <style>");
    sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, sans-serif; background-color: #F5F5F7; margin: 0; }");
    sb.AppendLine("    .top-banner { background: linear-gradient(90deg, #240057 0%, #370072 100%); color: #FFF; padding: 20px; text-align: center; }");
    sb.AppendLine("    .container { max-width: 1200px; margin: 20px auto; padding: 20px; }");
    sb.AppendLine("    .filters { background-color: #FFF; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); padding: 16px; margin-bottom: 20px; display: flex; flex-wrap: wrap; gap: 16px; align-items: center; }");
    sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
    sb.AppendLine("    th, td { padding: 8px; border: 1px solid #ccc; text-align: left; }");
    sb.AppendLine("    th { background-color: #ECEBF6; cursor: pointer; user-select: none; }");
    sb.AppendLine("    .collapsed table { display: none; }");
    sb.AppendLine("    .stale { background-color: #f7d4d4; }");
    sb.AppendLine("    .sort-indicator { margin-left: 5px; font-weight: bold; }");
    sb.AppendLine("  </style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body ng-controller=\"MainCtrl\">");

    sb.AppendLine("  <div class=\"top-banner\"><h1>Azure DevOps Pipeline Report</h1></div>");
    sb.AppendLine("  <div class=\"container\">");

    sb.AppendLine("    <div class=\"filters\">");
    sb.AppendLine("      <label>Search:</label><input type=\"text\" ng-model=\"searchText\" />");
    sb.AppendLine("      <label>Min # Runs:</label><input type=\"number\" ng-model=\"minRuns\" style=\"width:80px;\" />");
    sb.AppendLine("      <label>Date Range:</label>");
    sb.AppendLine("      <select ng-model=\"dateRange\">");
    sb.AppendLine("        <option value=''>All Time</option>");
    sb.AppendLine("        <option value='0'>Today</option>");
    sb.AppendLine("        <option value='7'>Last 7 Days</option>");
    sb.AppendLine("        <option value='14'>Last 14 Days</option>");
    sb.AppendLine("        <option value='30'>Last 30 Days</option>");
    sb.AppendLine("        <option value='60'>Last 60 Days</option>");
    sb.AppendLine("        <option value='90'>Last 90 Days</option>");
    sb.AppendLine("        <option value='365'>Last 365 Days</option>");
    sb.AppendLine("      </select>");
    sb.AppendLine("      <label><input type=\"checkbox\" ng-model=\"hideArchived\" /> Hide 'Archived'</label>");
    sb.AppendLine("      <label><input type=\"checkbox\" ng-model=\"hideDeprecated\" /> Hide 'Deprecated'</label>");
    sb.AppendLine("      <button ng-click=\"clearFilters()\">Clear Filters</button>");
    sb.AppendLine("    </div>");

    sb.AppendLine("    <div ng-repeat=\"project in projects\" class=\"project\" ng-class=\"{ 'collapsed': project.collapsed }\">");
    sb.AppendLine("      <h2 ng-click=\"project.collapsed = !project.collapsed\">");
    sb.AppendLine("        <span ng-show=\"!project.collapsed\">&#9660;</span>");
    sb.AppendLine("        <span ng-show=\"project.collapsed\">&#9658;</span>");
    sb.AppendLine("        {{ project.ProjectName }}");
    sb.AppendLine("      </h2>");
    sb.AppendLine("      <table>");
    sb.AppendLine("        <tr>");
    sb.AppendLine("          <th ng-click=\"setOrder('Id')\">ID<span class=\"sort-indicator\" ng-show=\"orderByField=='Id'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('Name')\">Name<span class=\"sort-indicator\" ng-show=\"orderByField=='Name'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('Folder')\">Folder<span class=\"sort-indicator\" ng-show=\"orderByField=='Folder'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('LastRunDate')\">Last Run<span class=\"sort-indicator\" ng-show=\"orderByField=='LastRunDate'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('LastRunState')\">State<span class=\"sort-indicator\" ng-show=\"orderByField=='LastRunState'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('LastRunResult')\">Result<span class=\"sort-indicator\" ng-show=\"orderByField=='LastRunResult'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th ng-click=\"setOrder('NumberOfRuns')\"># Runs<span class=\"sort-indicator\" ng-show=\"orderByField=='NumberOfRuns'\">{{reverseSort ? '▲' : '▼'}}</span></th>");
    sb.AppendLine("          <th>Link</th>");
    sb.AppendLine("        </tr>");
    sb.AppendLine("        <tr ng-repeat=\"pipe in project.Pipelines | filter:pipelineFilter | orderBy:orderByField:reverseSort\">");
    sb.AppendLine("          <td>{{pipe.Id}}</td>");
    sb.AppendLine("          <td>{{pipe.Name}}</td>");
    sb.AppendLine("          <td>{{pipe.Folder}}</td>");
    sb.AppendLine("          <td>{{pipe.LastRunDate ? (pipe.LastRunDate | date:'yyyy-MM-dd HH:mm') : 'No Runs'}}</td>");
    sb.AppendLine("          <td>{{pipe.LastRunState}}</td>");
    sb.AppendLine("          <td>{{pipe.LastRunResult}}</td>");
    sb.AppendLine("          <td>{{pipe.NumberOfRuns}}</td>");
    sb.AppendLine("          <td><a ng-href=\"{{pipe.Url}}\" target=\"_blank\">Link</a></td>");
    sb.AppendLine("        </tr>");
    sb.AppendLine("      </table>");
    sb.AppendLine("    </div>");
    sb.AppendLine("  </div>");

    sb.AppendLine("  <script>");
    sb.AppendLine("    angular.module('AdoApp', []).controller('MainCtrl', function($scope) {");
    sb.AppendLine($"      $scope.projects = {jsonData};");
    sb.AppendLine("      $scope.searchText = '';");
    sb.AppendLine("      $scope.minRuns = 0;");
    sb.AppendLine("      $scope.dateRange = '';");
    sb.AppendLine("      $scope.hideArchived = false;");
    sb.AppendLine("      $scope.hideDeprecated = false;");
    sb.AppendLine("      $scope.orderByField = 'Id';");
    sb.AppendLine("      $scope.reverseSort = false;");
    sb.AppendLine("      $scope.setOrder = function(field) {");
    sb.AppendLine("        if ($scope.orderByField === field) { $scope.reverseSort = !$scope.reverseSort; }");
    sb.AppendLine("        else { $scope.orderByField = field; $scope.reverseSort = false; }");
    sb.AppendLine("      };");

    sb.AppendLine("      $scope.clearFilters = function() {");
    sb.AppendLine("        $scope.searchText = '';");
    sb.AppendLine("        $scope.minRuns = 0;");
    sb.AppendLine("        $scope.dateRange = '';");
    sb.AppendLine("        $scope.hideArchived = false;");
    sb.AppendLine("        $scope.hideDeprecated = false;");
    sb.AppendLine("      };");

    sb.AppendLine("      $scope.pipelineFilter = function(pipe) {");
    sb.AppendLine("        if ($scope.hideArchived && ((pipe.Name || '').toLowerCase().includes('archived') || (pipe.Folder || '').toLowerCase().includes('archived'))) return false;");
    sb.AppendLine("        if ($scope.hideDeprecated && ((pipe.Name || '').toLowerCase().includes('deprecated') || (pipe.Folder || '').toLowerCase().includes('deprecated'))) return false;");
    sb.AppendLine("        if (pipe.NumberOfRuns < ($scope.minRuns || 0)) return false;");
    sb.AppendLine("        if ($scope.searchText) {");
    sb.AppendLine("          var txt = $scope.searchText.toLowerCase();");
    sb.AppendLine("          var combined = (pipe.Name + ' ' + (pipe.Folder || '') + ' ' + (pipe.LastRunDate || '') + ' ' + (pipe.LastRunState || '') + ' ' + (pipe.LastRunResult || '')).toLowerCase();");
    sb.AppendLine("          if (!combined.includes(txt)) return false;");
    sb.AppendLine("        }");
    sb.AppendLine("        if ($scope.dateRange && pipe.LastRunDate) {");
    sb.AppendLine("          var days = parseInt($scope.dateRange);");
    sb.AppendLine("          var now = new Date();");
    sb.AppendLine("          var then = new Date(); then.setDate(now.getDate() - days);");
    sb.AppendLine("          if (new Date(pipe.LastRunDate) < then) return false;");
    sb.AppendLine("        }");
    sb.AppendLine("        return true;");
    sb.AppendLine("      };");
    sb.AppendLine("    });");
    sb.AppendLine("  </script>");

    sb.AppendLine("</body>");
    sb.AppendLine("</html>");
    return sb.ToString();
}


// Model classes
public class AzureDevOpsProjectList
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("value")] public List<AzureDevOpsProject> Value { get; set; }
}

public class AzureDevOpsProject
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
}

public class AzureDevOpsPipelineList
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("value")] public List<AzureDevOpsPipeline> Value { get; set; }
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
    [JsonPropertyName("value")] public List<AzureDevOpsPipelineRun> Value { get; set; }
}

public class AzureDevOpsPipelineRun
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
    [JsonPropertyName("result")] public string Result { get; set; }
    [JsonPropertyName("createdDate")] public DateTime CreatedDate { get; set; }
}

public class ProjectInfo
{
    public string ProjectName { get; set; }
    public List<PipelineInfo> Pipelines { get; set; }
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
