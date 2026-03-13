using System.Text;
using dotnet_server._Data;
using dotnet_server.Application.Services;
using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

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

    if (db.Database.CanConnect())
    {
        db.Database.Migrate();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        async Task SeedUserAsync(string fullName, string email, UserRole role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                return;
            }

            var newUser = new User
            {
                FullName = fullName,
                UserName = email,
                Email = email,
                Role = role,
                IsActive = true
            };

            var result = await userManager.CreateAsync(newUser, "User123!");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                app.Logger.LogWarning("Failed to seed user {Email}: {Errors}", email, errors);
            }
        }

        await SeedUserAsync("Employee One", "employee@fuelapp.local", UserRole.Employee);
        await SeedUserAsync("Supervisor One", "supervisor@fuelapp.local", UserRole.Supervisor);
        await SeedUserAsync("Admin One", "admin@fuelapp.local", UserRole.Admin);

        if (!db.NotificationRecipients.Any())
        {
            db.NotificationRecipients.Add(new NotificationRecipient { Email = "admin@fuelapp.local", RecipientType = "Admin" });
            db.SaveChanges();
        }
    }
    else
    {
        app.Logger.LogWarning("Database is unreachable at startup. Skipping EnsureCreated and seed data.");
    }
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
