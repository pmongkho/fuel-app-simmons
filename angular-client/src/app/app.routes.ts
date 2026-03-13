import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { ReportsNewComponent } from './pages/reports-new/reports-new.component';
import { SupervisorEntriesComponent } from './pages/supervisor-entries/supervisor-entries.component';
import { SupervisorEntryDetailComponent } from './pages/supervisor-entry-detail/supervisor-entry-detail.component';
import { SupervisorReportDetailComponent } from './pages/supervisor-report-detail/supervisor-report-detail.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { AdminUsersComponent } from './pages/admin-users/admin-users.component';
import { AdminReportDetailComponent } from './pages/admin-report-detail/admin-report-detail.component';
import { authGuard, roleGuard } from './core/auth.guards';
import { ReportsMineComponent } from './pages/reports-mine/reports-mine.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'reports/new', component: ReportsNewComponent, canActivate: [authGuard, roleGuard(['Employee', 'Supervisor', 'Admin'])] },
  { path: 'reports/mine', component: ReportsMineComponent, canActivate: [authGuard, roleGuard(['Employee', 'Supervisor', 'Admin'])] },
  { path: 'supervisor/entries', component: SupervisorEntriesComponent, canActivate: [authGuard, roleGuard(['Supervisor', 'Admin'])] },
  { path: 'supervisor/entries/:entryId', component: SupervisorEntryDetailComponent, canActivate: [authGuard, roleGuard(['Supervisor', 'Admin'])] },
  { path: 'supervisor/reports/:reportId', component: SupervisorReportDetailComponent, canActivate: [authGuard, roleGuard(['Supervisor', 'Admin'])] },
  { path: 'admin/dashboard', component: AdminDashboardComponent, canActivate: [authGuard, roleGuard(['Admin'])] },
  { path: 'admin/users', component: AdminUsersComponent, canActivate: [authGuard, roleGuard(['Admin'])] },
  { path: 'admin/reports/:reportId', component: AdminReportDetailComponent, canActivate: [authGuard, roleGuard(['Admin'])] },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' },
];
