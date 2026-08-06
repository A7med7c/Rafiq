const fs = require('fs');
const css = `
.dash-avatar-dropdown {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 220px;
  background: var(--white);
  border: 1px solid var(--border);
  border-radius: var(--r);
  box-shadow: 0 10px 30px rgba(0, 0, 0, .12);
  display: flex;
  flex-direction: column;
  padding: 8px 0;
  z-index: 100;
  animation: fadeInDown .2s ease-out;
}

[dir="rtl"] .dash-avatar-dropdown {
  right: auto;
  left: 0;
}

.dash-dropdown-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 20px;
  background: transparent;
  border: none;
  width: 100%;
  font-family: inherit;
  font-size: 14px;
  font-weight: 600;
  color: var(--text);
  cursor: pointer;
  text-decoration: none;
  transition: background .2s, color .2s;
}

.dash-dropdown-item:hover {
  background: var(--bg);
}

.dash-dropdown-item i {
  font-size: 16px;
  color: var(--text-3);
  transition: color .2s;
}

.dash-dropdown-item:hover i {
  color: var(--blue);
}

.dash-dropdown-item--danger:hover {
  background: var(--red-lt);
  color: var(--red);
}

.dash-dropdown-item--danger:hover i {
  color: var(--red);
}

.dash-dropdown-div {
  height: 1px;
  background: var(--border-lt);
  margin: 6px 0;
}

@keyframes fadeInDown {
  from { opacity: 0; transform: translateY(-8px); }
  to { opacity: 1; transform: translateY(0); }
}
`;
fs.appendFileSync('src/app/Pages/dashboard/dashboard.css', css, 'utf8');
