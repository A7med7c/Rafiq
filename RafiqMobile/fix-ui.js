const fs = require('fs');
const path = require('path');

const htmlPath = path.join(__dirname, 'src', 'app', 'Pages', 'dashboard', 'dashboard.html');
const cssPath = path.join(__dirname, 'src', 'app', 'Pages', 'dashboard', 'dashboard.css');

let html = fs.readFileSync(htmlPath, 'utf8');
let css = fs.readFileSync(cssPath, 'utf8');

// 1. AI Health Summary actions
html = html.replace(
  `          </div>\n\n        }`,
  `          </div>\n\n          <div class="ai-hero__actions" style="margin-top: 16px;">\n            <button type="button" class="ai-hero__btn ai-hero__btn--ghost" (click)="aiChatService.openPanel()">\n              <i class="fa-solid fa-robot"></i> Ask AI for details\n            </button>\n            <button type="button" class="ai-hero__btn ai-hero__btn--solid" (click)="aiChatService.openPanel()">\n              <i class="fa-solid fa-wand-magic-sparkles"></i> Rafiq AI\n            </button>\n          </div>\n\n        }`
);

// 2. Today section
html = html.replace(
  `<h3 class="m-section__title">{{ t().dashboard.today }}</h3>\n      </div>`,
  `<h3 class="m-section__title">{{ t().dashboard.today }}</h3>\n        <a routerLink="/appointments" class="m-section__link">View all</a>\n      </div>`
);
html = html.replace(
  `            <a routerLink="/medications" class="today-row">\n              <div class="today-row__ico today-row__ico--green"><i class="fa-solid fa-capsules"></i></div>\n              <div class="today-row__info">\n                <p class="today-row__title">{{ t().dashboard.nextMedication }}</p>\n                <p class="today-row__sub">{{ rem.medicineName }} · {{ rem.reminderTime }}</p>\n              </div>\n              @if (getMedStatus(rem) === 'taken') {\n                <span class="today-badge today-badge--taken">Taken</span>\n              }\n              <i class="fa-solid fa-chevron-right today-row__chev"></i>\n            </a>\n          }`,
  `            <a routerLink="/medications" class="today-row">\n              <div class="today-row__ico today-row__ico--green"><i class="fa-solid fa-capsules"></i></div>\n              <div class="today-row__info">\n                <p class="today-row__title">{{ t().dashboard.nextMedication }}</p>\n                <p class="today-row__sub">{{ rem.medicineName }} · {{ rem.reminderTime }}</p>\n              </div>\n              @if (getMedStatus(rem) === 'taken') {\n                <span class="today-badge today-badge--taken">Taken</span>\n              }\n              <i class="fa-solid fa-chevron-right today-row__chev"></i>\n            </a>\n          }\n          <a routerLink="/health" class="today-row" style="cursor: default;">\n            <div class="today-row__ico today-row__ico--blue"><i class="fa-solid fa-droplet"></i></div>\n            <div class="today-row__info">\n              <p class="today-row__title">Water Goal</p>\n              <p class="today-row__sub" style="margin-bottom: 6px;">4 / 8 glasses</p>\n              <div style="height: 6px; width: 100%; max-width: 120px; background: #E5E7EB; border-radius: 3px; overflow: hidden;">\n                <div style="height: 100%; width: 50%; background: var(--blue); border-radius: 3px;"></div>\n              </div>\n            </div>\n            <i class="fa-solid fa-chevron-right today-row__chev"></i>\n          </a>`
);

// 3. Quick Actions
html = html.replace(
  /<div class="quick-actions">[\s\S]*?<\/section>/,
  `<div class="quick-actions">\n        <a routerLink="/medical-records" class="quick-action-box">\n          <span class="quick-action-box__circle bg-blue-light"><i class="fa-solid fa-cloud-arrow-up txt-blue"></i></span>\n          <span class="quick-action-box__lbl">{{ t().dashboard.uploadRecord }}</span>\n        </a>\n        <a routerLink="/appointments" class="quick-action-box">\n          <span class="quick-action-box__circle bg-purple-light"><i class="fa-regular fa-calendar txt-purple"></i></span>\n          <span class="quick-action-box__lbl">{{ t().sidebar.appointments }}</span>\n        </a>\n        <a routerLink="/medications" class="quick-action-box">\n          <span class="quick-action-box__circle bg-orange-light"><i class="fa-regular fa-bell txt-orange"></i></span>\n          <span class="quick-action-box__lbl">{{ t().dashboard.reminders }}</span>\n        </a>\n        <button type="button" class="quick-action-box" (click)="aiChatService.openPanel()">\n          <span class="quick-action-box__circle bg-teal-light"><i class="fa-solid fa-robot txt-teal"></i></span>\n          <span class="quick-action-box__lbl">{{ t().dashboard.aiChat }}</span>\n        </button>\n        <a routerLink="/family-profiles" class="quick-action-box">\n          <span class="quick-action-box__circle bg-purple-light"><i class="fa-solid fa-people-roof txt-purple"></i></span>\n          <span class="quick-action-box__lbl">{{ t().dashboard.family }}</span>\n        </a>\n      </div>\n    </section>`
);

// 4. Family Overview
html = html.replace(
  /@for \(profile of familyAvatars\(\); track profile\.userHealthProfileId\) \{[\s\S]*?\}[\s]*<a[\s\S]*?<\/a>/,
  `@for (profile of familyAvatars(); track profile.userHealthProfileId) {
            <button type="button" class="family-card" [assistantAnchor]="!profile.isSelf ? 'family-member-card' : ''"
              (click)="openFamilySummary(profile)">
              <span class="family-card__av" [style.background]="getProfileAvatarColor(profile.firstName)">
                @if (profile.profileImageUrl) {
                  <img [src]="profile.profileImageUrl" [alt]="profile.firstName">
                } @else {
                  {{ getInitial(profile.firstName) }}
                }
                <span class="family-card__dot"></span>
              </span>
              <span class="family-card__name" [class.family-card__name--self]="profile.isSelf">
                {{ profile.isSelf ? displayName : profile.firstName }}
              </span>
              <span class="family-card__sub">{{ profile.isSelf ? t().dashboard.you : getRelationshipLabel(profile.relationship) }}</span>
            </button>
          }
          <a assistantAnchor="add-family-member-button" class="family-card family-card--add" routerLink="/family-profiles">
            <span class="family-card__av family-card__av--add"><i class="fa-solid fa-plus"></i></span>
            <span class="family-card__name" style="color: var(--blue);">Add</span>
            <span class="family-card__sub" style="color: var(--blue);">Member</span>
          </a>`
);

// 5. Recent Medical Records
html = html.replace(
  /<div class="rec-type-badge rec-type-badge--\{\{ getRecordBadgeColor\(rec\.type\) \}\}">[\s\S]*?<\/div>/g,
  `<div class="rec-file-ico rec-file-ico--{{ getRecordBadgeColor(rec.type) }}">
                    <i class="fa-solid fa-file-{{ getRecordBadgeType(rec.type) === 'PDF' ? 'pdf' : 'image' }}"></i>
                    <span class="rec-file-lbl">{{ getRecordBadgeType(rec.type) }}</span>
                  </div>`
);

// 6. Health Tip
html = html.replace(
  /<div class="tip-card">[\s\S]*?<\/section>/,
  `<div class="tip-banner">
        <div class="tip-banner__content">
          <div class="tip-banner__hdr">
            <i class="fa-solid fa-lightbulb tip-banner__ico"></i>
            <span class="tip-banner__title">{{ t().dashboard.healthTip }}</span>
          </div>
          <p class="tip-banner__text">{{ t().dashboard.healthTipText }}</p>
        </div>
        <div class="tip-banner__ill">
          <svg viewBox="0 0 100 100" fill="none" xmlns="http://www.w3.org/2000/svg" style="width: 100%; height: 100%;">
            <rect x="25" y="20" width="45" height="65" rx="5" fill="#7DD3FC" opacity="0.4" />
            <rect x="28" y="25" width="39" height="58" rx="3" fill="#38BDF8" opacity="0.6" />
            <path d="M25 50 Q47.5 40 70 50 L70 80 Q47.5 90 25 80 Z" fill="#0284C7" opacity="0.3" />
            <path d="M25 50 Q47.5 60 70 50 L70 80 Q47.5 70 25 80 Z" fill="#38BDF8" />
            <path d="M15 80 Q20 70 25 75 Q30 80 25 85 Q20 90 15 80 Z" fill="#22C55E" />
            <path d="M75 75 Q80 65 85 70 Q90 75 85 80 Q80 85 75 75 Z" fill="#16A34A" />
            <path d="M85 85 Q90 75 95 80 Q100 85 95 90 Q90 95 85 85 Z" fill="#22C55E" />
          </svg>
        </div>
      </div>
    </section>`
);

css = css.replace(/--r: 16px;/, '--r: 20px;');
css = css.replace(/--bg: #F4F7FB;/, '--bg: #F8FAFC;');

css += `
/* Additions for UI update */
.bg-blue-light { background: #E0F2FE; }
.bg-purple-light { background: #F3E8FF; }
.bg-orange-light { background: #FFEDD5; }
.bg-teal-light { background: #CCFBF1; }
.txt-blue { color: #0284C7; }
.txt-purple { color: #7E22CE; }
.txt-orange { color: #EA580C; }
.txt-teal { color: #0F766E; }

.quick-action-box {
  background: var(--white);
  border-radius: 16px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 12px 6px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.04);
  text-decoration: none;
  border: 1px solid var(--border-lt);
  flex: 1;
  min-width: 72px;
}

.quick-action-box__circle {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
}

.quick-action-box__lbl {
  font-size: 11px;
  font-weight: 600;
  color: var(--text-2);
  text-align: center;
}

.quick-actions {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding: 4px 2px 8px;
  scrollbar-width: none;
}
.quick-actions::-webkit-scrollbar { display: none; }

.family-card {
  background: var(--white);
  border-radius: 16px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(0,0,0,0.04);
  border: 1px solid var(--border-lt);
  width: 96px;
  flex-shrink: 0;
  gap: 4px;
}

.family-card__av {
  position: relative;
  width: 56px;
  height: 56px;
  border-radius: 50%;
  margin-bottom: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  color: var(--white);
  font-weight: 700;
}
.family-card__av img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
}
.family-card__dot {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 14px;
  height: 14px;
  background: var(--green);
  border: 2px solid var(--white);
  border-radius: 50%;
}
.family-card__name {
  font-size: 13px;
  font-weight: 700;
  color: var(--text-2);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  width: 100%;
  text-align: center;
}
.family-card__name--self { color: var(--blue); }
.family-card__sub {
  font-size: 11px;
  color: var(--text-3);
}

.family-card--add {
  background: #F0F9FF;
  border: 1px dashed #7DD3FC;
  box-shadow: none;
}
.family-card__av--add {
  background: transparent;
  color: var(--blue);
  border: none;
  font-size: 24px;
}

.rec-file-ico {
  width: 44px;
  height: 56px;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  font-size: 18px;
}
.rec-file-ico--red { background: #FEE2E2; color: #DC2626; }
.rec-file-ico--blue { background: #DBEAFE; color: #2563EB; }
.rec-file-lbl { font-size: 10px; font-weight: 800; }

.tip-banner {
  background: #FFFBEB;
  border-radius: 16px;
  padding: 16px;
  display: flex;
  gap: 16px;
  align-items: center;
  overflow: hidden;
  position: relative;
}
.tip-banner__content {
  flex: 1;
  z-index: 2;
}
.tip-banner__hdr {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #D97706;
  font-weight: 800;
  font-size: 14px;
  margin-bottom: 8px;
}
.tip-banner__text {
  font-size: 12px;
  color: var(--text-2);
  margin: 0;
  line-height: 1.5;
}
.tip-banner__ill {
  width: 90px;
  height: 90px;
  flex-shrink: 0;
  position: relative;
  z-index: 1;
}

.m-card {
  background: var(--white);
  border-radius: 20px;
  padding: 16px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.03);
}
`;

fs.writeFileSync(htmlPath, html, 'utf8');
fs.writeFileSync(cssPath, css, 'utf8');
console.log('Done');
