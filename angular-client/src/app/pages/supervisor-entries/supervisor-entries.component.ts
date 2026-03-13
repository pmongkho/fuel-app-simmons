import { Component } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';

type SupervisorEntry = {
  id: number;
  reportDate: string;
  employee: string;
  trailerNumber: string;
  fuelType: 'Red Diesel' | 'Gasoline' | 'Def';
  gallons: number;
  submittedAt: string;
  status: 'Pending Verification' | 'Flagged for Review' | 'Verified';
  hasSignature: boolean;
};

@Component({
  selector: 'app-supervisor-entries',
  standalone: true,
  imports: [RouterLink, NgClass, DatePipe],
  templateUrl: './supervisor-entries.component.html',
  styleUrl: './supervisor-entries.component.css',
})
export class SupervisorEntriesComponent {
  readonly entries: SupervisorEntry[] = [
    {
      id: 1,
      reportDate: '2026-03-11',
      employee: 'Employee One',
      trailerNumber: '873423',
      fuelType: 'Red Diesel',
      gallons: 32,
      submittedAt: '2026-03-11T09:12:00',
      status: 'Pending Verification',
      hasSignature: false,
    },
    {
      id: 2,
      reportDate: '2026-03-11',
      employee: 'Employee Two',
      trailerNumber: '981200',
      fuelType: 'Def',
      gallons: 10,
      submittedAt: '2026-03-11T10:01:00',
      status: 'Flagged for Review',
      hasSignature: false,
    },
    {
      id: 3,
      reportDate: '2026-03-10',
      employee: 'Employee Three',
      trailerNumber: '655003',
      fuelType: 'Gasoline',
      gallons: 44,
      submittedAt: '2026-03-10T17:31:00',
      status: 'Verified',
      hasSignature: true,
    },
  ];

  get pendingCount(): number {
    return this.entries.filter((entry) => entry.status !== 'Verified').length;
  }
  badgeClass(status: SupervisorEntry['status']): string {
    if (status === 'Verified') {
      return 'bg-emerald-100 text-emerald-700';
    }

    if (status === 'Flagged for Review') {
      return 'bg-rose-100 text-rose-700';
    }

    return 'bg-amber-100 text-amber-700';
  }
}

