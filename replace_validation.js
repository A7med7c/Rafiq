const fs = require('fs');
const path = require('path');

const dir = 'src/Rafiq.Application/Features';
const keys = {};

function walk(dir, callback) {
    fs.readdirSync(dir).forEach(f => {
        let dirPath = path.join(dir, f);
        let isDirectory = fs.statSync(dirPath).isDirectory();
        isDirectory ? walk(dirPath, callback) : callback(dirPath);
    });
}

const keyMap = {};
let counter = 1;

walk(dir, (file) => {
    if (!file.endsWith('Validator.cs')) return;
    
    let content = fs.readFileSync(file, 'utf8');
    let changed = false;
    
    // Regex to match .WithMessage("some string")
    const regex = /\.WithMessage\(\s*"([^"]+)"\s*\)/g;
    
    content = content.replace(regex, (match, p1) => {
        // p1 is the string inside quotes
        let key = Object.keys(keyMap).find(k => keyMap[k] === p1);
        if (!key) {
            // Generate a simple key
            let clean = p1.replace(/[^a-zA-Z0-9 ]/g, '').split(' ').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join('');
            if (clean.length > 30) clean = clean.substring(0, 30);
            key = "Validation." + clean;
            
            // ensure unique
            let temp = key;
            let i = 2;
            while(Object.keys(keyMap).includes(temp) && keyMap[temp] !== p1) {
                temp = key + i;
                i++;
            }
            key = temp;
            keyMap[key] = p1;
        }
        
        changed = true;
        return `.WithMessage("${key}")`;
    });
    
    if (changed) {
        fs.writeFileSync(file, content, 'utf8');
        console.log(`Updated ${file}`);
    }
});

fs.writeFileSync('validation_keys.json', JSON.stringify(keyMap, null, 2), 'utf8');
console.log('Done! Keys written to validation_keys.json');
