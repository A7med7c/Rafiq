import { Component } from '@angular/core';
import { AuthHero } from "../../Components/auth-hero/auth-hero";
import { LoginFormComponent } from '../../Components/login-form/login-form';
import { Hero } from "../../landing/Components/hero/hero";

@Component({
  selector: 'app-login',
  imports: [AuthHero, LoginFormComponent, Hero],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login { }
