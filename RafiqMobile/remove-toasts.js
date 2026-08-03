const fs = require('fs');
const path = require('path');

function walk(dir) {
  let results = [];
  const list = fs.readdirSync(dir);
  list.forEach(file => {
    let filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    if (stat && stat.isDirectory()) {
      results = results.concat(walk(filePath));
    } else if (filePath.endsWith('.ts')) {
      results.push(filePath);
    }
  });
  return results;
}

const files = walk('./src/app');

files.forEach(file => {
  let content = fs.readFileSync(file, 'utf8');
  let originalContent = content;
  
  let i = 0;
  while (i < content.length) {
    const idx = content.indexOf('showToast(', i);
    if (idx === -1) break;
    
    // Check if it is a call
    const before = content.substring(Math.max(0, idx - 20), idx);
    if (before.includes('this.') || before.includes('notif')) {
      let openCount = 0;
      let j = idx + 'showToast'.length;
      let inString = false;
      let stringChar = '';
      
      while (j < content.length) {
        const char = content[j];
        if (!inString && (char === '"' || char === "'" || char === '`')) {
          inString = true;
          stringChar = char;
        } else if (inString && char === stringChar && content[j-1] !== '\\') {
          inString = false;
        } else if (!inString) {
          if (char === '(') openCount++;
          if (char === ')') {
            openCount--;
            if (openCount === 0) {
              j++;
              if (content[j] === ';') j++;
              
              let startIdx = idx;
              while (startIdx > 0 && /[a-zA-Z0-9_.]/.test(content[startIdx - 1])) {
                startIdx--;
              }
              
              let endIdx = j;
              while (content[endIdx] === ' ' || content[endIdx] === '\t') endIdx++;
              if (content[endIdx] === '\r') endIdx++;
              if (content[endIdx] === '\n') endIdx++;
              
              content = content.substring(0, startIdx) + content.substring(endIdx);
              i = startIdx;
              break;
            }
          }
        }
        j++;
      }
    } else {
      i = idx + 'showToast('.length;
    }
  }
  
  if (content !== originalContent) {
    fs.writeFileSync(file, content);
    console.log('Modified ' + file);
  }
});
