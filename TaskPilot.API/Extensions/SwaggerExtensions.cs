using Microsoft.OpenApi;

namespace TaskPilot.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerExtension(this IServiceCollection services)
    {
        services.AddSwaggerGen(x =>
        {
            x.SwaggerDoc("v1", new() { Title = "TaskPilot API", Version = "v1" });
            x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token."
            });
            x.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", null),
                    []
                }
            });
        });
        return services;
    }

    //middle ware itself!
    public static IApplicationBuilder UseSwaggerExtension(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json","TaskPilot API v1"));
        return app;
    }
}
