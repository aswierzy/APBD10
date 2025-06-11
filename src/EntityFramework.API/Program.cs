using System.Text;
using System.Text.Json;
using EntityFramework;
using EntityFramework.DAL;
using Microsoft.EntityFrameworkCore;
using EntityFramework.DTO;
using EntityFramework.Helpers.Options;
using EntityFramework.Middleware;
using EntityFramework.Services.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ITokenService, TokenService>();
var jwtConfigData = builder.Configuration.GetSection("Jwt");

var connectionString = builder.Configuration.GetConnectionString("MY_DB")
                        ?? throw new Exception("Connection string not found");

builder.Services.AddDbContext<DeviceContext>(options => options.UseSqlServer(connectionString));
builder.Services.Configure<JwtOptions>(jwtConfigData);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = jwtConfigData["Issuer"],
            ValidAudience = jwtConfigData["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigData["Key"])),
            ClockSkew = TimeSpan.FromSeconds(10)
        };
    });


builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseMiddleware<Middleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
