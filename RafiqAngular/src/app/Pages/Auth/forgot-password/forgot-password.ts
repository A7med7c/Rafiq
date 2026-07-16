import { Component } from '@angular/core';
import { AuthHero } from '../../Components/auth-hero/auth-hero';
import { ForgotPasswordFormComponent } from '../../Components/forgot-password-form/forgot-password-form';

@Component({
  selector: 'app-forgot-password',
  imports: [AuthHero, ForgotPasswordFormComponent],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword { }
