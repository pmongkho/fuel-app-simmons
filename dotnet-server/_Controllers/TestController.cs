using dotnet_server._Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext dbContext = context;
    

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "Backend is connected",
            time = DateTime.UtcNow
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabaseConnection()
    {
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                connected = false,
                message = "Database is not reachable",
                time = DateTime.UtcNow
            });
        }

        var timestamp = await dbContext.Database
            .SqlQueryRaw<DateTime>("SELECT NOW() AS \"Value\"")
            .SingleAsync();

        return Ok(new
        {
            connected = true,
            message = "Database connection is healthy",
            dbTime = timestamp,
            time = DateTime.UtcNow
        });
    }
}
