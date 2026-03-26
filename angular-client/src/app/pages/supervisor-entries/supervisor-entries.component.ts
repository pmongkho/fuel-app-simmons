import { Component, OnInit, inject } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth.service';
import { environment } from '../../../environments/environment';

type PendingSupervisorEntry = {
  id: number;
  reportId: number;
  reportDate: string;
  reportFuelingTankLevelStart: number | null;
  reportFuelingTankLevelEnd: number | null;
  reportCreatedByUserId: number;
  reportStatus: string;
  reportTotalRedDiesel: number;
  reportTotalClearDiesel: number;
  reportTotalDef: number;
  reportOverallTotalGallons: number;
  reportCreatedAtUtc: string;
  reportSubmittedAtUtc: string | null;
  reportStartGaugeSignedBySupervisorId: number | null;
  reportEndGaugeSignedBySupervisorId: number | null;
  reportEntriesCount: number;
  employee: string;
  trailerNumber: string;
  fuelType: string;
  gallonsPumped: number;
  submittedTime: string;
  status: string;
};

@Component({
  selector: 'app-supervisor-entries',
  standalone: true,
  imports: [RouterLink, DatePipe, NgIf],
  templateUrl: './supervisor-entries.component.html',
  styleUrl: './supervisor-entries.component.css',
})
export class SupervisorEntriesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  entries: PendingSupervisorEntry[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.http.get<PendingSupervisorEntry[]>(`${environment.apiBaseUrl}/supervisor/entries/pending`, { headers: this.auth.authHeaders() }).subscribe({
      next: (entries) => {
        this.entries = entries;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load supervisor entries.';
        this.isLoading = false;
      },
    });
  }

  get pendingCount(): number {
    return this.entries.length;
  }

  signOffStatus(entry: PendingSupervisorEntry): string {
    if (entry.reportEndGaugeSignedBySupervisorId) return 'Complete';
    if (entry.reportStartGaugeSignedBySupervisorId) return 'Start signed';
    return 'Needs sign-off';
  }
}
