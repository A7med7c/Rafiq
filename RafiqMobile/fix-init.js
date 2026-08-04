const fs = require('fs');

let ts = fs.readFileSync('src/app/Pages/family-profiles/family-profiles.ts', 'utf8');

let initHook = `
  ngOnInit(): void {
    this.profileCache.ensure();
    this.loadProfiles();
    this.checkAddRoute();
    this.router.events.subscribe(() => this.checkAddRoute());
`;

ts = ts.replace(/ngOnInit\(\): void\s*\{\s*this\.profileCache\.ensure\(\);\s*this\.loadProfiles\(\);/, initHook);
fs.writeFileSync('src/app/Pages/family-profiles/family-profiles.ts', ts);
console.log('done fixing init!');
