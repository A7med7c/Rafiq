import { Component } from '@angular/core';
import { AuthHero } from "../../Components/auth-hero/auth-hero";
import { LoginForm } from '../../Components/login-form/login-form';

@Component({
  selector: 'app-login',
  imports: [AuthHero, LoginForm],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {}
