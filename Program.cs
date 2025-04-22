using NoisyPipes.Configuration;
using NoisyPipes.Generators.Implementations;
using NoisyPipes.Generators.Interfaces;
using NoisyPipes.Services.Implementations;
using NoisyPipes.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Bind appsettings.json config section to strongly-typed class
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AzureDevOps"));

// Register typed HttpClient for Azure DevOps API
builder.Services.AddHttpClient<IAdoApiClient, AdoApiClient>();

// Register domain services
builder.Services.AddScoped<IProjectFetcher, ProjectFetcher>();
builder.Services.AddScoped<IReportGenerator, HtmlReportGenerator>();

// Add controllers
builder.Services.AddControllers();

// Enable OpenAPI/Swagger for debugging
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/api/report/generate", permanent: false);
    return Task.CompletedTask;
});

app.Run();
