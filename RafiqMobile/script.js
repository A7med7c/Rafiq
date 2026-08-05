const fs = require('fs');
const path = require('path');

const aiPanelHtml = fs.readFileSync('src/app/Components/ai-panel/ai-panel.html', 'utf8');
const aiPanelCss = fs.readFileSync('src/app/Components/ai-panel/ai-panel.css', 'utf8');
let aiAssistantHtml = fs.readFileSync('src/app/Pages/ai-assistant/ai-assistant.html', 'utf8');

// Extract the contents of .ai-main from ai-panel.html
// Match from <div class="ai-main"> to </div><!-- /.ai-main -->
const startIndex = aiPanelHtml.indexOf('<div class="ai-main">');
const endIndex = aiPanelHtml.indexOf('</div><!-- /.ai-main -->');
if (startIndex === -1 || endIndex === -1) {
    console.error('Could not find .ai-main in ai-panel.html');
    process.exit(1);
}

let aiMainContent = aiPanelHtml.substring(startIndex, endIndex);
// Remove the <div class="ai-main"> wrapping tag itself
aiMainContent = aiMainContent.replace('<div class="ai-main">', '').trim();

// Remove the <header class="ai-main-hdr" ...>...</header> from the top
aiMainContent = aiMainContent.replace(/<header class="ai-main-hdr"[\s\S]*?<\/header>/, '');

let replacementHtml = '<div class="ai-assistant-inner-wrapper" style="flex:1; display:flex; flex-direction:column; min-height:0; overflow:hidden; background:#F8FAFC;">\n' + aiMainContent + '\n</div>';

// Replace <section class="chat-main-panel">...</section> in ai-assistant.html
// Need to find exactly where <section class="chat-main-panel"> starts and ends.
const sectionStart = aiAssistantHtml.indexOf('<section class="chat-main-panel">');
const sectionEnd = aiAssistantHtml.indexOf('</section>', sectionStart) + '</section>'.length;

if (sectionStart === -1 || sectionEnd === -1) {
    console.error('Could not find <section class="chat-main-panel"> in ai-assistant.html');
    process.exit(1);
}

aiAssistantHtml = aiAssistantHtml.substring(0, sectionStart) + replacementHtml + aiAssistantHtml.substring(sectionEnd);

fs.writeFileSync('src/app/Pages/ai-assistant/ai-assistant.html', aiAssistantHtml);

fs.appendFileSync('src/app/Pages/ai-assistant/ai-assistant.css', '\n\n/* --- AI PANEL CSS --- */\n' + aiPanelCss);
console.log('Successfully migrated UI components.');
