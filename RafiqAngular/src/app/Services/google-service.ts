import { Injectable } from '@angular/core';

declare const google: any;

@Injectable({
  providedIn: 'root'
})
export class GoogleService {

  initialize(
    clientId: string,
    callback: (credential: string) => void
  ): void {

    google.accounts.id.initialize({
      client_id: clientId,
      callback: (response: any) => callback(response.credential)
    });

    google.accounts.id.renderButton(
      document.getElementById('google-button'),
      {
        theme: 'outline',
        size: 'large',
        width: 320,
        text: 'continue_with'
      }
    );
  }

}