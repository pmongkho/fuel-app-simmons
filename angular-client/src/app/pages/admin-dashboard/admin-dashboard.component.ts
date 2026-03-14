import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
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
  createdBy: string;
  status: string;
  overallTotalGallons: number;
  createdAtUtc: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, NgIf, NgFor, DatePipe],
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
    this.http.get<AdminDashboardTotals>('http://localhost:5152/api/admin/dashboard', { headers: this.auth.authHeaders() }).subscribe({
      next: (totals) => {
        this.totals = totals;
        this.loadReports();
      },
      error: () => {
        this.errorMessage = 'Unable to load dashboard totals.';
        this.isLoading = false;
      },
    });
  }

  private loadReports(): void {
    this.http.get<AdminReportRow[]>('http://localhost:5152/api/admin/reports', { headers: this.auth.authHeaders() }).subscribe({
      next: (reports) => {
        this.reports = reports;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load reports.';
        this.isLoading = false;
      },
    });
  }
}
