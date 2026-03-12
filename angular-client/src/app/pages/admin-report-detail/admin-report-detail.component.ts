import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-report-detail',
  standalone: true,
  template: `
    <section class="page">
      <h2>Admin Report Detail</h2>
      <p>Header info</p>
      <p>All entries and approved/rejected/pending status</p>
      <p>Totals</p>
      <p>Email/send status</p>
    </section>
  `,
})
export class AdminReportDetailComponent {}
