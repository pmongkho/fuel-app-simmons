import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Entry {
  trailerNumber: string;
  fuelType: 'RedDiesel' | 'ClearDiesel' | 'Def';
  startGaugeLevel: string;
  endGaugeLevel: string;
  gallonsPumped: number;
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
  expectations = '';
  trailersOnYard = '';
  mechanicalIssues = '';

  entry: Entry = {
    trailerNumber: '',
    fuelType: 'RedDiesel',
    startGaugeLevel: '',
    endGaugeLevel: '',
    gallonsPumped: 0,
    notes: '',
    verificationStatus: 'Pending',
  };

  entries = signal<Entry[]>([]);
  redTotal = computed(() => this.entries().filter((x) => x.fuelType === 'RedDiesel').reduce((a, b) => a + b.gallonsPumped, 0));
  clearTotal = computed(() => this.entries().filter((x) => x.fuelType === 'ClearDiesel').reduce((a, b) => a + b.gallonsPumped, 0));
  defTotal = computed(() => this.entries().filter((x) => x.fuelType === 'Def').reduce((a, b) => a + b.gallonsPumped, 0));
  overallTotal = computed(() => this.redTotal() + this.clearTotal() + this.defTotal());

  saveEntry() {
    this.entries.set([...this.entries(), { ...this.entry }]);
    this.entry = { trailerNumber: '', fuelType: 'RedDiesel', startGaugeLevel: '', endGaugeLevel: '', gallonsPumped: 0, notes: '', verificationStatus: 'Pending' };
  }

  deleteEntry(index: number) {
    this.entries.update((entries) => entries.filter((_, entryIndex) => entryIndex !== index));
  }
}
