import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthHero } from '../../Components/auth-hero/auth-hero';
import { RegisterFormComponent } from '../../Components/register-form/register-form';

@Component({
  selector: 'app-register',
  imports: [AuthHero, RegisterFormComponent, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register { }
