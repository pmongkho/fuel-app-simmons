import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Entry {
  trailerNumber: string;
  fuelType: 'RedDiesel' | 'ClearDiesel' | 'Def';
  trailerLocation: 'Main' | 'Flex';
  startGaugeLevel: string;
  endGaugeLevel: string;
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
  reportDate = new Date().toISOString().slice(0, 10);
  reportLocation: 'Main' | 'Flex' = 'Main';

  entry: Entry = {
    trailerNumber: '',
    fuelType: 'RedDiesel',
    trailerLocation: 'Main',
    startGaugeLevel: '1/8',
    endGaugeLevel: '1/8',
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
    if (!this.fuelingLevelsMatchGallons(this.entry)) {
      window.alert('Fueling tank start and finish must match gallons pumped (start - finish = gallons pumped).');
      return;
    }

    this.entries.set([...this.entries(), { ...this.entry }]);
    this.entry = {
      trailerNumber: '',
      fuelType: 'RedDiesel',
      trailerLocation: 'Main',
      startGaugeLevel: '1/8',
      endGaugeLevel: '1/8',
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
}
