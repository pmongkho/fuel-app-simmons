using System.Text;
using dotnet_server._Data;
using dotnet_server.Application.Services;
using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReportTotalsService = dotnet_server.Application.Services.ReportTotalsService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter only your JWT token. Swagger will send 'Bearer {token}'."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        async Task<User?> SeedUserAsync(string fullName, string email, UserRole role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                return existing;
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
                return null;
            }

            return newUser;
        }

        var employeeOne = await SeedUserAsync("Employee One", "employee@fuelapp.local", UserRole.Employee);
        var employeeTwo = await SeedUserAsync("Employee Two", "employee2@fuelapp.local", UserRole.Employee);
        var employeeThree = await SeedUserAsync("Employee Three", "employee3@fuelapp.local", UserRole.Employee);
        var supervisor = await SeedUserAsync("Supervisor One", "supervisor@fuelapp.local", UserRole.Supervisor);
        var admin = await SeedUserAsync("Admin One", "admin@fuelapp.local", UserRole.Admin);

        if (!db.Trailers.Any())
        {
            db.Trailers.AddRange(
                new Trailer { TrailerNumber = "8734567", Location = "Main", IsTankFull = false, UpdatedAtUtc = DateTime.UtcNow.AddDays(-4), Notes = "Assigned to produce route." },
                new Trailer { TrailerNumber = "8734568", Location = "Flex", IsTankFull = true, UpdatedAtUtc = DateTime.UtcNow.AddDays(-3), Notes = "DEF refill completed." },
                new Trailer { TrailerNumber = "8734569", Location = "Main", IsTankFull = false, HasMechanicalIssues = true, UpdatedAtUtc = DateTime.UtcNow.AddDays(-2), Notes = "Check valve this week." },
                new Trailer { TrailerNumber = "8734570", Location = "Flex", IsTankFull = true, UpdatedAtUtc = DateTime.UtcNow.AddDays(-1), Notes = "Ready for overnight shift." }
            );
            await db.SaveChangesAsync();
        }

        if (!db.FuelReports.Any() && employeeOne is not null && employeeTwo is not null && employeeThree is not null && supervisor is not null && admin is not null)
        {
            var trailers = await db.Trailers.OrderBy(x => x.TrailerNumber).Take(3).ToListAsync();
            if (trailers.Count == 3)
            {
                var reportOne = new FuelReport
                {
                    ReportDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                    CreatedByUserId = employeeOne.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                    SubmittedAtUtc = DateTime.UtcNow.AddDays(-2).AddHours(4),
                    Status = FuelReportStatus.Submitted,
                    Entries =
                    [
                        new FuelEntry
                        {
                            TrailerId = trailers[0].Id,
                            FuelType = FuelType.RedDiesel,
                            FuelingTankLevelStart = 450,
                            FuelingTankLevelEnd = 380,
                            GallonsPumped = 70,
                            EnteredByUserId = employeeOne.Id,
                            EnteredAtUtc = DateTime.UtcNow.AddDays(-2).AddHours(1),
                            VerificationStatus = VerificationStatus.Approved,
                            VerifiedBySupervisorId = supervisor.Id,
                            VerifiedAtUtc = DateTime.UtcNow.AddDays(-2).AddHours(2),
                            SupervisorSignatureName = "Supervisor One"
                        },
                        new FuelEntry
                        {
                            TrailerId = trailers[1].Id,
                            FuelType = FuelType.Def,
                            FuelingTankLevelStart = 220,
                            FuelingTankLevelEnd = 195,
                            GallonsPumped = 25,
                            EnteredByUserId = employeeOne.Id,
                            EnteredAtUtc = DateTime.UtcNow.AddDays(-2).AddHours(2),
                            VerificationStatus = VerificationStatus.Pending
                        }
                    ]
                };

                var reportTwo = new FuelReport
                {
                    ReportDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                    CreatedByUserId = employeeTwo.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    SubmittedAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(3),
                    Status = FuelReportStatus.Submitted,
                    Entries =
                    [
                        new FuelEntry
                        {
                            TrailerId = trailers[2].Id,
                            FuelType = FuelType.ClearDiesel,
                            FuelingTankLevelStart = 510,
                            FuelingTankLevelEnd = 440,
                            GallonsPumped = 70,
                            EnteredByUserId = employeeTwo.Id,
                            EnteredAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(1),
                            VerificationStatus = VerificationStatus.Rejected,
                            VerifiedBySupervisorId = supervisor.Id,
                            VerifiedAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(2),
                            RejectionReason = "Photo of meter unreadable."
                        },
                        new FuelEntry
                        {
                            TrailerId = trailers[0].Id,
                            FuelType = FuelType.RedDiesel,
                            FuelingTankLevelStart = 390,
                            FuelingTankLevelEnd = 340,
                            GallonsPumped = 50,
                            EnteredByUserId = employeeTwo.Id,
                            EnteredAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(2),
                            VerificationStatus = VerificationStatus.Pending
                        }
                    ]
                };

                var reportThree = new FuelReport
                {
                    ReportDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    CreatedByUserId = employeeThree.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-4),
                    Status = FuelReportStatus.Draft,
                    Entries =
                    [
                        new FuelEntry
                        {
                            TrailerId = trailers[1].Id,
                            FuelType = FuelType.Def,
                            FuelingTankLevelStart = 180,
                            FuelingTankLevelEnd = 150,
                            GallonsPumped = 30,
                            EnteredByUserId = employeeThree.Id,
                            EnteredAtUtc = DateTime.UtcNow.AddHours(-3),
                            VerificationStatus = VerificationStatus.Pending
                        }
                    ]
                };

                ReportTotalsService.Recalculate(reportOne);
                ReportTotalsService.Recalculate(reportTwo);
                ReportTotalsService.Recalculate(reportThree);

                db.FuelReports.AddRange(reportOne, reportTwo, reportThree);
                await db.SaveChangesAsync();
            }
        }

        if (!db.NotificationRecipients.Any())
        {
            db.NotificationRecipients.AddRange(
                new NotificationRecipient { FullName = "Admin One", Email = "admin@fuelapp.local", RecipientType = "Admin" },
                new NotificationRecipient { FullName = "Supervisor One", Email = "supervisor@fuelapp.local", RecipientType = "Supervisor" },
                new NotificationRecipient { FullName = "Dispatch Team", Email = "dispatch@simfoods.com", RecipientType = "Operations" },
                new NotificationRecipient { FullName = "Safety Inbox", Email = "safety@simfoods.com", RecipientType = "Safety" }
            );
            await db.SaveChangesAsync();
        }

        if (!db.EmailLogs.Any())
        {
            var latestReport = await db.FuelReports.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            db.EmailLogs.AddRange(
                new EmailLog
                {
                    FuelReportId = latestReport?.Id,
                    RecipientEmail = "admin@fuelapp.local",
                    Subject = "Fuel report submitted",
                    Status = "Delivered",
                    ProviderMessageId = "msg_10001",
                    SentAtUtc = DateTime.UtcNow.AddHours(-6)
                },
                new EmailLog
                {
                    FuelReportId = latestReport?.Id,
                    RecipientEmail = "fleet@simfoods.com",
                    Subject = "Fuel entry requires review",
                    Status = "Delivered",
                    ProviderMessageId = "msg_10002",
                    SentAtUtc = DateTime.UtcNow.AddHours(-5)
                },
                new EmailLog
                {
                    FuelReportId = latestReport?.Id,
                    RecipientEmail = "ops@simfoods.com",
                    Subject = "Fuel report rejection notice",
                    Status = "Failed",
                    ErrorMessage = "Mailbox unavailable",
                    SentAtUtc = DateTime.UtcNow.AddHours(-4)
                }
            );
            await db.SaveChangesAsync();
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
