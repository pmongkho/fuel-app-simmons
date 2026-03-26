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
  overallFuelingTankLevelStart: number | null = null;
  overallFuelingTankLevelEnd: number | null = null;
  startGaugePhoto: File | null = null;
  endGaugePhoto: File | null = null;
  readonly submitInProgress = signal(false);
  readonly ocrInProgress = signal(false);
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

  saveEntry() {
    this.submitMessage.set(null);

    if (!this.entry.trailerNumber.trim()) {
      window.alert('Please enter a trailer number before adding an entry.');
      return;
    }

    if (this.entry.gallonsPumped === null || this.entry.gallonsPumped < 0) {
      window.alert('Please enter a valid gallons pumped value before adding an entry.');
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

  async onStartGaugePhotoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.startGaugePhoto = input.files && input.files.length > 0 ? input.files[0] : null;
    if (this.startGaugePhoto) {
      await this.extractGaugeReading(this.startGaugePhoto, 'start');
      this.persistDraft();
    }
  }

  async onEndGaugePhotoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.endGaugePhoto = input.files && input.files.length > 0 ? input.files[0] : null;
    if (this.endGaugePhoto) {
      await this.extractGaugeReading(this.endGaugePhoto, 'end');
      this.persistDraft();
    }
  }

  async submitReport(): Promise<void> {
    this.submitMessage.set(null);

    if (this.entries().length === 0) {
      this.submitMessage.set('Add at least one entry before submitting.');
      return;
    }

    if (!this.startGaugePhoto || !this.endGaugePhoto) {
      this.submitMessage.set('Upload both start and end gauge photos before submitting.');
      return;
    }

    if (this.overallFuelingTankLevelStart === null || this.overallFuelingTankLevelEnd === null) {
      this.submitMessage.set('Enter overall fueling tank start and end before submitting.');
      return;
    }

    if (this.overallFuelingTankLevelEnd - this.overallFuelingTankLevelStart !== this.overallTotal()) {
      this.submitMessage.set('Overall fueling tank end minus start must match the report total gallons.');
      return;
    }

    this.submitInProgress.set(true);

    try {
      const createReportResponse = await firstValueFrom(
        this.http.post<{ id: number; status: string }>(
          `${environment.apiBaseUrl}/reports`,
          {
            reportDate: this.reportDate,
            fuelingTankLevelStart: this.overallFuelingTankLevelStart,
            fuelingTankLevelEnd: this.overallFuelingTankLevelEnd,
          },
          { headers: this.auth.authHeaders() }
        )
      );

      const createdEntryIds: number[] = [];
      for (const currentEntry of this.entries()) {
        const entryResponse = await firstValueFrom(
          this.http.post<{ id: number }>(
            `${environment.apiBaseUrl}/reports/${createReportResponse.id}/entries`,
            {
              trailerNumber: currentEntry.trailerNumber,
              isTankFull: currentEntry.trailerTankFull,
              hasMechanicalIssues: currentEntry.hasMechanicalIssues,
              trailerNotes: currentEntry.notes,
              fuelType: currentEntry.fuelType,
              gallonsPumped: currentEntry.gallonsPumped,
            },
            { headers: this.auth.authHeaders() }
          )
        );

        createdEntryIds.push(entryResponse.id);
      }

      if (createdEntryIds.length > 0) {
        await this.uploadGaugePhoto(createdEntryIds[0], 'StartGauge', this.startGaugePhoto);
        await this.uploadGaugePhoto(createdEntryIds[createdEntryIds.length - 1], 'EndGauge', this.endGaugePhoto);
      }

      await firstValueFrom(
        this.http.post(`${environment.apiBaseUrl}/reports/${createReportResponse.id}/submit`, {}, { headers: this.auth.authHeaders() })
      );

      this.entries.set([]);
      this.entry = this.getDefaultEntry();
      this.clearDraft();
      this.startGaugePhoto = null;
      this.endGaugePhoto = null;
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

  private async extractGaugeReading(file: File, position: 'start' | 'end'): Promise<void> {
    this.ocrInProgress.set(true);

    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await firstValueFrom(
        this.http.post<{ reading: number | null; rawText: string }>(
          `${environment.apiBaseUrl}/reports/extract-gauge-reading`,
          formData,
          { headers: this.auth.authHeaders() }
        )
      );

      if (typeof response.reading === 'number') {
        if (position === 'start') {
          this.overallFuelingTankLevelStart = response.reading;
        } else {
          this.overallFuelingTankLevelEnd = response.reading;
        }

        this.submitMessage.set(`Detected ${position} gauge reading: ${response.reading}. You can edit it before submitting.`);
      } else {
        this.submitMessage.set(`Could not confidently detect the ${position} gauge number. Please enter it manually.`);
      }
    } catch {
      this.submitMessage.set(`Unable to read the ${position} gauge image right now. Please enter the number manually.`);
    } finally {
      this.ocrInProgress.set(false);
    }
  }

  private async uploadGaugePhoto(entryId: number, photoType: 'StartGauge' | 'EndGauge', file: File): Promise<void> {
    const formData = new FormData();
    formData.append('photoType', photoType);
    formData.append('file', file);

    await firstValueFrom(
      this.http.post(`${environment.apiBaseUrl}/entries/${entryId}/photos`, formData, { headers: this.auth.authHeaders() })
    );
  }

  private getDefaultEntry(): Entry {
    return {
      trailerNumber: '',
      fuelType: 'RedDiesel',
      trailerTankFull: false,
      gallonsPumped: null,
      hasMechanicalIssues: false,
      notes: '',
      verificationStatus: 'Pending',
    };
  }

  private persistDraft() {
    const payload = {
      reportDate: this.reportDate,
      overallFuelingTankLevelStart: this.overallFuelingTankLevelStart,
      overallFuelingTankLevelEnd: this.overallFuelingTankLevelEnd,
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
      const parsedDraft = JSON.parse(rawDraft) as {
        reportDate?: string;
        overallFuelingTankLevelStart?: number | null;
        overallFuelingTankLevelEnd?: number | null;
        entry?: Entry;
        entries?: Entry[];
      };
      if (parsedDraft.reportDate) {
        this.reportDate = parsedDraft.reportDate;
      }
      if (typeof parsedDraft.overallFuelingTankLevelStart === 'number') {
        this.overallFuelingTankLevelStart = parsedDraft.overallFuelingTankLevelStart;
      }
      if (typeof parsedDraft.overallFuelingTankLevelEnd === 'number') {
        this.overallFuelingTankLevelEnd = parsedDraft.overallFuelingTankLevelEnd;
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
