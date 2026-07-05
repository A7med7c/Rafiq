import { Component } from '@angular/core';
import { AuthHero } from "../../Components/auth-hero/auth-hero";
import { LoginFormComponent } from '../../Components/login-form/login-form';

@Component({
  selector: 'app-login',
  imports: [AuthHero, LoginFormComponent],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login { }
