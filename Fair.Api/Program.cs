using Fair.Application.Trips.CreateTrip;
using Fair.Application.Trips.RequestTrip;
using Fair.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Fair.Application.Trips.AcceptTrip;
using Fair.Application.Trips.ArriveTrip;
using Fair.Application.Trips.StartTrip;
using Fair.Application.Trips.CompleteTrip;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

// Handlers (use cases)
builder.Services.AddScoped<CreateTripHandler>();
builder.Services.AddScoped<RequestTripHandler>();
builder.Services.AddScoped<AcceptTripHandler>();
builder.Services.AddScoped<ArriveTripHandler>();
builder.Services.AddScoped<StartTripHandler>();
builder.Services.AddScoped<CompleteTripHandler>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fair.Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {din JWT-token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"] ?? "fair-api";
var audience = jwt["Audience"] ?? "fair-client";
var key = jwt["Key"] ?? "DEV_ONLY_super_long_secret_key_change_later_1234567890";

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
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Errors -> ProblemDetails
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var problem = new ProblemDetails
        {
            Title = "Unhandled error",
            Detail = app.Environment.IsDevelopment() ? ex?.ToString() : "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseHttpsRedirection();
app.UseCors("dev");

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fair.Api v1"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();


