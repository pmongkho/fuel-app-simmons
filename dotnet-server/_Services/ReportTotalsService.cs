using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;

namespace dotnet_server.Application.Services;

public static class ReportTotalsService
{
    public static void Recalculate(FuelReport report)
    {
        report.TotalRedDiesel = report.Entries.Where(x => x.FuelType == FuelType.RedDiesel).Sum(x => x.GallonsPumped);
        report.TotalClearDiesel = report.Entries.Where(x => x.FuelType == FuelType.ClearDiesel).Sum(x => x.GallonsPumped);
        report.TotalDef = report.Entries.Where(x => x.FuelType == FuelType.Def).Sum(x => x.GallonsPumped);
        report.OverallTotalGallons = report.TotalRedDiesel + report.TotalClearDiesel + report.TotalDef;

    }
}
