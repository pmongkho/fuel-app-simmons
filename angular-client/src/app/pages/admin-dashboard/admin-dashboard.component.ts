import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth.service';

interface AdminDashboardTotals {
  reportsToday: number;
  pendingVerifications: number;
  totalRedDiesel: number;
  totalClearDiesel: number;
  totalDef: number;
  overallTotalGallons: number;
}

interface AdminReportRow {
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
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css',
})
export class AdminDashboardComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  totals: AdminDashboardTotals | null = null;
  reports: AdminReportRow[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.loadDashboard();
  }

  private loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = null;

    forkJoin({
      totals: this.http.get<AdminDashboardTotals>('http://localhost:5152/api/admin/dashboard', {
        headers: this.auth.authHeaders(),
      }),
      reports: this.http.get<AdminReportRow[]>('http://localhost:5152/api/admin/reports', {
        headers: this.auth.authHeaders(),
      }),
    }).subscribe({
      next: ({ totals, reports }) => {
        console.log('totals', totals);
        console.log('reports', reports);

        this.totals = totals;
        this.reports = Array.isArray(reports) ? reports : [];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('dashboard load error', err);
        this.errorMessage = 'Unable to load dashboard data.';
        this.isLoading = false;
      },
    });
  }
}
