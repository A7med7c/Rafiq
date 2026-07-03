import { Routes } from '@angular/router';
import { Login } from './Pages/Auth/login/login';
import { Landing } from './Pages/landing/landing';

export const routes: Routes = [
    {path:'',component:Landing},
    {path:'login',component:Login}
];
