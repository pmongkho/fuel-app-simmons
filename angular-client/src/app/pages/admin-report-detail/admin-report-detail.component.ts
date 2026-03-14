import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

interface FuelEntryDetail {
  id: number;
  fuelType: string;
  gallonsPumped: number;
  fuelingTankLevelStart: number | null;
  fuelingTankLevelEnd: number | null;
  verificationStatus: string;
  enteredAtUtc: string;
  enteredBy: string;
  trailerNumber: string;
  photoCount: number;
}

interface FuelReportDetail {
  id: number;
  reportDate: string;
  createdBy: string;
  status: string;
  totalRedDiesel: number;
  totalClearDiesel: number;
  totalDef: number;
  overallTotalGallons: number;
  submittedAtUtc: string | null;
  entries: FuelEntryDetail[];
}

type FuelReportDetailResponse = Partial<FuelReportDetail> & {
  entries?: FuelEntryDetail[] | null;
};

@Component({
  selector: 'app-admin-report-detail',
  standalone: true,
  imports: [NgIf, NgFor, RouterLink, DatePipe],
  templateUrl: './admin-report-detail.component.html',
  styleUrl: './admin-report-detail.component.css',
})
export class AdminReportDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  report: FuelReportDetail | null = null;
  isLoading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.route.paramMap.subscribe((params: ParamMap) => {
      this.loadReport(params);
    });
  }

  private loadReport(params: ParamMap): void {
    const reportIdParam = params.get('reportId');
    const reportId = Number(reportIdParam);

    if (!reportIdParam || !Number.isInteger(reportId) || reportId <= 0) {
      this.errorMessage = 'Invalid report id.';
      this.isLoading = false;
      this.report = null;
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    this.http
      .get<FuelReportDetailResponse>(`http://localhost:5152/api/admin/reports/${reportId}`, { headers: this.auth.authHeaders() })
      .subscribe({
        next: (report) => {
          this.report = this.normalizeReport(report);
          this.isLoading = false;
        },
        error: (error) => {
          this.report = null;
          this.errorMessage = error.status === 404 ? 'Report not found.' : 'Unable to load report details.';
          this.isLoading = false;
        },
      });
  }

  private normalizeReport(report: FuelReportDetailResponse): FuelReportDetail {
    return {
      id: report.id ?? 0,
      reportDate: report.reportDate ?? '',
      createdBy: report.createdBy ?? '',
      status: report.status ?? '',
      totalRedDiesel: report.totalRedDiesel ?? 0,
      totalClearDiesel: report.totalClearDiesel ?? 0,
      totalDef: report.totalDef ?? 0,
      overallTotalGallons: report.overallTotalGallons ?? 0,
      submittedAtUtc: report.submittedAtUtc ?? null,
      entries: Array.isArray(report.entries) ? report.entries : [],
    };
  }
}
