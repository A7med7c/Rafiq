const fs = require('fs');

// 1. Update family-profiles.ts
let ts = fs.readFileSync('src/app/Pages/family-profiles/family-profiles.ts', 'utf8');

let checkAddRouteFunc = `
  checkAddRoute() {
    if (this.router.url.endsWith('/family-profiles/add')) {
      if (!this.showAddModal()) {
        this.openAddModal();
      }
    } else {
      if (this.showAddModal()) {
        this.showAddModal.set(false);
      }
    }
  }
`;

ts = ts.replace(/openAddModal\(\): void \{/, checkAddRouteFunc + '\n  openAddModal(): void {');

let initHook = `
  ngOnInit(): void {
    this.loadProfiles();
    this.checkAddRoute();
    this.router.events.subscribe(() => this.checkAddRoute());
`;

ts = ts.replace(/ngOnInit\(\): void\s*\{\s*this\.loadProfiles\(\);/, initHook);

ts = ts.replace(/closeAddModal\(\): void \{ this\.showAddModal\.set\(false\); \}/, 
  "closeAddModal(): void { this.router.navigate(['/family-profiles']); }");

fs.writeFileSync('src/app/Pages/family-profiles/family-profiles.ts', ts);

// 2. Update family-profiles.html
let html = fs.readFileSync('src/app/Pages/family-profiles/family-profiles.html', 'utf8');

html = html.replace(/\(click\)="openAddModal\(\)"/g, "[routerLink]=\"['/family-profiles/add']\"");

html = html.replace(/<div class="m-page">/, "@if (!showAddModal()) {\n<div class=\"m-page\">");

html = html.replace(/<\!-- ══════════════════════════════════════════════════════\n     ADD FAMILY MEMBER MODAL\n══════════════════════════════════════════════════════ -->/,
"}\n\n<!-- ══════════════════════════════════════════════════════\n     ADD FAMILY MEMBER PAGE FLOW\n══════════════════════════════════════════════════════ -->");

html = html.replace(/<div class="modal-overlay" style="background-color: white; z-index: 1000;" \(click\)="closeAddModal\(\)">/,
"<div class=\"m-page\" style=\"background-color: #F8FAFC;\">");

html = html.replace(/<div style="width: 100%; min-height: 100vh; background-color: #F8FAFC; display: flex; flex-direction: column;" \(click\)="\$event\.stopPropagation\(\)">/,
"<div style=\"width: 100%; min-height: 100vh; display: flex; flex-direction: column;\">");

// Add bottom nav to the add flow!
html = html.replace(/<\/div>\s*}\s*$/m, "</div>\n  <app-bottom-nav></app-bottom-nav>\n</div>\n}");

fs.writeFileSync('src/app/Pages/family-profiles/family-profiles.html', html);
console.log('done!');
