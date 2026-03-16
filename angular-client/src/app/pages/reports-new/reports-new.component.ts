import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { environment } from '../../../environments/environment';

interface Entry {
  trailerNumber: string;
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
  readonly submitInProgress = signal(false);
  readonly submitMessage = signal<string | null>(null);

  entry: Entry = {
    trailerNumber: '',
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

  private fuelingLevelsMatchGallons(entry: Entry): boolean {
    if (entry.fuelingTankLevelStart === null || entry.fuelingTankLevelEnd === null || entry.gallonsPumped === null) {
      return false;
    }

    return entry.fuelingTankLevelStart - entry.fuelingTankLevelEnd === entry.gallonsPumped;
  }

  saveEntry() {
    this.submitMessage.set(null);

    if (!this.entry.trailerNumber.trim()) {
      window.alert('Please enter a trailer number before adding an entry.');
      return;
    }

    if (!this.fuelingLevelsMatchGallons(this.entry)) {
      window.alert('Fueling tank start and finish must match gallons pumped (start - finish = gallons pumped).');
      return;
    }

    this.entries.set([...this.entries(), { ...this.entry, trailerNumber: this.entry.trailerNumber.trim() }]);
    this.entry = {
      trailerNumber: '',
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
          `${environment.apiBaseUrl}/reports`,
          { reportDate: new Date(this.reportDate).toISOString() },
          { headers: this.auth.authHeaders() }
        )
      );

      for (const currentEntry of this.entries()) {
        await firstValueFrom(
          this.http.post(
            `${environment.apiBaseUrl}/reports/${createReportResponse.id}/entries`,
            {
              trailerNumber: currentEntry.trailerNumber,
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
        this.http.post(`${environment.apiBaseUrl}/reports/${createReportResponse.id}/submit`, {}, { headers: this.auth.authHeaders() })
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
