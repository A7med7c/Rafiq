import { Routes } from '@angular/router';
import { Login } from './Pages/Auth/login/login';
import { Register } from './Pages/Auth/register/register';
import { Landing } from './Pages/landing/landing';
import { Dashboard } from './Pages/dashboard/dashboard';
import { MedicalRecords } from './Pages/medical-records/medical-records';
import { Appointments } from './Pages/appointments/appointments';
import { AiAssistant } from './Pages/ai-assistant/ai-assistant';
import { OnboardingWelcome } from './Pages/onboarding/onboarding-welcome/onboarding-welcome';
import { OnboardingStep1 } from './Pages/onboarding/onboarding-step1/onboarding-step1';
import { OnboardingStep2 } from './Pages/onboarding/onboarding-step2/onboarding-step2';
import { OnboardingStep3 } from './Pages/onboarding/onboarding-step3/onboarding-step3';
import { OnboardingStep4 } from './Pages/onboarding/onboarding-step4/onboarding-step4';
import { OnboardingAiUpload } from './Pages/onboarding/onboarding-ai-upload/onboarding-ai-upload';
import { authGuard } from './Guards/auth.guard';
import { guestGuard } from './Guards/guest.guard';

export const routes: Routes = [
  { path: '', component: Landing },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  { path: 'medical-records', component: MedicalRecords, canActivate: [authGuard] },
  { path: 'appointments', component: Appointments, canActivate: [authGuard] },
  { path: 'ai-assistant', component: AiAssistant, canActivate: [authGuard] },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'onboarding/welcome', component: OnboardingWelcome, canActivate: [authGuard] },
  { path: 'onboarding/step1',   component: OnboardingStep1,   canActivate: [authGuard] },
  { path: 'onboarding/step2',   component: OnboardingStep2,   canActivate: [authGuard] },
  { path: 'onboarding/step3',   component: OnboardingStep3,   canActivate: [authGuard] },
  { path: 'onboarding/step4',      component: OnboardingStep4,      canActivate: [authGuard] },
  { path: 'onboarding/ai-upload',  component: OnboardingAiUpload,   canActivate: [authGuard] },
];

