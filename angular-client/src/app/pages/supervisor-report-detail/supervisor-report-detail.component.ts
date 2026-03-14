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
  createdByUserId: number;
  createdBy: string;
  status: string;
  totalRedDiesel: number;
  totalClearDiesel: number;
  totalDef: number;
  overallTotalGallons: number;
  createdAtUtc: string;
  submittedAtUtc: string | null;
  entriesCount: number;
  entries: FuelEntryDetail[];
}

@Component({
  selector: 'app-supervisor-report-detail',
  standalone: true,
  imports: [NgIf, NgFor, RouterLink, DatePipe],
  templateUrl: './supervisor-report-detail.component.html',
  styleUrl: './supervisor-report-detail.component.css',
})
export class SupervisorReportDetailComponent implements OnInit {
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
      .get<FuelReportDetail>(`http://localhost:5152/api/supervisor/reports/${reportId}`, { headers: this.auth.authHeaders() })
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
