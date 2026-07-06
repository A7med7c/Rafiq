import { Routes } from '@angular/router';
import { Login } from './Pages/Auth/login/login';
import { Register } from './Pages/Auth/register/register';
import { Landing } from './Pages/landing/landing';
import { guestGuard } from './Guards/guest.guard';

export const routes: Routes = [
  { path: '', component: Landing },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] }
];
