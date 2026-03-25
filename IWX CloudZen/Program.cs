using Microsoft.EntityFrameworkCore;
using IWX_CloudZen.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IWX_CloudZen.Services;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudAccounts.Interfaces;
using IWX_CloudZen.CloudStorage.Services;
using IWX_CloudZen.CloudDeployments.Services;
using IWX_CloudZen.CloudServiceCreation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddDataProtection();

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CloudAccountService>();
builder.Services.AddScoped<ICloudSecretProtector, CloudSecretProtector>();
builder.Services.AddScoped<CloudFileService>();
builder.Services.AddScoped<CloudDeploymentService>();
builder.Services.AddScoped<CloudInfrastructureService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();