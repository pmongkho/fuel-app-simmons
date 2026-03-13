import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

type VerificationDecision = 'approved' | 'rejected' | null;

@Component({
  selector: 'app-supervisor-entry-detail',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './supervisor-entry-detail.component.html',
  styleUrl: './supervisor-entry-detail.component.css',
})
export class SupervisorEntryDetailComponent {
  readonly entry = {
    reportDate: '2026-03-11',
    employee: 'Employee One',
    trailer: '873423',
    fuelType: 'Red Diesel',
    startGauge: 15522,
    endGauge: 15554,
    gallons: 32,
    notes: 'No leaks observed during fueling. Pump #4 used at North Yard.',
  };

  meterVerified = false;
  photoVerified = false;
  fuelAmountVerified = false;

  signatureName = '';
  signaturePin = '';
  attestationChecked = false;
  rejectionReason = '';

  decision: VerificationDecision = null;

  get canApprove(): boolean {
    return (
      this.meterVerified &&
      this.photoVerified &&
      this.fuelAmountVerified &&
      this.signatureName.trim().length >= 3 &&
      this.signaturePin.trim().length >= 4 &&
      this.attestationChecked
    );
  }

  get canReject(): boolean {
    return this.rejectionReason.trim().length >= 8 && this.signatureName.trim().length >= 3 && this.signaturePin.trim().length >= 4;
  }

  approve(): void {
    if (!this.canApprove) {
      return;
    }

    this.decision = 'approved';
    this.rejectionReason = '';
  }

  reject(): void {
    if (!this.canReject) {
      return;
    }

    this.decision = 'rejected';
  }
}
