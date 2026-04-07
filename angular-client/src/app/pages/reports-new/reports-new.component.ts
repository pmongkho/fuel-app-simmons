import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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

interface DraftReportState {
  reportId: number | null;
  isStartGaugeLocked: boolean;
  isStartGaugeSignedOff: boolean;
  startGauge: number | null;
  endGauge: number | null;
  reportDate: string;
  pendingLockRetry: boolean;
  entries: Entry[];
  activeEntry: Entry;
}

type ConfirmationAction = 'lock' | 'discard';

@Component({
  selector: 'app-reports-new',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports-new.component.html',
  styleUrl: './reports-new.component.css',
})
export class ReportsNewComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly reportDraftStorageKey = 'fuel_report_draft';
  private readonly onlineRetryHandler = () => {
    void this.retryPendingLockIfNeeded();
  };

  reportDate = new Date().toISOString().slice(0, 10);
  overallFuelingTankLevelStart: number | null = null;
  overallFuelingTankLevelEnd: number | null = null;
  isStartGaugeLocked = false;
  isStartGaugeSignedOff = false;
  private draftReportId: number | null = null;
  private creatingDraftReport = false;
  pendingLockRetry = false;
  confirmDialogOpen = false;
  confirmDialogTitle = '';
  confirmDialogBody = '';
  private confirmDialogAction: ConfirmationAction | null = null;
  readonly submitInProgress = signal(false);
  readonly lockInProgress = signal(false);
  readonly discardInProgress = signal(false);
  readonly signOffRefreshInProgress = signal(false);
  readonly submitMessage = signal<string | null>(null);

  entry: Entry = this.getDefaultEntry();

  entries = signal<Entry[]>([]);
  redTotal = computed(() => this.entries().filter((x) => x.fuelType === 'RedDiesel').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  clearTotal = computed(() => this.entries().filter((x) => x.fuelType === 'ClearDiesel').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  defTotal = computed(() => this.entries().filter((x) => x.fuelType === 'Def').reduce((a, b) => a + (b.gallonsPumped ?? 0), 0));
  overallTotal = computed(() => this.redTotal() + this.clearTotal() + this.defTotal());

  ngOnInit(): void {
    this.hydrateDraftState();
    this.reconcileHydratedDraftState();
    window.addEventListener('online', this.onlineRetryHandler);
    void this.refreshSupervisorSignOffStatus(false);
  }

  ngOnDestroy(): void {
    window.removeEventListener('online', this.onlineRetryHandler);
  }

  saveEntry() {
    if (this.isFormLockedUntilStartSignOff()) {
      this.submitMessage.set('The fuel form stays locked until the start gauge is locked and supervisor-signed.');
      return;
    }

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
    this.persistDraftState();
  }

  deleteEntry(index: number) {
    if (this.isFormLockedUntilStartSignOff()) {
      this.submitMessage.set('The fuel form stays locked until the start gauge is locked and supervisor-signed.');
      return;
    }

    this.entries.update((entries) => entries.filter((_, entryIndex) => entryIndex !== index));
    this.persistDraftState();
  }

  requestLockStartGauge(): void {
    this.submitMessage.set(null);

    if (this.overallFuelingTankLevelStart === null) {
      this.submitMessage.set('Enter overall fueling tank start before locking.');
      return;
    }

    this.openConfirmationDialog(
      'Confirm Start Gauge',
      `Is the start gauge reading ${this.overallFuelingTankLevelStart} correct? Once locked, it cannot be edited unless the draft is discarded.`,
      'lock'
    );
  }

  requestDiscardDraft(): void {
    this.submitMessage.set(null);
    this.openConfirmationDialog(
      'Discard Draft Report',
      'Discard this draft report and unlock the start gauge? This will remove unsent report data.',
      'discard'
    );
  }

  async confirmDialogProceed(): Promise<void> {
    if (!this.confirmDialogAction) return;
    const action = this.confirmDialogAction;
    this.closeConfirmationDialog();

    if (action === 'lock') {
      await this.lockStartGauge();
      return;
    }

    await this.discardDraftReport();
  }

  closeConfirmationDialog(): void {
    this.confirmDialogOpen = false;
    this.confirmDialogTitle = '';
    this.confirmDialogBody = '';
    this.confirmDialogAction = null;
  }

  async retryDraftLock(): Promise<void> {
    await this.lockStartGauge();
  }

  persistProgress(): void {
    this.persistDraftState();
  }

  private async lockStartGauge(): Promise<void> {
    if (this.overallFuelingTankLevelStart === null) {
      this.submitMessage.set('Enter overall fueling tank start before locking.');
      return;
    }

    this.lockInProgress.set(true);
    this.submitMessage.set('Locking start gauge...');

    try {
      const reportId = await this.ensureDraftReportCreated();
      if (!reportId) {
        this.submitMessage.set('Unable to lock start gauge. Please try again.');
        return;
      }

      this.isStartGaugeLocked = true;
      this.isStartGaugeSignedOff = false;
      this.pendingLockRetry = false;
      this.persistDraftState();
      this.submitMessage.set('Start gauge locked and saved. Checking supervisor sign-off status...');
      await this.refreshSupervisorSignOffStatus();
    } catch (error: unknown) {
      if (this.isNetworkError(error)) {
        this.pendingLockRetry = true;
        this.persistDraftState();
        this.submitMessage.set('Network issue while locking start gauge. Reconnect and click "Retry Lock" to continue.');
        return;
      }

      const message =
        typeof error === 'object' &&
        error !== null &&
        'error' in error &&
        typeof (error as { error?: unknown }).error === 'string'
          ? (error as { error: string }).error
          : 'Unable to lock start gauge. Please try again.';

      this.submitMessage.set(message);
    } finally {
      this.lockInProgress.set(false);
    }
  }

  private async discardDraftReport(): Promise<void> {
    this.discardInProgress.set(true);
    this.submitMessage.set('Discarding draft report...');

    if (!this.draftReportId) {
      this.resetDraftState();
      this.submitMessage.set('Draft cleared. Redirecting to My Reports...');
      await this.router.navigate(['/reports/mine']);
      this.discardInProgress.set(false);
      return;
    }

    try {
      await firstValueFrom(
        this.http.delete(`${environment.apiBaseUrl}/reports/${this.draftReportId}`, {
          headers: this.auth.authHeaders(),
        })
      );

      this.resetDraftState();
      this.submitMessage.set('Draft report discarded. Redirecting to My Reports...');
      await this.router.navigate(['/reports/mine']);
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.resetDraftState();
        this.submitMessage.set('Draft was already missing on the server. Redirecting to My Reports...');
        await this.router.navigate(['/reports/mine']);
        return;
      }

      const message =
        typeof error === 'object' &&
        error !== null &&
        'error' in error &&
        typeof (error as { error?: unknown }).error === 'string'
          ? (error as { error: string }).error
          : 'Unable to discard draft report.';
      this.submitMessage.set(message);
    } finally {
      this.discardInProgress.set(false);
    }
  }

  async submitReport(): Promise<void> {
    this.submitMessage.set(null);

    if (this.isFormLockedUntilStartSignOff()) {
      this.submitMessage.set('The fuel form is locked until a supervisor signs off the start gauge.');
      return;
    }

    if (this.entries().length === 0) {
      this.submitMessage.set('Add at least one entry before submitting.');
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

    if (!this.isStartGaugeLocked) {
      this.submitMessage.set('Lock the start gauge before submitting this report.');
      return;
    }

    this.submitInProgress.set(true);

    try {
      const reportId = await this.ensureDraftReportCreated();
      if (!reportId) {
        this.submitMessage.set('Enter overall fueling tank start before submitting.');
        return;
      }

      await firstValueFrom(
        this.http.put(
          `${environment.apiBaseUrl}/reports/${reportId}`,
          {
            reportDate: this.reportDate,
            fuelingTankLevelStart: this.overallFuelingTankLevelStart,
            fuelingTankLevelEnd: this.overallFuelingTankLevelEnd,
          },
          { headers: this.auth.authHeaders() }
        )
      );

      for (const currentEntry of this.entries()) {
        await firstValueFrom(
          this.http.post<{ id: number }>(
            `${environment.apiBaseUrl}/reports/${reportId}/entries`,
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
      }

      await firstValueFrom(
        this.http.post(`${environment.apiBaseUrl}/reports/${reportId}/submit`, {}, { headers: this.auth.authHeaders() })
      );

      this.entries.set([]);
      this.entry = this.getDefaultEntry();
      this.resetDraftState();
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
      gallonsPumped: null,
      hasMechanicalIssues: false,
      notes: '',
      verificationStatus: 'Pending',
    };
  }

  private async ensureDraftReportCreated(): Promise<number | null> {
    if (this.draftReportId) return this.draftReportId;
    if (this.creatingDraftReport) return null;
    if (this.overallFuelingTankLevelStart === null) return null;

    this.creatingDraftReport = true;
    try {
      const createReportResponse = await firstValueFrom(
        this.http.post<{ id: number; status: string }>(
          `${environment.apiBaseUrl}/reports`,
          {
            reportDate: this.reportDate,
            fuelingTankLevelStart: this.overallFuelingTankLevelStart,
            fuelingTankLevelEnd: this.overallFuelingTankLevelStart,
          },
          { headers: this.auth.authHeaders() }
        )
      );
      this.draftReportId = createReportResponse.id;
      this.persistDraftState();
      return createReportResponse.id;
    } finally {
      this.creatingDraftReport = false;
    }
  }

  private async retryPendingLockIfNeeded(): Promise<void> {
    if (!this.pendingLockRetry) return;
    await this.lockStartGauge();
  }

  private persistDraftState(): void {
    const key = this.draftStorageKeyForUser();
    if (!key) return;

    const state: DraftReportState = {
      reportId: this.draftReportId,
      isStartGaugeLocked: this.isStartGaugeLocked,
      isStartGaugeSignedOff: this.isStartGaugeSignedOff,
      startGauge: this.overallFuelingTankLevelStart,
      endGauge: this.overallFuelingTankLevelEnd,
      reportDate: this.reportDate,
      pendingLockRetry: this.pendingLockRetry,
      entries: this.entries(),
      activeEntry: this.entry,
    };

    localStorage.setItem(key, JSON.stringify(state));
  }

  private hydrateDraftState(): void {
    const key = this.draftStorageKeyForUser();
    if (!key) return;

    const rawState = localStorage.getItem(key);
    if (!rawState) return;

    try {
      const parsed = JSON.parse(rawState) as DraftReportState;
      this.draftReportId = parsed.reportId;
      this.isStartGaugeLocked = parsed.isStartGaugeLocked;
      this.isStartGaugeSignedOff = parsed.isStartGaugeSignedOff ?? false;
      this.overallFuelingTankLevelStart = parsed.startGauge;
      this.overallFuelingTankLevelEnd = parsed.endGauge;
      this.reportDate = parsed.reportDate || this.reportDate;
      this.pendingLockRetry = parsed.pendingLockRetry;
      this.entries.set(Array.isArray(parsed.entries) ? parsed.entries : []);
      this.entry = parsed.activeEntry ? { ...this.getDefaultEntry(), ...parsed.activeEntry } : this.getDefaultEntry();
    } catch {
      localStorage.removeItem(key);
    }
  }

  private reconcileHydratedDraftState(): void {
    if (this.draftReportId !== null) return;

    const hasStaleLockedGauge =
      this.isStartGaugeLocked ||
      this.isStartGaugeSignedOff ||
      this.pendingLockRetry ||
      this.overallFuelingTankLevelStart !== null ||
      this.overallFuelingTankLevelEnd !== null;

    if (!hasStaleLockedGauge) return;
    this.resetDraftState();
  }

  private resetDraftState(): void {
    this.draftReportId = null;
    this.isStartGaugeLocked = false;
    this.isStartGaugeSignedOff = false;
    this.pendingLockRetry = false;
    this.overallFuelingTankLevelStart = null;
    this.overallFuelingTankLevelEnd = null;
    this.entries.set([]);
    this.entry = this.getDefaultEntry();
    this.removePersistedDraftState();
  }

  private removePersistedDraftState(): void {
    const key = this.draftStorageKeyForUser();
    if (key) localStorage.removeItem(key);

    const currentUser = this.auth.user();
    if (currentUser) {
      localStorage.removeItem(`${this.reportDraftStorageKey}_${currentUser.id}`);
    }
  }

  private draftStorageKeyForUser(): string | null {
    const currentUser = this.auth.user();
    if (!currentUser) return null;
    return `${this.reportDraftStorageKey}_${currentUser.id}`;
  }

  private openConfirmationDialog(title: string, body: string, action: ConfirmationAction): void {
    this.confirmDialogTitle = title;
    this.confirmDialogBody = body;
    this.confirmDialogAction = action;
    this.confirmDialogOpen = true;
  }

  private isNetworkError(error: unknown): error is HttpErrorResponse {
    return error instanceof HttpErrorResponse && error.status === 0;
  }

  isFormLockedUntilStartSignOff(): boolean {
    return !this.isStartGaugeSignedOff;
  }

  async refreshSupervisorSignOffStatus(showLoadingState = true): Promise<void> {
    if (!this.draftReportId || !this.isStartGaugeLocked) return;

    if (showLoadingState) {
      this.signOffRefreshInProgress.set(true);
      this.submitMessage.set('Checking supervisor sign-off status...');
    }

    try {
      const report = await firstValueFrom(
        this.http.get<{ startGaugeSignedBySupervisorId: number | null }>(
          `${environment.apiBaseUrl}/reports/${this.draftReportId}`,
          { headers: this.auth.authHeaders() }
        )
      );

      this.isStartGaugeSignedOff = report.startGaugeSignedBySupervisorId !== null;
      this.persistDraftState();

      if (this.isStartGaugeSignedOff) {
        this.submitMessage.set('Start gauge has been signed off. You can now complete the fuel report.');
      } else if (showLoadingState) {
        this.submitMessage.set('No supervisor sign-off found yet. Try again in a moment.');
      }
    } catch {
      if (showLoadingState) {
        this.submitMessage.set('Unable to refresh supervisor sign-off status right now.');
      }
    } finally {
      if (showLoadingState) {
        this.signOffRefreshInProgress.set(false);
      }
    }
  }

}
