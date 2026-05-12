using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System;
using System.Text;

namespace FairBackgammon.Api
{
  public sealed class Startup
  {
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddControllers();

      services.AddEndpointsApiExplorer();
      services.AddSwaggerGen(options =>
      {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "FairBackgammon API", Version = "v1" });

        // Enables the Swagger UI "Authorize" button for pasting a JWT token.
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
          Name = "Authorization",
          Type = SecuritySchemeType.Http,
          Scheme = "bearer",
          BearerFormat = "JWT",
          In = ParameterLocation.Header,
          Description = "Paste your JWT here (without the 'Bearer ' prefix)."
        });

        options.AddSecurityRequirement((document) => new OpenApiSecurityRequirement()
        {
          [new OpenApiSecuritySchemeReference("Bearer", document)] = ["readAccess", "writeAccess"]
        });
      });

      // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
      services.AddOpenApi();

      string? jwtIssuer = _configuration["Jwt:Issuer"];
      string? jwtAudience = _configuration["Jwt:Audience"];
      string? jwtKey = _configuration["Jwt:Key"];

      if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience) || string.IsNullOrWhiteSpace(jwtKey))
      {
        throw new InvalidOperationException("JWT configuration is missing. Please set Jwt:Issuer, Jwt:Audience and Jwt:Key in configuration.");
      }

      services
          .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
          .AddJwtBearer(options =>
          {
            options.TokenValidationParameters = new TokenValidationParameters
            {
              ValidateIssuer = true,
              ValidIssuer = jwtIssuer,
              ValidateAudience = true,
              ValidAudience = jwtAudience,
              ValidateIssuerSigningKey = true,
              IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
              ValidateLifetime = true,
              ClockSkew = TimeSpan.FromMinutes(1)
            };
          });

      services.AddAuthorization();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
          options.SwaggerEndpoint("/swagger/v1/swagger.json", "FairBackgammon API v1");
          options.EnablePersistAuthorization();
        });
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        if (env.IsDevelopment())
        {
          endpoints.MapOpenApi();
        }

        endpoints.MapControllers();
      });
    }
  }
}
