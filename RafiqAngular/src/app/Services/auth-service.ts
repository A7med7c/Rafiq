import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { AuthResponse } from "../Modles/auth-response";
import { environment } from "../Environments/Environment";

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(
    private http: HttpClient
  ) { }

  loginWithGoogle(idToken: string) {

    return this.http.post<AuthResponse>(
      environment.apiUrl + '/auth/google',
      {
        idToken
      });

  }

}