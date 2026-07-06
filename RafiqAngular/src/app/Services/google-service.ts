import { Injectable } from '@angular/core';

declare const google: any;

@Injectable({
  providedIn: 'root'
})
export class GoogleService {
  private resizeObserver?: ResizeObserver;
  private renderedButtonWidth = 0;

  initialize(
    clientId: string,
    callback: (credential: string) => void
  ): void {
    if (typeof google === 'undefined') {
      return;
    }

    google.accounts.id.initialize({
      client_id: clientId,
      callback: (response: any) => callback(response.credential)
    });

    const buttonElement = document.getElementById('google-button');

    if (!buttonElement) {
      return;
    }

    this.renderButton(buttonElement);
    this.observeButtonWidth(buttonElement);
  }

  private observeButtonWidth(buttonElement: HTMLElement): void {
    this.resizeObserver?.disconnect();

    if (typeof ResizeObserver === 'undefined') {
      return;
    }

    this.resizeObserver = new ResizeObserver(() => this.renderButton(buttonElement));
    this.resizeObserver.observe(buttonElement);
  }

  private renderButton(buttonElement: HTMLElement): void {
    const buttonWidth = Math.floor(buttonElement.getBoundingClientRect().width);

    if (!buttonWidth || (buttonElement.childElementCount && Math.abs(buttonWidth - this.renderedButtonWidth) < 2)) {
      return;
    }

    this.renderedButtonWidth = buttonWidth;
    buttonElement.innerHTML = '';

    google.accounts.id.renderButton(
      buttonElement,
      {
        theme: 'outline',
        type: 'standard',
        size: 'large',
        shape: 'pill',
        width: Math.min(buttonWidth, 400),
        text: 'signin_with',
        locale: 'en',
        logo_alignment: 'left'
      }
    );
  }
}
