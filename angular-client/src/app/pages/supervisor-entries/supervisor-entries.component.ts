import { Component, OnInit, inject } from '@angular/core';
import { DatePipe, NgClass, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth.service';
import { environment } from '../../../environments/environment';

type SupervisorEntry = {
  id: number;
  reportId: number;
  reportDate: string;
  reportCreatedByUserId: number;
  reportStatus: string;
  reportTotalRedDiesel: number;
  reportTotalClearDiesel: number;
  reportTotalDef: number;
  reportOverallTotalGallons: number;
  reportCreatedAtUtc: string;
  reportSubmittedAtUtc: string | null;
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
  imports: [RouterLink, NgClass, DatePipe, NgIf],
  templateUrl: './supervisor-entries.component.html',
  styleUrl: './supervisor-entries.component.css',
})
export class SupervisorEntriesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  entries: SupervisorEntry[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.http.get<SupervisorEntry[]>(`${environment.apiBaseUrl}/supervisor/entries/pending`, { headers: this.auth.authHeaders() }).subscribe({
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

  badgeClass(status: string): string {
    if (status === 'Approved') {
      return 'bg-emerald-100 text-emerald-700';
    }

    if (status === 'Rejected') {
      return 'bg-rose-100 text-rose-700';
    }

    return 'bg-amber-100 text-amber-700';
  }
}
