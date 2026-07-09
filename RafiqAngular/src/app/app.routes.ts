import { Routes } from '@angular/router';
import { Login } from './Pages/Auth/login/login';
import { Register } from './Pages/Auth/register/register';
import { Landing } from './Pages/landing/landing';
import { Dashboard } from './Pages/dashboard/dashboard';
import { MedicalRecords } from './Pages/medical-records/medical-records';
import { authGuard } from './Guards/auth.guard';
import { guestGuard } from './Guards/guest.guard';

export const routes: Routes = [
  { path: '', component: Landing },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  { path: 'medical-records', component: MedicalRecords, canActivate: [authGuard] },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] }
];
