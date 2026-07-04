import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GoogleService } from '../../../Services/google-service';
import { AuthService } from '../../../Services/auth-service';
import { environment } from '../../../Environments/Environment';
import { TokenStorageService } from '../../../Services/token-storage-service';
import { Router } from '@angular/router';
@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css'
})
export class LoginFormComponent implements OnInit {

  constructor(
    private googleService: GoogleService,
    private authService: AuthService,
    private tokenStorage: TokenStorageService,
    private router: Router

  ) { }

  ngOnInit(): void {

    this.googleService.initialize(
      environment.googleClientId,
      (idToken: string) => {

        this.authService.loginWithGoogle(idToken)
          .subscribe({

            next: (response: any) => {

              this.tokenStorage.setTokens(
                response.data.accessToken,
                response.data.refreshToken
              );

              console.log(environment);
              console.log(environment.googleClientId);

              this.router.navigate(['/']);

            },

            error: (error) => {

              console.error('Google Login Failed', error);

            }

          });

      });

  }
}