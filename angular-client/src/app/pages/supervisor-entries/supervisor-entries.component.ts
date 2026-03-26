import { Component, OnInit, inject } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth.service';
import { environment } from '../../../environments/environment';

type SupervisorReport = {
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
  endGaugeSignedBySupervisorId: number | null;
  createdAtUtc: string;
  submittedAtUtc: string | null;
  entriesCount: number;
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

  reports: SupervisorReport[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.http.get<SupervisorReport[]>(`${environment.apiBaseUrl}/supervisor/reports/pending`, { headers: this.auth.authHeaders() }).subscribe({
      next: (reports) => {
        this.reports = reports;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load supervisor reports.';
        this.isLoading = false;
      },
    });
  }

  get pendingCount(): number {
    return this.reports.length;
  }

  signOffStatus(report: SupervisorReport): string {
    if (report.endGaugeSignedBySupervisorId) return 'Complete';
    if (report.startGaugeSignedBySupervisorId) return 'Start signed';
    return 'Needs sign-off';
  }
}
