import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthHero } from "../../Components/auth-hero/auth-hero";
import { LoginFormComponent } from '../../Components/login-form/login-form';

@Component({
  selector: 'app-login',
  imports: [AuthHero, LoginFormComponent, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login { }
