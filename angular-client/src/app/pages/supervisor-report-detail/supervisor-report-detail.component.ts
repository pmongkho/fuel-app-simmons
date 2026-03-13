import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

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
  createdBy: string;
  status: string;
  totalRedDiesel: number;
  totalClearDiesel: number;
  totalDef: number;
  overallTotalGallons: number;
  submittedAtUtc: string | null;
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

  report: FuelReportDetail | null = null;

  ngOnInit(): void {
    const reportId = Number(this.route.snapshot.paramMap.get('reportId'));
    if (!Number.isFinite(reportId)) {
      return;
    }

    this.http.get<FuelReportDetail>(`/api/supervisor/reports/${reportId}`).subscribe((report) => {
      this.report = report;
    });
  }
}
