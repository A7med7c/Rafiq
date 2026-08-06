const fs = require('fs');
const path = require('path');

const aiPanelHtmlPath = 'src/app/Components/ai-panel/ai-panel.html';
const aiAssistantHtmlPath = 'src/app/Pages/ai-assistant/ai-assistant.html';
const aiPanelCssPath = 'src/app/Components/ai-panel/ai-panel.css';
const aiAssistantCssPath = 'src/app/Pages/ai-assistant/ai-assistant.css';

let aiPanelHtml = fs.readFileSync(aiPanelHtmlPath, 'utf8');
let aiAssistantHtml = fs.readFileSync(aiAssistantHtmlPath, 'utf8');

const mainStartStr = '<div class="ai-main">';
const mainEndStr = '</div><!-- /.ai-main -->';
let startIndex = aiPanelHtml.indexOf(mainStartStr);
let endIndex = aiPanelHtml.indexOf(mainEndStr);

let aiMainContent = aiPanelHtml.substring(startIndex + mainStartStr.length, endIndex).trim();

// The header inside ai-main is:
const headerRegex = /<header class="ai-main-hdr"[\s\S]*?<\/header>/;
aiMainContent = aiMainContent.replace(headerRegex, '');

// We want to replace <section class="chat-main-panel"> in aiAssistantHtml with this content
const sectionStartStr = '<section class="chat-main-panel">';
const sectionEndStr = '</section>';

let sectionStart = aiAssistantHtml.indexOf(sectionStartStr);
let sectionEnd = aiAssistantHtml.indexOf(sectionEndStr, sectionStart) + sectionEndStr.length;

let finalHtml = aiAssistantHtml.substring(0, sectionStart) + 
  '<section class="chat-main-panel">\n' + 
  '<div class="ai-assistant-inner" style="flex:1; display:flex; flex-direction:column; overflow:hidden; position:relative; background: #F8FAFC; height:100%;">\n' +
  aiMainContent + 
  '\n</div>\n' +
  '</section>' + 
  aiAssistantHtml.substring(sectionEnd);

fs.writeFileSync(aiAssistantHtmlPath, finalHtml);

// Append CSS
let aiPanelCss = fs.readFileSync(aiPanelCssPath, 'utf8');
fs.appendFileSync(aiAssistantCssPath, '\n\n/* AI PANEL STYLES PORTED */\n\n' + aiPanelCss);
