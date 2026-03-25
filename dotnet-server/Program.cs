using System.Text;
using dotnet_server._Data;
using dotnet_server.Application.Services;
using dotnet_server.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection(ResendOptions.SectionName));
builder.Services.AddHttpClient<EmailService>();
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
            ?? ["http://localhost:4200", "https://fuel-app-simmons.vercel.app", "https://*.vercel.app"];

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
        db.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied.");
    }
    else
    {
        app.Logger.LogWarning("Database is unreachable at startup. Skipping database migrations.");
    }
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
