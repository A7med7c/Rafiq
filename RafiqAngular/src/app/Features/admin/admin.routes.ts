import { Routes } from '@angular/router';
import { AdminLayoutComponent } from './layout/admin-layout.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard/admin-dashboard.component')
            .then(module => module.AdminDashboardComponent)
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./pages/users/admin-users.component')
            .then(module => module.AdminUsersComponent)
      },
      ...[
        'doctors',
        'families',
        'medical-records',
        'appointments',
        'medications',
        'notifications',
        'documents',
        'ai-analysis',
        'statistics',
        'settings',
        'audit-logs',
        'profile'
      ].map(path => ({
        path,
        loadComponent: () =>
          import('./pages/placeholder/admin-placeholder.component')
            .then(module => module.AdminPlaceholderComponent)
      }))
    ]
  }
];
