import {
  Component, OnInit, signal,
  HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Services/auth-service';
import { inject } from '@angular/core';
import { RecordsContentComponent } from '../../Components/records-content/records-content';

@Component({
  selector: 'app-medical-records',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RecordsContentComponent],
  templateUrl: './medical-records.html',
  styleUrl: './medical-records.css',
})
export class MedicalRecords implements OnInit {
  private readonly authService = inject(AuthService);

  readonly sidebarCollapsed  = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen      = signal(false);

  get displayName(): string {
    const u = this.authService.currentUser;
    if (!u) return 'there';
    return u.firstName?.trim() || u.email;
  }

  get userEmail(): string { return this.authService.currentUser?.email ?? ''; }

  ngOnInit(): void { this.applyResponsiveSidebar(); }

  @HostListener('window:resize')
  onWindowResize(): void { this.applyResponsiveSidebar(); }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.hdr-user')) {
      this.dropdownOpen.set(false);
    }
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) this.mobileSidebarOpen.set(false);
  }

  toggleSidebar(): void { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void { this.dropdownOpen.update(v => !v); }
  logout(): void { this.dropdownOpen.set(false); this.authService.logout().subscribe(); }
}
