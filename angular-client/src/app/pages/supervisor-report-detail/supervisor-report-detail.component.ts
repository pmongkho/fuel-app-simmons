import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  createdByUserId: number;
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
  createdAtUtc: string;
  submittedAtUtc: string | null;
  entriesCount: number;
  entries: FuelEntryDetail[];
}

@Component({
  selector: 'app-supervisor-report-detail',
  standalone: true,
  imports: [NgIf, NgFor, RouterLink, DatePipe, FormsModule],
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
  startSignatureName = '';
  startSignaturePin = '';
  endSignatureName = '';
  endSignaturePin = '';
  actionMessage: string | null = null;
  isSubmitting = false;

  get canSign(): boolean {
    return this.auth.hasRole('Supervisor');
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

    this.loadReportById(reportId);
  }

  private loadReportById(reportId: number): void {
    this.http
      .get<FuelReportDetail>(`${environment.apiBaseUrl}/supervisor/reports/${reportId}`, { headers: this.auth.authHeaders() })
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

  signOffStartGauge(): void {
    if (!this.canSign) {
      this.actionMessage = 'Admins can view this page but only supervisors can sign.';
      return;
    }

    if (!this.report || this.startSignatureName.trim().length < 3 || this.startSignaturePin.trim().length < 4 || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.actionMessage = null;
    this.http
      .post(
        `${environment.apiBaseUrl}/supervisor/reports/${this.report.id}/signoff-start`,
        { signatureName: this.startSignatureName, signaturePin: this.startSignaturePin },
        { headers: this.auth.authHeaders() },
      )
      .subscribe({
        next: () => {
          this.actionMessage = 'Start gauge sign-off saved.';
          this.loadReportById(this.report!.id);
          this.isSubmitting = false;
        },
        error: () => {
          this.actionMessage = 'Unable to sign off start gauge right now.';
          this.isSubmitting = false;
        },
      });
  }

  signOffEndGauge(): void {
    if (!this.canSign) {
      this.actionMessage = 'Admins can view this page but only supervisors can sign.';
      return;
    }

    if (!this.report || this.endSignatureName.trim().length < 3 || this.endSignaturePin.trim().length < 4 || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.actionMessage = null;
    this.http
      .post(
        `${environment.apiBaseUrl}/supervisor/reports/${this.report.id}/signoff-end`,
        { signatureName: this.endSignatureName, signaturePin: this.endSignaturePin },
        { headers: this.auth.authHeaders() },
      )
      .subscribe({
        next: () => {
          this.actionMessage = 'End gauge sign-off saved.';
          this.loadReportById(this.report!.id);
          this.isSubmitting = false;
        },
        error: (error) => {
          this.actionMessage = typeof error?.error === 'string' ? error.error : 'Unable to sign off end gauge right now.';
          this.isSubmitting = false;
        },
      });
  }
}
