const fs = require('fs');
let html = fs.readFileSync('src/app/Pages/family-profiles/family-profiles.html', 'utf8');

let splitText = '<!-- ══════════════════════════════════════════════════════\n     SUPERVISE PERMISSIONS MODAL';
let idx = html.indexOf(splitText);

if (idx > -1) {
  let before = html.substring(0, idx);
  let after = html.substring(idx);

  // Replace the ending brackets with the bottom nav included
  before = before.replace(/<\/div>\s*<\/div>\s*}\s*$/, "  </div>\n  <app-bottom-nav></app-bottom-nav>\n</div>\n}\n\n");
  fs.writeFileSync('src/app/Pages/family-profiles/family-profiles.html', before + after);
  console.log('Done padding bottom nav');
} else {
  console.log('Could not find split text');
}
