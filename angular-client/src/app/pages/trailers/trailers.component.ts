import { DatePipe, NgClass, NgFor } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AuthService } from '../../core/auth.service';

interface TrailerRow {
  id: number;
  trailerNumber: string;
  location: 'Main' | 'Flex';
  isTankFull: boolean;
  hasMechanicalIssues: boolean;
  notes: string | null;
  isActive: boolean;
  updatedAtUtc: string;
}

@Component({
  selector: 'app-trailers',
  standalone: true,
  imports: [DatePipe, NgClass, NgFor],
  templateUrl: './trailers.component.html',
})
export class TrailersComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  readonly trailers = signal<TrailerRow[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.http
      .get<TrailerRow[]>('http://localhost:5152/api/trailers', { headers: this.auth.authHeaders() })
      .subscribe({
        next: (rows) => {
          this.trailers.set(rows);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
