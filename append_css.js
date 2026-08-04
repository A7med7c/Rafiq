const fs = require('fs');
const css = `
/* === NEW REDESIGN STYLES === */
.fp-search-bar {
  display: flex;
  align-items: center;
  background-color: var(--white);
  border-radius: 12px;
  padding: 12px 16px;
  margin: 16px 0;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
}

.fp-search-bar .search-icon {
  color: var(--text-3);
  font-size: 16px;
  margin-inline-end: 12px;
}

.fp-search-bar input {
  flex: 1;
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
  color: var(--text);
  font-family: inherit;
}

.fp-search-bar .filter-btn-icon {
  background: none;
  border: none;
  color: var(--text-2);
  font-size: 18px;
  cursor: pointer;
  padding: 4px;
  margin-inline-start: 12px;
}

.fp-section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.fp-mt-24 {
  margin-top: 24px;
}

.fp-section-header h3 {
  font-size: 16px;
  font-weight: 700;
  color: var(--text);
  margin: 0;
}

.fp-section-header .fp-view-all {
  font-size: 13px;
  font-weight: 600;
  color: var(--primary);
  text-decoration: none;
  cursor: pointer;
}

.fp-types-scroll {
  display: flex;
  gap: 12px;
  overflow-x: auto;
  padding-bottom: 8px;
  margin-inline: -16px;
  padding-inline: 16px;
  scrollbar-width: none;
}

.fp-types-scroll::-webkit-scrollbar {
  display: none;
}

.fp-type-card {
  min-width: 80px;
  flex: 0 0 auto;
  border-radius: 16px;
  padding: 16px 12px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  box-shadow: 0 2px 4px rgba(0,0,0,0.02);
}

.fp-type-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  margin-bottom: 12px;
  background-color: var(--white);
}

.fp-bg-cyan { background-color: var(--cyan-lt); }
.fp-bg-cyan .fp-type-icon { color: var(--cyan); }
.fp-bg-orange { background-color: var(--orange-lt); }
.fp-bg-orange .fp-type-icon { color: var(--orange); }
.fp-bg-green { background-color: var(--green-lt); }
.fp-bg-green .fp-type-icon { color: var(--green); }
.fp-bg-purple { background-color: var(--purple-lt); }
.fp-bg-purple .fp-type-icon { color: var(--purple); }
.fp-bg-blue { background-color: var(--blue-lt); }
.fp-bg-blue .fp-type-icon { color: var(--blue); }

.fp-type-title {
  font-size: 13px;
  font-weight: 700;
  color: var(--text);
  margin-bottom: 4px;
}

.fp-type-count {
  font-size: 11px;
  color: var(--text-3);
}

.fp-records-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 100px;
}

.fp-record-item {
  display: flex;
  align-items: center;
  background: var(--white);
  border-radius: 16px;
  padding: 16px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  cursor: pointer;
  position: relative;
}

.fp-record-menu {
  padding: 8px;
  margin-inline-start: -8px;
  margin-inline-end: 12px;
  color: var(--text-2);
  cursor: pointer;
  position: relative;
}

.fp-record-info {
  flex: 1;
  text-align: start;
}

.fp-record-info h4 {
  font-size: 15px;
  font-weight: 700;
  color: var(--text);
  margin: 0 0 4px 0;
}

.fp-record-info p {
  font-size: 12px;
  color: var(--text-3);
  margin: 0 0 6px 0;
}

.fp-record-info span {
  font-size: 11px;
  color: var(--text-3);
}

.fp-record-file-icon {
  margin-inline-start: 12px;
}

.fp-file-badge {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 56px;
  border-radius: 8px;
  background-color: var(--blue-lt);
  color: var(--blue);
  border: 1px solid var(--border-lt);
}

.fp-file-badge i {
  font-size: 20px;
  margin-bottom: 4px;
}

.fp-file-badge span {
  font-size: 10px;
  font-weight: 700;
  background: var(--blue);
  color: var(--white);
  padding: 2px 6px;
  border-radius: 4px;
}

.fp-file-badge.is-pdf {
  background-color: #FFF0F0;
  color: #E53E3E;
  border-color: #FED7D7;
}

.fp-file-badge.is-pdf span {
  background-color: #E53E3E;
}

.fp-file-badge.is-jpg {
  background-color: #EBF8FF;
  color: #3182CE;
  border-color: #BEE3F8;
}

.fp-file-badge.is-jpg span {
  background-color: #3182CE;
}

.fp-file-badge.is-png {
  background-color: #F0FFF4;
  color: #38A169;
  border-color: #C6F6D5;
}

.fp-file-badge.is-png span {
  background-color: #38A169;
}


.fp-fab-btn {
  position: fixed;
  bottom: 80px;
  left: 50%;
  transform: translateX(-50%);
  background-color: var(--primary);
  color: var(--white);
  border: none;
  border-radius: 100px;
  padding: 16px 32px;
  font-size: 16px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 12px;
  box-shadow: 0 4px 12px rgba(13, 110, 253, 0.3);
  cursor: pointer;
  z-index: 100;
  width: calc(100% - 32px);
  justify-content: center;
  max-width: 400px;
}

.fp-fab-menu {
  position: fixed;
  bottom: 140px;
  left: 50%;
  transform: translateX(-50%);
  width: calc(100% - 32px);
  max-width: 400px;
  background: var(--white);
  border-radius: 16px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.15);
  z-index: 101;
}
`;
fs.appendFileSync('RafiqMobile/src/app/Pages/medical-records/medical-records.css', css);
console.log('CSS appended');
