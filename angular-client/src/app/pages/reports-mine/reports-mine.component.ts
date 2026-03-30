import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { environment } from '../../../environments/environment';

interface ReportRow {
  id: number;
  reportDate: string;
  status: string;
  overallTotalGallons: number;
  createdAtUtc: string;
  submittedAtUtc?: string | null;
  createdBy?: string;
}

@Component({
  selector: 'app-reports-mine',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './reports-mine.component.html',
})
export class ReportsMineComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  readonly reports = signal<ReportRow[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.http
      .get<ReportRow[]>(`${environment.apiBaseUrl}/reports/mine`, { headers: this.auth.authHeaders() })
      .subscribe({
        next: (rows) => {
          this.reports.set(rows);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  get userName() {
    return this.auth.user()?.fullName ?? 'Employee';
  }
}
