using System.Text;
using dotnet_server._Data;
using dotnet_server.Application.Services;
using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-key-dev-key-dev-key-dev-key";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "fuel-app",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "fuel-app-client",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User { FullName = "Employee One", Email = "employee@fuelapp.local", PasswordHash = AuthService.HashPassword("User123!"), Role = UserRole.Employee },
            new User { FullName = "Supervisor One", Email = "supervisor@fuelapp.local", PasswordHash = AuthService.HashPassword("User123!"), Role = UserRole.Supervisor },
            new User { FullName = "Admin One", Email = "admin@fuelapp.local", PasswordHash = AuthService.HashPassword("User123!"), Role = UserRole.Admin }
        );

        db.NotificationRecipients.Add(new NotificationRecipient { Email = "admin@fuelapp.local", RecipientType = "Admin" });
        db.SaveChanges();
    }
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
