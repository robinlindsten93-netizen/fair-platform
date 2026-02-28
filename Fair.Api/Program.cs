using Fair.Api.AuthZ;
using Fair.Application.Dispatch;
using Fair.Application.Drivers;
using Fair.Application.Me;
using Fair.Application.Trips.AcceptTrip;
using Fair.Application.Trips.ArriveTrip;
using Fair.Application.Trips.CompleteTrip;
using Fair.Application.Trips.CreateTrip;
using Fair.Application.Trips.RequestTrip;
using Fair.Application.Trips.StartTrip;
using Fair.Domain.Auth;
using Fair.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fair.Api.Swagger;
using Fair.Api.Dispatch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<DispatchOptions>(builder.Configuration.GetSection("Dispatch"));
builder.Services.AddHostedService<DispatchOfferExpiryService>();

// =========================
// Handlers / Use cases
// =========================
builder.Services.AddScoped<CreateTripHandler>();
builder.Services.AddScoped<RequestTripHandler>();
builder.Services.AddScoped<AcceptTripHandler>();
builder.Services.AddScoped<ArriveTripHandler>();
builder.Services.AddScoped<StartTripHandler>();
builder.Services.AddScoped<CompleteTripHandler>();

// 🔥 DISPATCH
builder.Services.AddScoped<GetMyOffers>();
builder.Services.AddScoped<AcceptDispatchOffer>();
builder.Services.AddScoped<CreateDispatchOffers>();

// Identity
builder.Services.AddScoped<GetMe>();

// Driver availability
builder.Services.AddScoped<GetDriverMe>();
builder.Services.AddScoped<SetDriverAvailability>();

// =========================
// Swagger
// =========================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fair.Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Skriv: Bearer {din JWT-token}"
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

    // ✅ HÄR kopplar du in ditt OTP-exempel-filter:
    c.OperationFilter<Fair.Api.Swagger.OtpExamplesOperationFilter>();
});

// =========================
// CORS (dev only)
// =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// =========================
// JWT
// =========================
var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"] ?? "fair-api";
var audience = jwt["Audience"] ?? "fair-client";
var key = jwt["Key"] ?? "DEV_ONLY_super_long_secret_key_change_later_1234567890";

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

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
            ClockSkew = TimeSpan.FromMinutes(30),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
    });

// =========================
// Authorization (repo-baserad)
// =========================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Rider", p => p.RequireAuthenticatedUser()
        .AddRequirements(new RequireAppRoleRequirement(Role.Rider)));

    options.AddPolicy("Driver", p => p.RequireAuthenticatedUser()
        .AddRequirements(new RequireAppRoleRequirement(Role.Driver)));

    options.AddPolicy("Owner", p => p.RequireAuthenticatedUser()
        .AddRequirements(new RequireAppRoleRequirement(Role.Owner)));

    options.AddPolicy("FleetOwnerOrAdmin", p => p.RequireAuthenticatedUser()
        .AddRequirements(new RequireFleetRoleRequirement(Role.Owner, Role.FleetAdmin)));
});

builder.Services.AddSingleton<IAuthorizationHandler, RoleAuthorizationHandler>();

var app = builder.Build();

// =========================
// Errors -> ProblemDetails
// =========================
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var problem = new ProblemDetails
        {
            Title = "Unhandled error",
            Detail = app.Environment.IsDevelopment()
                ? ex?.ToString()
                : "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();