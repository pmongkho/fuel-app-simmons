import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth.service';

interface TrailerRow {
  id: number;
  trailerNumber: string;
  location: 'Main' | 'Flex';
  isActive: boolean;
}

interface Entry {
  trailerId: number | null;
  fuelType: 'RedDiesel' | 'ClearDiesel' | 'Def';
  trailerTankFull: boolean;
  fuelingTankLevelStart: number | null;
  fuelingTankLevelEnd: number | null;
  gallonsPumped: number | null;
  hasMechanicalIssues: boolean;
  notes: string;
  verificationStatus: string;
}

@Component({
  selector: 'app-reports-new',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports-new.component.html',
  styleUrl: './reports-new.component.css',
})
export class ReportsNewComponent {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  reportDate = new Date().toISOString().slice(0, 10);
  readonly trailers = signal<TrailerRow[]>([]);
  readonly loadingTrailers = signal(true);
  readonly submitInProgress = signal(false);
  readonly submitMessage = signal<string | null>(null);

  entry: Entry = {
    trailerId: null,
    fuelType: 'RedDiesel',
    trailerTankFull: false,
    fuelingTankLevelStart: null,
    fuelingTankLevelEnd: null,
    gallonsPumped: null,
    hasMechanicalIssues: false,
    notes: '',
    verificationStatus: 'Pending',
  };

  entries = signal<Entry[]>([]);
  redTotal = computed(() => this.entries().filter((x) => x.fuelType === 'RedDiesel').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  clearTotal = computed(() => this.entries().filter((x) => x.fuelType === 'ClearDiesel').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  defTotal = computed(() => this.entries().filter((x) => x.fuelType === 'Def').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  overallTotal = computed(() => this.redTotal() + this.clearTotal() + this.defTotal());

  constructor() {
    this.loadTrailers();
  }

  getTrailerLabel(trailerId: number | null): string {
    if (trailerId === null) {
      return 'Unknown trailer';
    }

    const trailer = this.trailers().find((x) => x.id === trailerId);
    return trailer ? `${trailer.trailerNumber} (${trailer.location})` : `Trailer #${trailerId}`;
  }

  private async loadTrailers(): Promise<void> {
    this.loadingTrailers.set(true);
    try {
      const rows = await firstValueFrom(
        this.http.get<TrailerRow[]>('http://localhost:5152/api/trailers', { headers: this.auth.authHeaders() })
      );
      this.trailers.set(rows.filter((x) => x.isActive));

      if (this.entry.trailerId === null && this.trailers().length > 0) {
        this.entry = { ...this.entry, trailerId: this.trailers()[0].id };
      }
    } catch {
      this.submitMessage.set('Unable to load trailers. Please refresh and try again.');
    } finally {
      this.loadingTrailers.set(false);
    }
  }

  private fuelingLevelsMatchGallons(entry: Entry): boolean {
    if (entry.fuelingTankLevelStart === null || entry.fuelingTankLevelEnd === null || entry.gallonsPumped === null) {
      return false;
    }

    return entry.fuelingTankLevelStart - entry.fuelingTankLevelEnd === entry.gallonsPumped;
  }

  saveEntry() {
    this.submitMessage.set(null);

    if (this.entry.trailerId === null) {
      window.alert('Please select a trailer before adding an entry.');
      return;
    }

    if (!this.fuelingLevelsMatchGallons(this.entry)) {
      window.alert('Fueling tank start and finish must match gallons pumped (start - finish = gallons pumped).');
      return;
    }

    this.entries.set([...this.entries(), { ...this.entry }]);
    this.entry = {
      trailerId: this.trailers().length > 0 ? this.trailers()[0].id : null,
      fuelType: 'RedDiesel',
      trailerTankFull: false,
      fuelingTankLevelStart: null,
      fuelingTankLevelEnd: null,
      gallonsPumped: null,
      hasMechanicalIssues: false,
      notes: '',
      verificationStatus: 'Pending',
    };
  }

  deleteEntry(index: number) {
    this.entries.update((entries) => entries.filter((_, entryIndex) => entryIndex !== index));
  }

  async submitReport(): Promise<void> {
    this.submitMessage.set(null);

    if (this.entries().length === 0) {
      this.submitMessage.set('Add at least one entry before submitting.');
      return;
    }

    this.submitInProgress.set(true);

    try {
      const createReportResponse = await firstValueFrom(
        this.http.post<{ id: number; status: string }>(
          'http://localhost:5152/api/reports',
          { reportDate: new Date(this.reportDate).toISOString() },
          { headers: this.auth.authHeaders() }
        )
      );

      for (const currentEntry of this.entries()) {
        await firstValueFrom(
          this.http.post(
            `http://localhost:5152/api/reports/${createReportResponse.id}/entries`,
            {
              trailerId: currentEntry.trailerId,
              isTankFull: currentEntry.trailerTankFull,
              hasMechanicalIssues: currentEntry.hasMechanicalIssues,
              trailerNotes: currentEntry.notes,
              fuelType: currentEntry.fuelType,
              fuelingTankLevelStart: currentEntry.fuelingTankLevelStart,
              fuelingTankLevelEnd: currentEntry.fuelingTankLevelEnd,
              gallonsPumped: currentEntry.gallonsPumped,
            },
            { headers: this.auth.authHeaders() }
          )
        );
      }

      await firstValueFrom(
        this.http.post(`http://localhost:5152/api/reports/${createReportResponse.id}/submit`, {}, { headers: this.auth.authHeaders() })
      );

      this.entries.set([]);
      this.submitMessage.set('Report submitted successfully.');
      await this.router.navigate(['/reports/mine']);
    } catch (error: unknown) {
      const message =
        typeof error === 'object' &&
        error !== null &&
        'error' in error &&
        typeof (error as { error?: unknown }).error === 'string'
          ? (error as { error: string }).error
          : 'Submit failed. Please verify your entries and try again.';

      this.submitMessage.set(message);
    } finally {
      this.submitInProgress.set(false);
    }
  }
}
