namespace dotnet_server._Models;

public class FuelEntry
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = "";
    public string FuelType { get; set; } = "";
    public decimal StartGauge { get; set; }
    public decimal EndGauge { get; set; }
    public bool SupervisorSigned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
