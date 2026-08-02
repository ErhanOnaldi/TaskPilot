using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using TaskPilot.API.ExceptionHandling;
using TaskPilot.API.Extensions;
using TaskPilot.API.Features.Platform.Correlation;
using TaskPilot.API.Features.Platform.Options;
using TaskPilot.API.Features.Platform.RateLimiting;
using TaskPilot.API.Features.Operations;
using TaskPilot.API.Features.Knowledge;
using TaskPilot.API.Features.Interop;
using TaskPilot.Application.Features.Knowledge;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Features.Semantic;
using TaskPilot.Application.Features.Copilot;
using TaskPilot.Application.Features.Interop;
using TaskPilot.Application.Features.Reports;
using TaskPilot.API.Filters;
using TaskPilot.Application.Extensions;
using TaskPilot.Domain.Options;
using TaskPilot.Infrastructure.Extensions;
using TaskPilot.Infrastructure.Features.Knowledge;
using TaskPilot.Infrastructure.Features.Ai;
using TaskPilot.Infrastructure.Features.Semantic;
using TaskPilot.Infrastructure.Features.Copilot;
using TaskPilot.Infrastructure.Features.Interop;
using TaskPilot.Infrastructure.Features.Reports;
using TaskPilot.Persistence.Extensions;
using TaskPilot.Persistence.Features.Knowledge;
using TaskPilot.Persistence.Features.Ai;
using TaskPilot.Persistence.Features.Semantic;
using TaskPilot.Persistence.Features.Copilot;
using TaskPilot.Persistence.Features.Reports;
using Serilog;
using A2A;
using A2A.AspNetCore;
using Microsoft.Agents.AI.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<FluentValidationActionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerExtension();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddKnowledgeApplication();
builder.Services.AddKnowledgePersistence();
builder.Services.AddKnowledgeInfrastructure();
builder.Services.AddKnowledgeApi();
builder.Services.AddAiApplication();
builder.Services.AddAiPersistence();
builder.Services.AddAiInfrastructure(builder.Configuration);
builder.Services.AddSemanticApplication();
builder.Services.AddSemanticPersistence();
builder.Services.AddSemanticInfrastructure(builder.Configuration);
builder.Services.AddCopilotApplication();
builder.Services.AddCopilotPersistence();
builder.Services.AddCopilotInfrastructure();
builder.Services.AddInteropApplication();
builder.Services.AddInteropInfrastructure();
builder.Services.AddInteropApi();
builder.Services.AddReportApplication();
builder.Services.AddReportPersistence();
builder.Services.AddReportInfrastructure(builder.Configuration);
builder.Services.AddConnectionCriticalOptionsValidation();
builder.Services.AddTaskPilotRateLimiting();
builder.Services.AddTaskPilotOperations(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Jwt configuration is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCorrelationProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Persistence:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<TaskPilot.Persistence.AppDbContext>().Database.MigrateAsync();
}
app.UseCorrelationId();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseMiddleware<SlowRequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerExtension();
}

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapTaskPilotHealthEndpoints();
app.MapControllers();
if (builder.Configuration.GetValue<bool>("Features:Interop:McpEnabled"))
{
    app.MapMcp("/mcp")
        .RequireAuthorization(InteropApiExtensions.McpAccessPolicy)
        .RequireRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy);
}
if (builder.Configuration.GetValue<bool>("Features:Interop:A2aEnabled"))
{
    var publicBaseUrl = builder.Configuration["Features:Interop:PublicBaseUrl"]?.TrimEnd('/')
                        ?? "http://localhost:8080";
    app.MapA2AHttpJson("taskpilot-copilot", "/a2a")
        .RequireAuthorization()
        .RequireRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy);
    app.MapA2AJsonRpc("taskpilot-copilot", "/a2a/jsonrpc")
        .RequireAuthorization()
        .RequireRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy);
    app.MapWellKnownAgentCard(new AgentCard
    {
        Name = "TaskPilot Copilot",
        Description = "Authorized TaskPilot project and workspace knowledge copilot.",
        Version = "1.0.0",
        SupportedInterfaces =
        [
            new AgentInterface { Url = $"{publicBaseUrl}/a2a", ProtocolBinding = ProtocolBindingNames.HttpJson, ProtocolVersion = "1.0" },
            new AgentInterface { Url = $"{publicBaseUrl}/a2a/jsonrpc", ProtocolBinding = ProtocolBindingNames.JsonRpc, ProtocolVersion = "1.0" }
        ],
        Capabilities = new AgentCapabilities { Streaming = true },
        DefaultInputModes = ["text/plain", "application/json"],
        DefaultOutputModes = ["text/plain", "application/json"],
        Skills =
        [
            new AgentSkill
            {
                Id = "project-copilot",
                Name = "Project Copilot",
                Description = "Answers from authorized TaskPilot project and knowledge context.",
                Tags = ["taskpilot", "project", "knowledge"],
                InputModes = ["application/json"],
                OutputModes = ["text/plain"]
            }
        ]
    });
}

app.Run();

public partial class Program;
