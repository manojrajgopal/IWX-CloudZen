using IWX_CloudZen.Authentication.Interfaces;
using IWX_CloudZen.Authentication.Services;
using IWX_CloudZen.CloudAccounts.Interfaces;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.CloudStorage.Services;
using IWX_CloudZen.CloudServices.Cluster.Services;
using IWX_CloudZen.CloudServices.VPC.Services;
using IWX_CloudZen.Permissions.Services;
using IWX_CloudZen.CloudServices.ECR.Services;
using IWX_CloudZen.CloudServices.ECS.Services;
using IWX_CloudZen.CloudServices.Subnet.Services;
using IWX_CloudZen.CloudServices.SecurityGroups.Services;
using IWX_CloudZen.CloudServices.CloudWatchLogs.Services;
using IWX_CloudZen.CloudServices.EC2.Services;
using IWX_CloudZen.CloudServices.KeyPair.Services;
using IWX_CloudZen.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddDataProtection();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CloudAccountService>();
builder.Services.AddScoped<ICloudSecretProtector, CloudSecretProtector>();
builder.Services.AddScoped<CloudStorageService>();
builder.Services.AddScoped<ClusterService>();
builder.Services.AddScoped<VpcService>();
builder.Services.AddScoped<PermissionsService>();
builder.Services.AddScoped<EcrService>();
builder.Services.AddScoped<EcsService>();
builder.Services.AddScoped<SubnetService>();
builder.Services.AddScoped<SecurityGroupService>();
builder.Services.AddScoped<CloudWatchLogsService>();
builder.Services.AddScoped<Ec2Service>();
builder.Services.AddScoped<KeyPairService>();

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