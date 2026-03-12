import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  template: `
    <section class="page">
      <h2>Admin Dashboard</h2>
      <div class="cards">
        <p>Reports today: 0</p><p>Pending verifications: 0</p><p>Red diesel total: 0</p><p>Clear diesel total: 0</p><p>DEF total: 0</p><p>Overall total: 0</p>
      </div>
      <h3>Recent reports</h3>
      <p>Table placeholder</p>
      <h3>Recent pending entries</h3>
      <p>Table placeholder</p>
    </section>
  `,
})
export class AdminDashboardComponent {}
