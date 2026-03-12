using dotnet_server.Domain.Enums;

namespace dotnet_server.Domain.Entities;

public class FuelEntryPhoto
{
    public int Id { get; set; }
    public int FuelEntryId { get; set; }
    public FuelEntry? FuelEntry { get; set; }
    public FuelPhotoType PhotoType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
