import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { environment } from '../../../environments/environment';

type VerificationDecision = 'approved' | 'rejected' | null;

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
}

interface FuelReportSummary {
  id: number;
  reportDate: string;
  createdBy: string;
  status: string;
  entriesCount: number;
}

interface SupervisorEntryDetail {
  id: number;
  fuelType: string;
  gallonsPumped: number;
  fuelingTankLevelStart: number | null;
  fuelingTankLevelEnd: number | null;
  notes: string | null;
  verificationStatus: string;
  enteredAtUtc: string;
  enteredBy: string;
  trailerNumber: string;
  report: FuelReportSummary;
  reportEntries: FuelEntryDetail[];
}

@Component({
  selector: 'app-supervisor-entry-detail',
  standalone: true,
  imports: [FormsModule, NgIf, NgFor, NgClass, RouterLink, DatePipe],
  templateUrl: './supervisor-entry-detail.component.html',
  styleUrl: './supervisor-entry-detail.component.css',
})
export class SupervisorEntryDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  entry: SupervisorEntryDetail | null = null;
  isLoading = true;
  errorMessage: string | null = null;

  meterVerified = false;
  photoVerified = false;
  fuelAmountVerified = false;

  signatureName = '';
  signaturePin = '';
  attestationChecked = false;
  rejectionReason = '';

  decision: VerificationDecision = null;
  actionMessage: string | null = null;
  isSubmitting = false;

  readonly firstAndLastGauge = computed(() => {
    if (!this.entry) {
      return { start: null as number | null, end: null as number | null };
    }

    const sortedEntries = [...this.entry.reportEntries].sort((a, b) => new Date(a.enteredAtUtc).getTime() - new Date(b.enteredAtUtc).getTime());
    const firstEntry = sortedEntries[0];
    const lastEntry = sortedEntries[sortedEntries.length - 1];

    return {
      start: firstEntry?.fuelingTankLevelStart ?? this.entry.fuelingTankLevelStart,
      end: lastEntry?.fuelingTankLevelEnd ?? this.entry.fuelingTankLevelEnd,
    };
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => this.loadEntry(params));
  }

  get canApprove(): boolean {
    return (
      this.meterVerified &&
      this.photoVerified &&
      this.fuelAmountVerified &&
      this.signatureName.trim().length >= 3 &&
      this.signaturePin.trim().length >= 4 &&
      this.attestationChecked &&
      !this.isSubmitting
    );
  }

  get canReject(): boolean {
    return this.rejectionReason.trim().length >= 8 && this.signatureName.trim().length >= 3 && this.signaturePin.trim().length >= 4 && !this.isSubmitting;
  }

  get isMultiEntryReport(): boolean {
    return (this.entry?.reportEntries.length ?? 0) > 1;
  }

  approve(): void {
    if (!this.entry || !this.canApprove) {
      return;
    }

    this.isSubmitting = true;
    this.actionMessage = null;

    this.http
      .post(
        `${environment.apiBaseUrl}/supervisor/entries/${this.entry.id}/approve`,
        { signatureName: this.signatureName, signaturePin: this.signaturePin },
        { headers: this.auth.authHeaders() },
      )
      .subscribe({
        next: () => {
          this.decision = 'approved';
          this.entry = { ...this.entry!, verificationStatus: 'Approved' };
          this.rejectionReason = '';
          this.actionMessage = 'Entry approved and signed.';
          this.isSubmitting = false;
        },
        error: () => {
          this.actionMessage = 'Unable to approve this entry right now.';
          this.isSubmitting = false;
        },
      });
  }

  reject(): void {
    if (!this.entry || !this.canReject) {
      return;
    }

    this.isSubmitting = true;
    this.actionMessage = null;

    this.http
      .post(
        `${environment.apiBaseUrl}/supervisor/entries/${this.entry.id}/reject`,
        { rejectionReason: this.rejectionReason, signatureName: this.signatureName, signaturePin: this.signaturePin },
        { headers: this.auth.authHeaders() },
      )
      .subscribe({
        next: () => {
          this.decision = 'rejected';
          this.entry = { ...this.entry!, verificationStatus: 'Rejected' };
          this.actionMessage = 'Entry rejected and reason logged.';
          this.isSubmitting = false;
        },
        error: () => {
          this.actionMessage = 'Unable to reject this entry right now.';
          this.isSubmitting = false;
        },
      });
  }

  private loadEntry(params: ParamMap): void {
    const entryIdParam = params.get('entryId');
    const entryId = Number(entryIdParam);

    if (!entryIdParam || !Number.isInteger(entryId) || entryId <= 0) {
      this.errorMessage = 'Invalid entry id.';
      this.isLoading = false;
      this.entry = null;
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.decision = null;
    this.actionMessage = null;

    this.http
      .get<SupervisorEntryDetail>(`${environment.apiBaseUrl}/supervisor/entries/${entryId}`, { headers: this.auth.authHeaders() })
      .subscribe({
        next: (entry) => {
          this.entry = entry;
          this.isLoading = false;
        },
        error: (error) => {
          this.entry = null;
          this.errorMessage = error.status === 404 ? 'Entry not found.' : 'Unable to load entry details.';
          this.isLoading = false;
        },
      });
  }
}
