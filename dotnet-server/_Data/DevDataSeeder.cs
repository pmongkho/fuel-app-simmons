using dotnet_server.Application.Services;
using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Data;

public static class DevDataSeeder
{
    private const string SeedMarkerType = "TEST_SEED_MARKER";
    private const string SeedTag = "[TEST-SEED]";

    private static readonly string[] LegacySeedEmails =
    [
        "employee@fuelapp.local",
        "employee2@fuelapp.local",
        "employee3@fuelapp.local",
        "supervisor@fuelapp.local",
        "supervisor2@fuelapp.local",
        "admin@fuelapp.local"
    ];

    public static async Task SeedAsync(IServiceProvider services, ILogger logger, bool isDevelopment)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        var existingMarker = await db.NotificationRecipients
            .AsNoTracking()
            .AnyAsync(x => x.RecipientType == SeedMarkerType);

        var legacySeedExists = await db.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email != null && LegacySeedEmails.Contains(x.Email));

        var testSeedExists = await db.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email != null && x.Email.EndsWith("@seed.local"));

        if (existingMarker || legacySeedExists || testSeedExists)
        {
            logger.LogInformation("Startup seed skipped because existing seed/test data was detected.");
            return;
        }

        async Task<User?> SeedUserAsync(string fullName, string email, UserRole role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                return existing;
            }

            var newUser = new User
            {
                FullName = $"{SeedTag} {fullName}",
                UserName = email,
                Email = email,
                Role = role,
                IsActive = true
            };

            var result = await userManager.CreateAsync(newUser, "User123!");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to seed user {Email}: {Errors}", email, errors);
                return null;
            }

            return newUser;
        }

        var employeeOne = await SeedUserAsync("Employee One", "test.employee1@seed.local", UserRole.Employee);
        var employeeTwo = await SeedUserAsync("Employee Two", "test.employee2@seed.local", UserRole.Employee);
        var employeeThree = await SeedUserAsync("Employee Three", "test.employee3@seed.local", UserRole.Employee);
        var supervisorOne = await SeedUserAsync("Supervisor One", "test.supervisor1@seed.local", UserRole.Supervisor);
        var supervisorTwo = await SeedUserAsync("Supervisor Two", "test.supervisor2@seed.local", UserRole.Supervisor);
        await SeedUserAsync("Admin One", "test.admin1@seed.local", UserRole.Admin);

        if (!db.Trailers.Any())
        {
            db.Trailers.AddRange(
                new Trailer { TrailerNumber = "TEST-8734567", Location = "Main", IsTankFull = false, UpdatedAtUtc = DateTime.UtcNow.AddDays(-4), Notes = $"{SeedTag} Assigned to produce route." },
                new Trailer { TrailerNumber = "TEST-8734568", Location = "Flex", IsTankFull = true, UpdatedAtUtc = DateTime.UtcNow.AddDays(-3), Notes = $"{SeedTag} DEF refill completed." },
                new Trailer { TrailerNumber = "TEST-8734569", Location = "Main", IsTankFull = false, HasMechanicalIssues = true, UpdatedAtUtc = DateTime.UtcNow.AddDays(-2), Notes = $"{SeedTag} Check valve this week." },
                new Trailer { TrailerNumber = "TEST-8734570", Location = "Flex", IsTankFull = true, UpdatedAtUtc = DateTime.UtcNow.AddDays(-1), Notes = $"{SeedTag} Ready for overnight shift." }
            );
            await db.SaveChangesAsync();
        }

        if (!db.FuelReports.Any() && employeeOne is not null && employeeTwo is not null && employeeThree is not null && supervisorOne is not null && supervisorTwo is not null)
        {
            var now = DateTime.UtcNow;
            var trailers = await db.Trailers.OrderBy(x => x.TrailerNumber).Take(3).ToListAsync();

            if (trailers.Count == 3)
            {
                var reportOne = new FuelReport
                {
                    ReportDate = DateOnly.FromDateTime(now.AddDays(-2)),
                    CreatedByUserId = employeeOne.Id,
                    CreatedAtUtc = now.AddDays(-2),
                    SubmittedAtUtc = now.AddDays(-2).AddHours(4),
                    Status = FuelReportStatus.Completed,
                    FuelingTankLevelStart = 450,
                    FuelingTankLevelEnd = 355,
                    StartGaugeSignedBySupervisorId = supervisorOne.Id,
                    StartGaugeSignedAtUtc = now.AddDays(-2).AddMinutes(10),
                    StartGaugeSupervisorSignatureName = $"{SeedTag} {supervisorOne.FullName}",
                    EndGaugeSignedBySupervisorId = supervisorTwo.Id,
                    EndGaugeSignedAtUtc = now.AddDays(-2).AddHours(4).AddMinutes(5),
                    EndGaugeSupervisorSignatureName = $"{SeedTag} {supervisorTwo.FullName}",
                    Entries =
                    [
                        new FuelEntry
                        {
                            TrailerId = trailers[0].Id,
                            FuelType = FuelType.RedDiesel,
                            GallonsPumped = 70,
                            EnteredByUserId = employeeOne.Id,
                            EnteredAtUtc = now.AddDays(-2).AddHours(1),
                            VerificationStatus = VerificationStatus.Approved,
                            VerifiedBySupervisorId = supervisorOne.Id,
                            VerifiedAtUtc = now.AddDays(-2).AddHours(2),
                            SupervisorSignatureName = $"{SeedTag} {supervisorOne.FullName}",
                            Photos =
                            [
                                new FuelEntryPhoto
                                {
                                    PhotoType = FuelPhotoType.StartGauge,
                                    FileName = "TEST-start-gauge-report-1.jpg",
                                    FilePath = "uploads/test-seed/start-gauge-report-1.jpg",
                                    ContentType = "image/jpeg",
                                    UploadedAtUtc = now.AddDays(-2).AddHours(1)
                                },
                                new FuelEntryPhoto
                                {
                                    PhotoType = FuelPhotoType.EndGauge,
                                    FileName = "TEST-end-gauge-report-1.jpg",
                                    FilePath = "uploads/test-seed/end-gauge-report-1.jpg",
                                    ContentType = "image/jpeg",
                                    UploadedAtUtc = now.AddDays(-2).AddHours(2)
                                }
                            ]
                        },
                        new FuelEntry
                        {
                            TrailerId = trailers[1].Id,
                            FuelType = FuelType.Def,
                            GallonsPumped = 25,
                            EnteredByUserId = employeeOne.Id,
                            EnteredAtUtc = now.AddDays(-2).AddHours(2),
                            VerificationStatus = VerificationStatus.Approved,
                            VerifiedBySupervisorId = supervisorOne.Id,
                            VerifiedAtUtc = now.AddDays(-2).AddHours(3),
                            SupervisorSignatureName = $"{SeedTag} {supervisorOne.FullName}"
                        }
                    ]
                };

                var reportTwo = new FuelReport
                {
                    ReportDate = DateOnly.FromDateTime(now.AddDays(-1)),
                    CreatedByUserId = employeeTwo.Id,
                    CreatedAtUtc = now.AddDays(-1),
                    SubmittedAtUtc = now.AddDays(-1).AddHours(3),
                    Status = FuelReportStatus.Submitted,
                    FuelingTankLevelStart = 510,
                    FuelingTankLevelEnd = 430,
                    StartGaugeSignedBySupervisorId = supervisorOne.Id,
                    StartGaugeSignedAtUtc = now.AddDays(-1).AddMinutes(15),
                    StartGaugeSupervisorSignatureName = $"{SeedTag} {supervisorOne.FullName}",
                    Entries =
                    [
                        new FuelEntry
                        {
                            TrailerId = trailers[2].Id,
                            FuelType = FuelType.ClearDiesel,
                            GallonsPumped = 70,
                            EnteredByUserId = employeeTwo.Id,
                            EnteredAtUtc = now.AddDays(-1).AddHours(1),
                            VerificationStatus = VerificationStatus.Rejected,
                            VerifiedBySupervisorId = supervisorOne.Id,
                            VerifiedAtUtc = now.AddDays(-1).AddHours(2),
                            RejectionReason = $"{SeedTag} Photo of meter unreadable."
                        },
                        new FuelEntry
                        {
                            TrailerId = trailers[0].Id,
                            FuelType = FuelType.RedDiesel,
                            GallonsPumped = 50,
                            EnteredByUserId = employeeTwo.Id,
                            EnteredAtUtc = now.AddDays(-1).AddHours(2),
                            VerificationStatus = VerificationStatus.Pending
                        }
                    ]
                };

                var reportThree = new FuelReport
                {
                    ReportDate = DateOnly.FromDateTime(now),
                    CreatedByUserId = employeeThree.Id,
                    CreatedAtUtc = now.AddHours(-4),
                    Status = FuelReportStatus.Draft,
                    FuelingTankLevelStart = 220,
                    FuelingTankLevelEnd = 220,
                    Entries =
                    [
                        new FuelEntry
                        {
                            TrailerId = trailers[1].Id,
                            FuelType = FuelType.Def,
                            GallonsPumped = 30,
                            EnteredByUserId = employeeThree.Id,
                            EnteredAtUtc = now.AddHours(-3),
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
                new NotificationRecipient { FullName = $"{SeedTag} Admin One", Email = "test.admin1@seed.local", RecipientType = "Admin" },
                new NotificationRecipient { FullName = $"{SeedTag} Supervisor One", Email = "test.supervisor1@seed.local", RecipientType = "Supervisor" },
                new NotificationRecipient { FullName = $"{SeedTag} Dispatch Team", Email = "test.dispatch@seed.local", RecipientType = "Operations" },
                new NotificationRecipient { FullName = $"{SeedTag} Safety Inbox", Email = "test.safety@seed.local", RecipientType = "Safety" },
                new NotificationRecipient { FullName = SeedTag, Email = "test.seed-marker@seed.local", RecipientType = SeedMarkerType }
            );
            await db.SaveChangesAsync();
        }

        if (!db.EmailLogs.Any())
        {
            var latestReport = await db.FuelReports.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            var latestEntry = latestReport?.Entries.FirstOrDefault();

            db.EmailLogs.AddRange(
                new EmailLog
                {
                    FuelReportId = latestReport?.Id,
                    FuelEntryId = latestEntry?.Id,
                    RecipientEmail = "test.admin1@seed.local",
                    Subject = "[TEST] Fuel report submitted",
                    Status = "Delivered",
                    ProviderMessageId = "test_msg_10001",
                    SentAtUtc = DateTime.UtcNow.AddHours(-6)
                },
                new EmailLog
                {
                    FuelReportId = latestReport?.Id,
                    RecipientEmail = "test.fleet@seed.local",
                    Subject = "[TEST] Fuel entry requires review",
                    Status = "Delivered",
                    ProviderMessageId = "test_msg_10002",
                    SentAtUtc = DateTime.UtcNow.AddHours(-5)
                },
                new EmailLog
                {
                    FuelReportId = latestReport?.Id,
                    RecipientEmail = "test.ops@seed.local",
                    Subject = "[TEST] Fuel report rejection notice",
                    Status = "Failed",
                    ErrorMessage = "[TEST] Mailbox unavailable",
                    SentAtUtc = DateTime.UtcNow.AddHours(-4)
                }
            );
            await db.SaveChangesAsync();
        }

        if (!isDevelopment)
        {
            logger.LogInformation("Demo data seeding ran outside development because EnableStartupSeeding was explicitly set.");
        }

        logger.LogInformation("Startup test seed completed. Seeded records are tagged with {SeedTag} and @seed.local for easy cleanup.", SeedTag);
    }
}
