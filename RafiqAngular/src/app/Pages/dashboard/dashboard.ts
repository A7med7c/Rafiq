import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../Services/auth-service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  private readonly authService = inject(AuthService);

  get displayName(): string {
    const user = this.authService.currentUser;

    if (!user) {
      return 'there';
    }

    return user.firstName?.trim() || user.email;
  }
}
