const fs = require('fs');
const path = require('path');

const htmlPath = path.join(__dirname, 'src', 'app', 'Pages', 'dashboard', 'dashboard.html');
const cssPath = path.join(__dirname, 'src', 'app', 'Pages', 'dashboard', 'dashboard.css');

let html = fs.readFileSync(htmlPath, 'utf8');
let css = fs.readFileSync(cssPath, 'utf8');

// 1. Header background
css = css.replace(/background: var\(--white\);\s*border-bottom: 1px solid var\(--border-lt\);\s*box-shadow: var\(--shadow-sm\);/, 'background: transparent;\n  border-bottom: none;\n  box-shadow: none;');

// 2. AI Health Summary title and colors
html = html.replace(/<span class="ai-title">\{\{ t\(\)\.dashboard\.aiHealthSummary \}\}<\/span>/, '<span class="ai-title text-blue">{{ t().dashboard.aiHealthSummary }}</span>');
html = html.replace(/<span class="pill pill-blue">/, '<span class="pill pill-blue-light">');

// AI stats colors
html = html.replace(/<i class="fa-solid fa-shield-heart health-stat__ico health-stat__ico--green"><\/i>\s*<span class="health-stat__lbl">\{\{ t\(\)\.dashboard\.summaryAllergies \}\}<\/span>\s*<span class="health-stat__val health-stat__val--\{\{ healthSummary\(\)!\.allergies\.length === 0 \? 'good' : 'warn' \}\}">\{\{ healthSummary\(\)!\.allergies\.length === 0 \? t\(\)\.dashboard\.summaryStatusGood : healthSummary\(\)!\.allergies\.length \}\}<\/span>/,
`<i class="fa-solid fa-shield-halved health-stat__ico text-green"></i>
              <span class="health-stat__lbl">{{ t().dashboard.summaryAllergies }}</span>
              <span class="health-stat__val text-green">{{ healthSummary()!.allergies.length === 0 ? t().dashboard.summaryStatusGood : healthSummary()!.allergies.length }}</span>`);

html = html.replace(/<i class="fa-solid fa-flask health-stat__ico health-stat__ico--teal"><\/i>\s*<span class="health-stat__lbl">\{\{ t\(\)\.dashboard\.summaryLabResults \}\}<\/span>\s*<span class="health-stat__val health-stat__val--\{\{ healthSummary\(\)!\.labResults\.status === 'Normal' \? 'good' : 'warn' \}\}">\{\{ healthSummary\(\)!\.labResults\.status === 'Normal' \? t\(\)\.dashboard\.summaryStatusGood : healthSummary\(\)!\.labResults\.abnormalCount \}\}<\/span>/,
`<i class="fa-solid fa-flask health-stat__ico text-green"></i>
              <span class="health-stat__lbl">{{ t().dashboard.summaryLabResults }}</span>
              <span class="health-stat__val text-green">{{ healthSummary()!.labResults.status === 'Normal' ? t().dashboard.summaryStatusGood : healthSummary()!.labResults.abnormalCount }}</span>`);

html = html.replace(/<i class="fa-solid fa-heart-pulse health-stat__ico health-stat__ico--red"><\/i>\s*<span class="health-stat__lbl">\{\{ t\(\)\.dashboard\.summaryConditions \}\}<\/span>\s*<span class="health-stat__val health-stat__val--\{\{ healthSummary\(\)!\.conditions\.length === 0 \? 'neutral' : 'warn' \}\}">\{\{ healthSummary\(\)!\.conditions\.length === 0 \? t\(\)\.dashboard\.summaryNoConditions : healthSummary\(\)!\.conditions\.length \}\}<\/span>/,
`<i class="fa-solid fa-heart-pulse health-stat__ico text-teal"></i>
              <span class="health-stat__lbl">{{ t().dashboard.summaryConditions }}</span>
              <span class="health-stat__val text-teal">{{ healthSummary()!.conditions.length === 0 ? t().dashboard.summaryNoConditions : healthSummary()!.conditions.length }}</span>`);

html = html.replace(/<i class="fa-solid fa-pills health-stat__ico health-stat__ico--blue"><\/i>\s*<span class="health-stat__lbl">\{\{ t\(\)\.dashboard\.summaryMedications \}\}<\/span>\s*<span class="health-stat__val health-stat__val--neutral">\{\{ healthSummary\(\)!\.medications\.count \}\} \{\{ t\(\)\.dashboard\.summaryMedActive \}\}<\/span>/,
`<i class="fa-solid fa-pills health-stat__ico text-teal"></i>
              <span class="health-stat__lbl">{{ t().dashboard.summaryMedications }}</span>
              <span class="health-stat__val text-teal">{{ healthSummary()!.medications.count }} Active</span>`);

// AI buttons
html = html.replace(/class="ai-hero__btn ai-hero__btn--ghost"/, 'class="ai-hero__btn ai-hero__btn--outline"');
html = html.replace(/class="ai-hero__btn ai-hero__btn--solid"/, 'class="ai-hero__btn ai-hero__btn--teal"');

// 3. Today section wrapper
html = html.replace(/<div class="today-list">/, '<div class="m-card today-list">');

// Add specific classes to CSS
css += `
.text-blue { color: #0284C7 !important; }
.text-green { color: #16A34A !important; }
.text-teal { color: #0D9488 !important; }

.pill-blue-light {
  background: #E0F2FE;
  color: #0284C7;
  padding: 4px 10px;
  border-radius: 99px;
  font-size: 11px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 4px;
}

.ai-hero__btn--outline {
  background: var(--white);
  color: #0284C7;
  border: 1px solid #0284C7;
  border-radius: 99px;
}

.ai-hero__btn--teal {
  background: #0D9488;
  color: var(--white);
  border: none;
  border-radius: 99px;
  box-shadow: 0 4px 12px rgba(13, 148, 136, 0.25);
}

.health-stat__ico { font-size: 20px; margin-bottom: 4px; }
.health-stat__val { font-size: 13px; font-weight: 800; }

.today-list {
  padding: 0;
  display: flex;
  flex-direction: column;
}
.today-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  border-bottom: 1px solid var(--border-lt);
  text-decoration: none;
}
.today-row:last-child { border-bottom: none; }
.today-row__ico {
  width: 44px; height: 44px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center; font-size: 18px;
}
.today-row__ico--blue { background: #E0F2FE; color: #0284C7; }
.today-row__ico--green { background: #DCFCE7; color: #16A34A; }
.today-row__title { margin: 0; font-size: 14px; font-weight: 700; color: var(--text); }
.today-row__sub { margin: 2px 0 0; font-size: 12px; color: var(--text-3); }
.today-row__chev { color: var(--text-4); margin-left: auto; }
.today-badge--taken { background: #DCFCE7; color: #16A34A; padding: 4px 10px; border-radius: 99px; font-size: 11px; font-weight: 700; margin-left: auto; }
`;

fs.writeFileSync(htmlPath, html, 'utf8');
fs.writeFileSync(cssPath, css, 'utf8');
console.log('Done');
