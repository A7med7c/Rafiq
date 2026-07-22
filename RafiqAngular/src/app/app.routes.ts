import { Routes } from '@angular/router';
import { Login } from './Pages/Auth/login/login';
import { Register } from './Pages/Auth/register/register';
import { ForgotPassword } from './Pages/Auth/forgot-password/forgot-password';
import { VerifyAccount } from './Pages/Auth/verify-account/verify-account';
import { Landing } from './Pages/landing/landing';
import { Dashboard } from './Pages/dashboard/dashboard';
import { MedicalRecords } from './Pages/medical-records/medical-records';
import { Appointments } from './Pages/appointments/appointments';
import { AiAssistant } from './Pages/ai-assistant/ai-assistant';
import { Medications } from './Pages/medications/medications';
import { OnboardingWelcome } from './Pages/onboarding/onboarding-welcome/onboarding-welcome';
import { OnboardingStep1 } from './Pages/onboarding/onboarding-step1/onboarding-step1';
import { OnboardingStep2 } from './Pages/onboarding/onboarding-step2/onboarding-step2';
import { OnboardingStep3 } from './Pages/onboarding/onboarding-step3/onboarding-step3';
import { OnboardingStep4 } from './Pages/onboarding/onboarding-step4/onboarding-step4';
import { OnboardingAiUpload } from './Pages/onboarding/onboarding-ai-upload/onboarding-ai-upload';
import { OnboardingEmergency } from './Pages/onboarding/onboarding-emergency/onboarding-emergency';
import { authGuard } from './Guards/auth.guard';
import { guestGuard } from './Guards/guest.guard';
import { FamilyProfiles } from './Pages/family-profiles/family-profiles';
import { adminGuard } from './Guards/admin.guard';

export const routes: Routes = [
  { path: '', component: Landing },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () =>
      import('./Features/admin/admin.routes').then(module => module.ADMIN_ROUTES)
  },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  { path: 'medical-records', component: MedicalRecords, canActivate: [authGuard] },
  { path: 'appointments', component: Appointments, canActivate: [authGuard] },
  { path: 'medications', component: Medications, canActivate: [authGuard] },
  { path: 'ai-assistant', component: AiAssistant, canActivate: [authGuard] },

  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'forgot-password', component: ForgotPassword, canActivate: [guestGuard] },
  { path: 'verify-account', component: VerifyAccount, canActivate: [guestGuard] },
  { path: 'family-profiles', component: FamilyProfiles, canActivate: [authGuard] },
  { path: 'my-profile', component: MyProfile, canActivate: [authGuard] },
  { path: 'onboarding/welcome', component: OnboardingWelcome, canActivate: [authGuard] },
  { path: 'onboarding/step1', component: OnboardingStep1, canActivate: [authGuard] },
  { path: 'onboarding/step2', component: OnboardingStep2, canActivate: [authGuard] },
  { path: 'onboarding/step3', component: OnboardingStep3, canActivate: [authGuard] },
  { path: 'onboarding/step4', component: OnboardingStep4, canActivate: [authGuard] },
  { path: 'onboarding/ai-upload', component: OnboardingAiUpload, canActivate: [authGuard] },
  { path: 'onboarding/emergency', component: OnboardingEmergency, canActivate: [authGuard] },
];
