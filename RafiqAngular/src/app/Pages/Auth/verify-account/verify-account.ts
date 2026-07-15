import { Component } from '@angular/core';
import { AuthHero } from '../../Components/auth-hero/auth-hero';
import { VerifyAccountFormComponent } from '../../Components/verify-account-form/verify-account-form';

@Component({
  selector: 'app-verify-account',
  imports: [AuthHero, VerifyAccountFormComponent],
  templateUrl: './verify-account.html',
  styleUrl: './verify-account.css',
})
export class VerifyAccount { }
