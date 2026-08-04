const fs = require('fs');
let html = fs.readFileSync('src/app/Pages/family-profiles/family-profiles.html', 'utf8');

// 1. Dashboard wrap
if (!html.includes('@if (!showAddModal()) {')) {
  html = html.replace('<div class="m-page">', '@if (!showAddModal()) {\n<div class="m-page">');
}

// 2. Dashboard wrap end
if (!html.includes('</div><!-- /m-page -->\n}')) {
  html = html.replace('</div><!-- /m-page -->', '</div><!-- /m-page -->\n}');
}

// 3. Add Flow modal styles
html = html.replace(
  '<div class="modal-overlay" style="background-color: white; z-index: 1000;" (click)="closeAddModal()">',
  '<div class="m-page" style="background-color: #F8FAFC;">'
);
html = html.replace(
  '<div style="width: 100%; min-height: 100vh; background-color: #F8FAFC; display: flex; flex-direction: column;" (click)="$event.stopPropagation()">',
  '<div style="width: 100%; min-height: 100vh; display: flex; flex-direction: column;">'
);

// 4. Empty state button
html = html.replace(
  '<button type="button" class="fph-btn-primary" (click)="openAddModal()">',
  '<button type="button" class="fph-btn-primary" [routerLink]="[\'/family-profiles/add\']">'
);

// 5. Add nav to Add Flow
let splitText = '<!-- ══════════════════════════════════════════════════════\n     EDIT PROFILE MODAL';
let idx = html.indexOf(splitText);
if (idx > -1) {
  let before = html.substring(0, idx);
  let after = html.substring(idx);
  if (!before.includes('<app-bottom-nav></app-bottom-nav>')) {
    before = before.replace(/<\/div>\s*<\/div>\s*}\s*$/, "  <app-bottom-nav></app-bottom-nav>\n  </div>\n</div>\n}\n\n");
    html = before + after;
  }
}

// 6. Update Image Picker buttons
html = html.replace(
  '<button type="button" class="fp-btn-primary" style="padding: 8px 16px; font-size: 14px;" (click)="selectCreateImage()">',
  '<button type="button" class="fp-btn-outline" style="padding: 8px 16px; font-size: 14px; border: 1px solid #CBD5E1; border-radius: 6px; background-color: #F8FAFC; color: #1E293B;" (click)="selectCreateImage()">'
);
html = html.replace(
  '<button type="button" class="fp-btn-primary" style="padding: 8px 16px; font-size: 14px;" (click)="selectEditImage()">',
  '<button type="button" class="fp-btn-outline" style="padding: 8px 16px; font-size: 14px; border: 1px solid #CBD5E1; border-radius: 6px; background-color: #F8FAFC; color: #1E293B;" (click)="selectEditImage()">'
);

fs.writeFileSync('src/app/Pages/family-profiles/family-profiles.html', html);
console.log('All changes applied!');
