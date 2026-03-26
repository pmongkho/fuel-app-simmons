using System.Data;
using System.Text;
using dotnet_server._Data;
using dotnet_server.Application.Services;
using dotnet_server.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection(ResendOptions.SectionName));
builder.Services.AddHttpClient<EmailService>();
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.SectionName));
builder.Services.Configure<GaugeOcrOptions>(builder.Configuration.GetSection(GaugeOcrOptions.SectionName));
builder.Services.AddScoped<FuelPhotoStorageService>();
builder.Services.AddHttpClient<GaugeOcrService>();
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
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)));

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
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200", "https://fuel-app-simmons.vercel.app", "https://fuel-app-simmons.com", "https://www.fuel-app-simmons.com", "https://*.vercel.app"];

        var exactOrigins = configuredOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin) && !origin.Contains('*'))
            .Select(origin => origin.TrimEnd('/'))
            .ToArray();

        var wildcardOrigins = configuredOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin) && origin.Contains('*'))
            .Select(origin => origin.Replace("https://*.", string.Empty, StringComparison.OrdinalIgnoreCase).TrimEnd('/'))
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .ToArray();

        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                var normalizedOrigin = origin.TrimEnd('/');
                if (exactOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                return wildcardOrigins.Any(suffix =>
                    uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                    && uri.Host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase));
            })
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
        using var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        var hasMigrationsHistoryTable = TableExists(connection, "__EFMigrationsHistory");
        var hasAppliedMigrations = hasMigrationsHistoryTable && HasRows(connection, "__EFMigrationsHistory");
        var hasLegacySchemaObjects = TableExists(connection, "AspNetRoles");

        if (!hasAppliedMigrations && hasLegacySchemaObjects)
        {
            BaselineInitialMigration(connection);
            app.Logger.LogWarning(
                "Database schema existed without EF migration history. Inserted baseline migration record {MigrationId}.",
                InitialMigrationId);
        }

        db.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied.");
    }
    else
    {
        app.Logger.LogWarning("Database is unreachable at startup. Skipping database migrations.");
    }
}

static bool TableExists(NpgsqlConnection connection, string tableName)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
                          SELECT EXISTS (
                              SELECT 1
                              FROM pg_catalog.pg_class c
                              JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                              WHERE n.nspname = 'public' AND c.relname = @tableName
                          );
                          """;
    command.Parameters.AddWithValue("tableName", tableName);
    return command.ExecuteScalar() is true;
}

static bool HasRows(NpgsqlConnection connection, string tableName)
{
    using var command = connection.CreateCommand();
    command.CommandText = $"""SELECT EXISTS (SELECT 1 FROM "{tableName}" LIMIT 1);""";
    return command.ExecuteScalar() is true;
}

const string InitialMigrationId = "20260314133117_InitialCreate";
const string EfProductVersion = "8.0.11";

static void BaselineInitialMigration(NpgsqlConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
                          INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                          VALUES (@migrationId, @productVersion)
                          ON CONFLICT ("MigrationId") DO NOTHING;
                          """;
    command.Parameters.AddWithValue("migrationId", InitialMigrationId);
    command.Parameters.AddWithValue("productVersion", EfProductVersion);
    command.ExecuteNonQuery();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
