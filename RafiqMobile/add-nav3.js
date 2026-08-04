const fs = require('fs');
let html = fs.readFileSync('src/app/Pages/family-profiles/family-profiles.html', 'utf8');

let idx = html.search(/<\!-- ══════════════════════════════════════════════════════\s+EDIT PROFILE MODAL/);
if (idx > -1) {
  let before = html.substring(0, idx);
  let after = html.substring(idx);

  before = before.replace(/<\/div>\s*<\/div>\s*}\s*$/, "  <app-bottom-nav></app-bottom-nav>\n  </div>\n</div>\n}\n\n");
  fs.writeFileSync('src/app/Pages/family-profiles/family-profiles.html', before + after);
  console.log('Done!');
} else {
  console.log('Not found edit modal either!');
}
