const fs = require('fs');

const original = fs.readFileSync('src/app/Pages/my-profile/my-profile.html.bak', 'utf-8');

function extractSection(id, str) {
    const startStr = '<section class="mp-section" id="' + id + '">';
    const startIdx = str.indexOf(startStr);
    if (startIdx === -1) return '';
    
    // Find matching </section>
    let count = 1;
    const startTagStr = '<section';
    const endTagStr = '</section>';
    
    let currentIdx = startIdx + startStr.length;
    let endIdx = -1;
    
    while (currentIdx < str.length) {
        const nextStart = str.indexOf(startTagStr, currentIdx);
        const nextEnd = str.indexOf(endTagStr, currentIdx);
        
        if (nextEnd === -1) break; // Error
        
        if (nextStart !== -1 && nextStart < nextEnd) {
            count++;
            currentIdx = nextStart + startTagStr.length;
        } else {
            count--;
            currentIdx = nextEnd + endTagStr.length;
            if (count === 0) {
                endIdx = currentIdx;
                break;
            }
        }
    }
    
    if (endIdx !== -1) {
        return str.substring(startIdx, endIdx);
    }
    return '';
}

const personalForm = original.match(/<form \[formGroup\]="personalForm"[\s\S]*?<\/form>/)[0];
const healthForm = original.match(/<form \[formGroup\]="healthForm"[\s\S]*?<\/form>/)[0];
const allergies = extractSection('allergies-section', original);
const diseases = extractSection('diseases-section', original);
const contacts = extractSection('contacts-section', original);

let modals = '';
const photoModalIdx = original.indexOf('<!-- ════════════ PHOTO MODAL');
if (photoModalIdx !== -1) {
    modals = original.substring(photoModalIdx);
} else {
    const photoModalAlt = original.indexOf('<div class="photo-modal-overlay"');
    if (photoModalAlt !== -1) {
       modals = original.substring(original.lastIndexOf('@if (photoModalOpen())'));
    }
}

const html = `
<div class="mp-container">
  @if (activeView() === 'main') {
    <!-- MAIN PROFILE VIEW -->
    <header class="mp-header-minimal">
      <div class="mp-header-minimal-left">
        <div class="mp-header-minimal-av" (click)="openPhotoModal()">
          <img [src]="profileCache.resolveNavbarAvatar()" alt="Avatar">
          <div class="mp-header-minimal-av-edit"><i class="fa-solid fa-camera"></i></div>
        </div>
        <div>
          <h1 class="mp-header-title">{{ t().myProfile.pageTitle }}</h1>
          <p class="mp-header-subtitle">Manage your account and healthcare information.</p>
        </div>
      </div>
      <div class="mp-header-minimal-right">
        <button class="mp-icon-btn" (click)="notifService.openNotificationCenter()">
          <i class="fa-regular fa-bell"></i>
          @if (unreadNotifCount() > 0) {
            <span class="mp-notif-badge">{{ unreadNotifCount() }}</span>
          }
        </button>
      </div>
    </header>

    @if (profileLoading()) {
      <div class="mp-loading-state">
        <span class="mp-spinner mp-spinner--lg"></span>
      </div>
    } @else if (profile()) {
      <div class="mp-content-padded">
        <!-- PROFILE CARD -->
        <div class="mp-profile-card">
          <div class="mp-profile-card-top">
            <img [src]="profileCache.resolveNavbarAvatar()" alt="User Avatar" class="mp-profile-card-img" (click)="openPhotoModal()">
            <div class="mp-profile-card-info">
              <h2 class="mp-profile-card-name">{{ profile()!.firstName }} {{ profile()!.lastName }}</h2>
              <p class="mp-profile-card-email">{{ profile()!.email }}</p>
              <p class="mp-profile-card-phone">{{ profile()!.phoneNumber || 'No phone number' }}</p>
              <span class="mp-badge mp-badge--primary">Self Profile</span>
            </div>
          </div>
          <div class="mp-profile-card-stats">
            <div class="mp-profile-stat">
              <span class="mp-stat-label">Member Since</span>
              <span class="mp-stat-value">2023</span>
            </div>
            <div class="mp-profile-stat">
              <span class="mp-stat-label">Family Account</span>
              <span class="mp-stat-value">Active</span>
            </div>
            <div class="mp-profile-stat">
              <span class="mp-stat-label">Role</span>
              <span class="mp-stat-value">Owner</span>
            </div>
          </div>
        </div>

        <!-- NAVIGATION CARDS -->
        <div class="mp-nav-grid">
          <button class="mp-nav-card" (click)="activeView.set('personal'); scrollToTop()">
            <div class="mp-nav-card-icon"><i class="fa-solid fa-user"></i></div>
            <div class="mp-nav-card-text">
              <h3>Personal Information</h3>
              <p>Name, email, phone, address</p>
            </div>
            <i class="fa-solid fa-chevron-right mp-nav-card-arrow"></i>
          </button>
          
          <button class="mp-nav-card" (click)="activeView.set('health'); scrollToTop()">
            <div class="mp-nav-card-icon"><i class="fa-solid fa-heart-pulse"></i></div>
            <div class="mp-nav-card-text">
              <h3>Health Information</h3>
              <p>Vitals, allergies, conditions</p>
            </div>
            <i class="fa-solid fa-chevron-right mp-nav-card-arrow"></i>
          </button>
          
          <button class="mp-nav-card" (click)="activeView.set('emergency'); scrollToTop()">
            <div class="mp-nav-card-icon"><i class="fa-solid fa-phone-volume"></i></div>
            <div class="mp-nav-card-text">
              <h3>Emergency Contacts</h3>
              <p>Primary and backup contacts</p>
            </div>
            <i class="fa-solid fa-chevron-right mp-nav-card-arrow"></i>
          </button>
          
          <button class="mp-nav-card" routerLink="/family-profiles">
            <div class="mp-nav-card-icon"><i class="fa-solid fa-users"></i></div>
            <div class="mp-nav-card-text">
              <h3>Family Members</h3>
              <p>Manage linked accounts</p>
            </div>
            <i class="fa-solid fa-chevron-right mp-nav-card-arrow"></i>
          </button>
          
          <button class="mp-nav-card" (click)="activeView.set('settings'); scrollToTop()">
            <div class="mp-nav-card-icon"><i class="fa-solid fa-gear"></i></div>
            <div class="mp-nav-card-text">
              <h3>Settings</h3>
              <p>App preferences and options</p>
            </div>
            <i class="fa-solid fa-chevron-right mp-nav-card-arrow"></i>
          </button>
          
          <button class="mp-nav-card" (click)="activeView.set('settings'); scrollToTop()">
            <div class="mp-nav-card-icon"><i class="fa-solid fa-shield-halved"></i></div>
            <div class="mp-nav-card-text">
              <h3>Privacy & Security</h3>
              <p>Data sharing and password</p>
            </div>
            <i class="fa-solid fa-chevron-right mp-nav-card-arrow"></i>
          </button>
        </div>

        <!-- BOTTOM SECTION -->
        <div class="mp-danger-zone-minimal">
          <button class="mp-btn-delete-account" (click)="openDeleteModal()">
            <i class="fa-solid fa-triangle-exclamation"></i>
            Delete Account
          </button>
          <div class="mp-secure-info">
            <i class="fa-solid fa-lock"></i>
            <p>Your data is securely encrypted and HIPAA compliant.</p>
          </div>
        </div>
      </div>
    }
    
    <app-bottom-nav></app-bottom-nav>

  } @else if (activeView() === 'personal') {
    <!-- PERSONAL INFORMATION VIEW -->
    <header class="mp-sub-header">
      <button class="mp-back-btn" (click)="activeView.set('main'); scrollToTop()"><i class="fa-solid fa-arrow-left"></i></button>
      <h2>Personal Information</h2>
      @if (!editingPersonal()) {
        <button class="mp-icon-btn" (click)="editPersonal()"><i class="fa-solid fa-pen"></i></button>
      }
    </header>
    <div class="mp-sub-content">
      ${personalForm}
    </div>

  } @else if (activeView() === 'health') {
    <!-- HEALTH INFORMATION VIEW -->
    <header class="mp-sub-header">
      <button class="mp-back-btn" (click)="activeView.set('main'); scrollToTop()"><i class="fa-solid fa-arrow-left"></i></button>
      <h2>Health Information</h2>
      @if (!editingHealth()) {
        <button class="mp-icon-btn" (click)="editHealth()"><i class="fa-solid fa-pen"></i></button>
      }
    </header>
    <div class="mp-sub-content">
      ${healthForm}
      ${allergies}
      ${diseases}
    </div>

  } @else if (activeView() === 'emergency') {
    <!-- EMERGENCY CONTACTS VIEW -->
    <header class="mp-sub-header">
      <button class="mp-back-btn" (click)="activeView.set('main'); scrollToTop()"><i class="fa-solid fa-arrow-left"></i></button>
      <h2>Emergency Contacts</h2>
    </header>
    <div class="mp-sub-content">
      ${contacts}
    </div>
    
  } @else if (activeView() === 'settings') {
    <!-- SETTINGS VIEW -->
    <header class="mp-sub-header">
      <button class="mp-back-btn" (click)="activeView.set('main'); scrollToTop()"><i class="fa-solid fa-arrow-left"></i></button>
      <h2>Settings</h2>
    </header>
    <div class="mp-sub-content">
      <div class="mp-settings-group">
        <h3>Notifications</h3>
        <div class="mp-setting-item">
          <span>Medication reminders</span>
          <label class="mp-switch"><input type="checkbox" checked><span class="mp-slider round"></span></label>
        </div>
        <div class="mp-setting-item">
          <span>Appointment reminders</span>
          <label class="mp-switch"><input type="checkbox" checked><span class="mp-slider round"></span></label>
        </div>
      </div>
      
      <div class="mp-settings-group">
        <h3>Privacy & Security</h3>
        <div class="mp-setting-item">
          <span>Biometric login</span>
          <label class="mp-switch"><input type="checkbox" checked><span class="mp-slider round"></span></label>
        </div>
        <button class="mp-setting-btn">
            <span>Change password</span>
            <i class="fa-solid fa-chevron-right"></i>
        </button>
      </div>
      
      <div class="mp-settings-group">
        <h3>About</h3>
        <button class="mp-setting-btn">
            <span>Help & Support</span>
            <i class="fa-solid fa-chevron-right"></i>
        </button>
        <button class="mp-setting-btn">
            <span>Privacy Policy</span>
            <i class="fa-solid fa-chevron-right"></i>
        </button>
        <button class="mp-setting-btn">
            <span>Terms of Service</span>
            <i class="fa-solid fa-chevron-right"></i>
        </button>
      </div>
      
      <button class="mp-btn-logout" (click)="logout()">
        <i class="fa-solid fa-arrow-right-from-bracket"></i>
        Logout
      </button>
    </div>
  }
</div>

${modals}
`;

fs.writeFileSync('src/app/Pages/my-profile/my-profile.html', html, 'utf-8');
console.log("HTML successfully rewritten!");
