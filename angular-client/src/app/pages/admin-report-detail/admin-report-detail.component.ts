import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { environment } from '../../../environments/environment';

interface FuelEntryDetail {
  id: number;
  fuelType: string;
  gallonsPumped: number;
  verificationStatus: string;
  enteredAtUtc: string;
  enteredBy: string;
  trailerNumber: string;
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
  fuelingTankLevelStart: number;
  fuelingTankLevelEnd: number;
  startGaugeSignedBySupervisorId: number | null;
  startGaugeSignedAtUtc: string | null;
  startGaugeSupervisorSignatureName: string | null;
  endGaugeSignedBySupervisorId: number | null;
  endGaugeSignedAtUtc: string | null;
  endGaugeSupervisorSignatureName: string | null;
  submittedAtUtc: string | null;
  entries: FuelEntryDetail[];
}

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

  get isEmployeeView(): boolean {
    return this.auth.hasRole('Employee');
  }

  get backLink(): string {
    return this.isEmployeeView ? '/reports/mine' : '/admin/dashboard';
  }

  get backLabel(): string {
    return this.isEmployeeView ? 'Back to my reports' : 'Back to dashboard';
  }

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

    const endpoint = this.isEmployeeView
      ? `${environment.apiBaseUrl}/reports/${reportId}`
      : `${environment.apiBaseUrl}/admin/reports/${reportId}`;

    this.http
      .get<FuelReportDetail>(endpoint, { headers: this.auth.authHeaders() })
      .subscribe({
        next: (report) => {
          this.report = report;
          this.isLoading = false;
        },
        error: (error) => {
          this.report = null;
          this.errorMessage = error.status === 404 ? 'Report not found.' : 'Unable to load report details.';
          this.isLoading = false;
        },
      });
  }
}
