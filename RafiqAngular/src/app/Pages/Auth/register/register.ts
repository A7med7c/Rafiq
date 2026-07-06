import { Component } from '@angular/core';
import { AuthHero } from '../../Components/auth-hero/auth-hero';
import { RegisterFormComponent } from '../../Components/register-form/register-form';

@Component({
  selector: 'app-register',
  imports: [AuthHero, RegisterFormComponent],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register { }
