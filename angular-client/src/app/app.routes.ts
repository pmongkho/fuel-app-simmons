import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { ReportsNewComponent } from './pages/reports-new/reports-new.component';
import { SupervisorEntriesComponent } from './pages/supervisor-entries/supervisor-entries.component';
import { SupervisorEntryDetailComponent } from './pages/supervisor-entry-detail/supervisor-entry-detail.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { AdminUsersComponent } from './pages/admin-users/admin-users.component';
import { AdminReportDetailComponent } from './pages/admin-report-detail/admin-report-detail.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'reports/new', component: ReportsNewComponent },
  { path: 'supervisor/entries', component: SupervisorEntriesComponent },
  { path: 'supervisor/entries/:entryId', component: SupervisorEntryDetailComponent },
  { path: 'admin/dashboard', component: AdminDashboardComponent },
  { path: 'admin/users', component: AdminUsersComponent },
  { path: 'admin/reports/:reportId', component: AdminReportDetailComponent },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
];
