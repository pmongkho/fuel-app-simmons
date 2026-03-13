import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-report-detail',
  standalone: true,
  template: `
    <section class="space-y-4">
      <div class="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:p-6">
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <h2 class="text-2xl font-semibold">Fuel Report Detail <span class="text-slate-500">- 04/23/2026</span></h2>
          <button class="rounded-md border border-slate-300 px-3 py-1.5 text-sm hover:bg-slate-50">Back to dashboard</button>
        </div>
        <p class="mt-3 text-sm text-slate-600">Submitted by: John Driver</p>

        <div class="mt-4 grid gap-2 text-sm sm:grid-cols-3 lg:grid-cols-4">
          <div class="rounded-lg border border-slate-200 p-3">Red Diesel: <strong>120 gal</strong></div>
          <div class="rounded-lg border border-slate-200 p-3">Clear Diesel: <strong>98 gal</strong></div>
          <div class="rounded-lg border border-slate-200 p-3">DEF: <strong>20 gal</strong></div>
          <div class="rounded-lg border border-slate-200 p-3">Overall: <strong>238 gal</strong></div>
        </div>
      </div>

      <div class="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:p-6">
        <h3 class="text-xl font-semibold">Trailer Mechanical Issues</h3>
        <div class="mt-3 overflow-x-auto">
          <table class="min-w-full text-left text-sm">
            <thead class="border-y border-slate-200 bg-slate-50 text-slate-600">
              <tr><th class="px-2 py-2 font-medium">Trailer #</th><th class="px-2 py-2 font-medium">Tank Start</th><th class="px-2 py-2 font-medium">Tank End</th><th class="px-2 py-2 font-medium">Gallons</th><th class="px-2 py-2 font-medium">Fuel Type</th></tr>
            </thead>
            <tbody>
              <tr class="border-b border-slate-100"><td class="px-2 py-2">873325</td><td class="px-2 py-2">1/4</td><td class="px-2 py-2">Full</td><td class="px-2 py-2">30</td><td class="px-2 py-2">Red Diesel</td></tr>
              <tr class="border-b border-slate-100"><td class="px-2 py-2">802201</td><td class="px-2 py-2">1/2</td><td class="px-2 py-2">Full</td><td class="px-2 py-2">48</td><td class="px-2 py-2">Clear Diesel</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </section>
  `,
})
export class AdminReportDetailComponent {}
