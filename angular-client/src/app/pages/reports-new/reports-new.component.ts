import { Component, computed, inject, OnInit, signal } from '@angular/core';
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
export class ReportsNewComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly reportDraftStorageKey = 'fuel_report_draft';

  reportDate = new Date().toISOString().slice(0, 10);
  readonly submitInProgress = signal(false);
  readonly submitMessage = signal<string | null>(null);

  entry: Entry = this.getDefaultEntry();

  entries = signal<Entry[]>([]);
  redTotal = computed(() => this.entries().filter((x) => x.fuelType === 'RedDiesel').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  clearTotal = computed(() => this.entries().filter((x) => x.fuelType === 'ClearDiesel').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  defTotal = computed(() => this.entries().filter((x) => x.fuelType === 'Def').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  overallTotal = computed(() => this.redTotal() + this.clearTotal() + this.defTotal());

  ngOnInit(): void {
    this.restoreDraft();
  }

  private fuelingLevelsMatchGallons(entry: Entry): boolean {
    if (entry.fuelingTankLevelStart === null || entry.fuelingTankLevelEnd === null || entry.gallonsPumped === null) {
      return false;
    }

    return entry.fuelingTankLevelEnd - entry.fuelingTankLevelStart === entry.gallonsPumped;
  }

  saveEntry() {
    this.submitMessage.set(null);

    if (!this.entry.trailerNumber.trim()) {
      window.alert('Please enter a trailer number before adding an entry.');
      return;
    }

    if (!this.fuelingLevelsMatchGallons(this.entry)) {
      window.alert('Fueling tank start and finish must match gallons pumped (finish - start = gallons pumped).');
      return;
    }

    this.entries.set([...this.entries(), { ...this.entry, trailerNumber: this.entry.trailerNumber.trim() }]);
    this.entry = this.getDefaultEntry();
    this.persistDraft();
  }

  deleteEntry(index: number) {
    this.entries.update((entries) => entries.filter((_, entryIndex) => entryIndex !== index));
    this.persistDraft();
  }

  onDraftChange() {
    this.persistDraft();
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
          { reportDate: this.reportDate },
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
      this.entry = this.getDefaultEntry();
      this.clearDraft();
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

  private getDefaultEntry(): Entry {
    return {
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

  private persistDraft() {
    const payload = {
      reportDate: this.reportDate,
      entry: this.entry,
      entries: this.entries(),
    };

    localStorage.setItem(this.getStorageKey(), JSON.stringify(payload));
  }

  private restoreDraft() {
    const rawDraft = localStorage.getItem(this.getStorageKey());
    if (!rawDraft) {
      return;
    }

    try {
      const parsedDraft = JSON.parse(rawDraft) as { reportDate?: string; entry?: Entry; entries?: Entry[] };
      if (parsedDraft.reportDate) {
        this.reportDate = parsedDraft.reportDate;
      }

      if (parsedDraft.entry) {
        this.entry = { ...this.getDefaultEntry(), ...parsedDraft.entry };
      }

      if (Array.isArray(parsedDraft.entries)) {
        this.entries.set(parsedDraft.entries.map((item) => ({ ...this.getDefaultEntry(), ...item })));
      }
    } catch {
      this.clearDraft();
    }
  }

  private clearDraft() {
    localStorage.removeItem(this.getStorageKey());
  }

  private getStorageKey(): string {
    const currentUser = this.auth.user();
    return currentUser ? `${this.reportDraftStorageKey}_${currentUser.id}` : this.reportDraftStorageKey;
  }
}
