import {
  Component, Input, Output, EventEmitter, forwardRef,
  ElementRef, HostListener, inject, OnInit, OnDestroy, PLATFORM_ID, ChangeDetectorRef
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LocalizationService } from '../../../Services/localization.service';

@Component({
  selector: 'app-time-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './time-picker.html',
  styleUrl: './time-picker.css',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => TimePickerComponent),
    multi: true
  }]
})
export class TimePickerComponent implements ControlValueAccessor, OnInit, OnDestroy {
  protected readonly l10n = inject(LocalizationService);
  private readonly elRef = inject(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() placeholder = '--:-- --';
  @Input() disabled = false;
  @Input() invalid = false;

  @Output() valueChange = new EventEmitter<string>();

  isOpen = false;
  value = '';

  private hoursList   = Array.from({ length: 12 }, (_, i) => (i + 1).toString().padStart(2, '0'));
  private minutesList = Array.from({ length: 60 }, (_, i) => i.toString().padStart(2, '0'));

  selectedHour   = '04';
  selectedMinute = '00';
  selectedPeriod = 'PM';

  // DOM portal
  private pickerEl: HTMLElement | null = null;
  private scrollHandler: (() => void) | null = null;
  private resizeHandler: (() => void) | null = null;

  private domNodes: {
    prevHour?: HTMLElement; curHour?: HTMLElement; nextHour?: HTMLElement;
    prevMin?: HTMLElement;  curMin?: HTMLElement;  nextMin?: HTMLElement;
    prevPer?: HTMLElement;  curPer?: HTMLElement;  nextPer?: HTMLElement;
  } = {};

  private onChange: (v: string) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit() { this.parseValue(this.value); }

  writeValue(val: string) { this.value = val || ''; this.parseValue(this.value); }
  registerOnChange(fn: any) { this.onChange = fn; }
  registerOnTouched(fn: any) { this.onTouched = fn; }
  setDisabledState(d: boolean) { this.disabled = d; }

  getDisplayValue(): string {
    if (!this.value) return '';
    return `${this.selectedHour}:${this.selectedMinute} ${this.selectedPeriod}`;
  }

  private parseValue(val: string) {
    if (!val) {
      const now = new Date();
      let h = now.getHours();
      const m = now.getMinutes();
      this.selectedPeriod = h >= 12 ? 'PM' : 'AM';
      h = h % 12 || 12;
      this.selectedHour   = h.toString().padStart(2, '0');
      this.selectedMinute = m.toString().padStart(2, '0');
      return;
    }
    const match = val.match(/(\d{1,2}):(\d{2})\s*(AM|PM)?/i);
    if (match) {
      let h = parseInt(match[1], 10);
      const min = match[2];
      let p = match[3] ? match[3].toUpperCase() : null;
      if (!p) { p = h >= 12 ? 'PM' : 'AM'; h = h % 12 || 12; }
      this.selectedHour   = h.toString().padStart(2, '0');
      this.selectedMinute = min;
      this.selectedPeriod = p;
    }
  }

  togglePopover() {
    if (this.disabled) return;
    this.isOpen ? this.closePopover() : this.openPopover();
  }

  openPopover() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.isOpen = true;
    this.cdr.markForCheck();
    this.buildPortal();
  }

  closePopover() {
    this.isOpen = false;
    this.cdr.markForCheck();
    this.destroyPortal();
    this.onTouched();
  }

  private buildPortal() {
    this.destroyPortal();

    const trigger = this.elRef.nativeElement.querySelector('.time-picker-trigger') as HTMLElement;
    if (!trigger) return;

    const rect = trigger.getBoundingClientRect();
    const isRtl = this.l10n.isRtl();

    // Inject shared styles once (reuse dp-portal-styles if already present)
    if (!document.getElementById('tp-portal-styles')) {
      const s = document.createElement('style');
      s.id = 'tp-portal-styles';
      s.textContent = `
        .tp-portal {
          position: fixed;
          z-index: 9999999;
          background: #fff;
          border: 1.5px solid #0EAFD7;
          border-radius: 14px;
          box-shadow: 0 8px 24px rgba(14,175,215,0.18);
          padding: 8px 10px;
          box-sizing: border-box;
          font-family: inherit;
          animation: tpIn 0.18s cubic-bezier(0.16,1,0.3,1) both;
        }
        @keyframes tpIn { from { opacity:0; transform:translateY(-4px); } to { opacity:1; transform:none; } }
        .tp-portal .tp-wheels {
          display:flex; align-items:center; justify-content:space-between;
          height:136px; position:relative; padding:4px 0;
        }
        .tp-portal .tp-sel-bar {
          position:absolute; top:50%; left:4px; right:4px;
          transform:translateY(-50%); height:34px;
          background:rgba(14,175,215,0.08); border-radius:8px;
          pointer-events:none; z-index:1;
        }
        .tp-portal .tp-col {
          flex:1; display:flex; flex-direction:column;
          align-items:center; justify-content:space-between;
          height:100%; position:relative; z-index:2; padding:0 4px;
        }
        .tp-portal .tp-arrow {
          background:none; border:none; color:#0EAFD7;
          font-size:0.75rem; padding:2px 4px; cursor:pointer;
          transition:transform 0.15s; font-family:inherit;
        }
        .tp-portal .tp-arrow:hover { transform:scale(1.15); }
        .tp-portal .tp-list {
          display:flex; flex-direction:column; align-items:center;
          justify-content:space-around; height:88px; width:100%;
        }
        .tp-portal .tp-item {
          font-size:0.82rem; font-weight:400; color:#64748b;
          cursor:pointer; white-space:nowrap; text-align:center;
          transition:color 0.15s,opacity 0.15s; user-select:none;
        }
        .tp-portal .tp-item-subtle { opacity:0.45; font-size:0.78rem; }
        .tp-portal .tp-item-subtle:hover { opacity:0.8; color:#0EAFD7; }
        .tp-portal .tp-item-active { color:#0EAFD7; font-weight:500; font-size:0.92rem; opacity:1; }
        .tp-portal .tp-divider { width:1px; height:70%; background:#f1f5f9; }
        .tp-portal .tp-footer {
          display:flex; align-items:center; justify-content:space-between;
          padding-top:6px; border-top:1px solid #f1f5f9; margin-top:4px;
        }
        .tp-portal .tp-btn {
          background:none; border:none; color:#0EAFD7;
          font-weight:600; font-size:0.82rem; cursor:pointer;
          padding:4px 8px; font-family:inherit;
        }
        .tp-portal .tp-btn-confirm { font-weight:700; }
        .tp-portal .tp-btn:hover { opacity:0.8; }
      `;
      document.head.appendChild(s);
    }

    this.pickerEl = document.createElement('div');
    this.pickerEl.className = 'tp-portal';
    this.pickerEl.style.cssText = `
      top: ${rect.bottom + 4}px;
      left: ${rect.left}px;
      right: auto;
      width: ${rect.width}px;
      direction: ${isRtl ? 'rtl' : 'ltr'};
    `;

    const wheels = document.createElement('div');
    wheels.className = 'tp-wheels';
    const bar = document.createElement('div');
    bar.className = 'tp-sel-bar';
    wheels.appendChild(bar);

    wheels.appendChild(this.buildCol('hour'));
    const d1 = document.createElement('div'); d1.className = 'tp-divider'; wheels.appendChild(d1);
    wheels.appendChild(this.buildCol('min'));
    const d2 = document.createElement('div'); d2.className = 'tp-divider'; wheels.appendChild(d2);
    wheels.appendChild(this.buildCol('period'));

    this.pickerEl.appendChild(wheels);

    const footer = document.createElement('div');
    footer.className = 'tp-footer';

    const confirmBtn = document.createElement('button');
    confirmBtn.type = 'button';
    confirmBtn.className = 'tp-btn tp-btn-confirm';
    confirmBtn.textContent = isRtl ? 'تم' : 'Done';
    confirmBtn.addEventListener('click', (e) => { e.stopPropagation(); this.confirm(); });

    const clearBtn = document.createElement('button');
    clearBtn.type = 'button';
    clearBtn.className = 'tp-btn';
    clearBtn.textContent = isRtl ? 'مسح' : 'Clear';
    clearBtn.addEventListener('click', (e) => { e.stopPropagation(); this.clear(); });

    if (isRtl) { footer.appendChild(confirmBtn); footer.appendChild(clearBtn); }
    else        { footer.appendChild(clearBtn);   footer.appendChild(confirmBtn); }
    this.pickerEl.appendChild(footer);

    document.body.appendChild(this.pickerEl);
    this.updateWheelText();

    this.scrollHandler = () => this.repositionPortal();
    this.resizeHandler = () => this.repositionPortal();
    window.addEventListener('scroll', this.scrollHandler, true);
    window.addEventListener('resize', this.resizeHandler);
  }

  private buildCol(type: 'hour' | 'min' | 'period'): HTMLElement {
    const col = document.createElement('div');
    col.className = 'tp-col';

    const upBtn = document.createElement('button');
    upBtn.type = 'button'; upBtn.className = 'tp-arrow';
    upBtn.innerHTML = '<i class="fa-solid fa-chevron-up"></i>';
    upBtn.addEventListener('click', (e) => { e.stopPropagation(); this.stepType(type, 1); });
    col.appendChild(upBtn);

    const list = document.createElement('div'); list.className = 'tp-list';
    const prev = document.createElement('div'); prev.className = 'tp-item tp-item-subtle';
    prev.addEventListener('click', (e) => { e.stopPropagation(); this.stepType(type, 1); });
    const cur  = document.createElement('div'); cur.className  = 'tp-item tp-item-active';
    const next = document.createElement('div'); next.className = 'tp-item tp-item-subtle';
    next.addEventListener('click', (e) => { e.stopPropagation(); this.stepType(type, -1); });
    list.appendChild(prev); list.appendChild(cur); list.appendChild(next);
    col.appendChild(list);

    const downBtn = document.createElement('button');
    downBtn.type = 'button'; downBtn.className = 'tp-arrow';
    downBtn.innerHTML = '<i class="fa-solid fa-chevron-down"></i>';
    downBtn.addEventListener('click', (e) => { e.stopPropagation(); this.stepType(type, -1); });
    col.appendChild(downBtn);

    if (type === 'hour')   { this.domNodes.prevHour = prev; this.domNodes.curHour = cur; this.domNodes.nextHour = next; }
    if (type === 'min')    { this.domNodes.prevMin  = prev; this.domNodes.curMin  = cur; this.domNodes.nextMin  = next; }
    if (type === 'period') { this.domNodes.prevPer  = prev; this.domNodes.curPer  = cur; this.domNodes.nextPer  = next; }

    return col;
  }

  private stepType(type: 'hour' | 'min' | 'period', delta: number) {
    if (type === 'hour') {
      const idx = this.hoursList.indexOf(this.selectedHour);
      let newIdx = (idx - delta + 12) % 12;
      this.selectedHour = this.hoursList[newIdx];
    } else if (type === 'min') {
      const idx = this.minutesList.indexOf(this.selectedMinute);
      let newIdx = (idx - delta + 60) % 60;
      this.selectedMinute = this.minutesList[newIdx];
    } else {
      this.selectedPeriod = this.selectedPeriod === 'AM' ? 'PM' : 'AM';
    }
    this.updateWheelText();
  }

  private updateWheelText() {
    if (!this.pickerEl) return;

    const hIdx = this.hoursList.indexOf(this.selectedHour);
    const mIdx = this.minutesList.indexOf(this.selectedMinute);

    if (this.domNodes.prevHour) this.domNodes.prevHour.textContent = this.hoursList[(hIdx - 1 + 12) % 12];
    if (this.domNodes.curHour)  this.domNodes.curHour.textContent  = this.selectedHour;
    if (this.domNodes.nextHour) this.domNodes.nextHour.textContent = this.hoursList[(hIdx + 1) % 12];

    if (this.domNodes.prevMin) this.domNodes.prevMin.textContent = this.minutesList[(mIdx - 1 + 60) % 60];
    if (this.domNodes.curMin)  this.domNodes.curMin.textContent  = this.selectedMinute;
    if (this.domNodes.nextMin) this.domNodes.nextMin.textContent = this.minutesList[(mIdx + 1) % 60];

    const otherPer = this.selectedPeriod === 'AM' ? 'PM' : 'AM';
    if (this.domNodes.prevPer) this.domNodes.prevPer.textContent = otherPer;
    if (this.domNodes.curPer)  this.domNodes.curPer.textContent  = this.selectedPeriod;
    if (this.domNodes.nextPer) this.domNodes.nextPer.textContent = otherPer;
  }

  private repositionPortal() {
    if (!this.pickerEl) return;
    const trigger = this.elRef.nativeElement.querySelector('.time-picker-trigger') as HTMLElement;
    if (!trigger) return;
    const rect = trigger.getBoundingClientRect();
    this.pickerEl.style.top   = `${rect.bottom + 4}px`;
    this.pickerEl.style.left  = `${rect.left}px`;
    this.pickerEl.style.width = `${rect.width}px`;
  }

  private destroyPortal() {
    if (this.pickerEl) { this.pickerEl.remove(); this.pickerEl = null; }
    this.domNodes = {};
    if (this.scrollHandler) { window.removeEventListener('scroll', this.scrollHandler, true); this.scrollHandler = null; }
    if (this.resizeHandler) { window.removeEventListener('resize', this.resizeHandler); this.resizeHandler = null; }
  }

  confirm() {
    let h = parseInt(this.selectedHour, 10);
    if (this.selectedPeriod === 'PM' && h < 12) h += 12;
    if (this.selectedPeriod === 'AM' && h === 12) h = 0;
    const formatted = `${h.toString().padStart(2, '0')}:${this.selectedMinute}`;
    this.value = formatted;
    this.onChange(formatted);
    this.valueChange.emit(formatted);
    this.cdr.markForCheck();
    this.closePopover();
  }

  clear() {
    this.value = '';
    this.onChange('');
    this.valueChange.emit('');
    this.cdr.markForCheck();
    this.closePopover();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.elRef.nativeElement.contains(event.target) &&
        !this.pickerEl?.contains(event.target as Node)) {
      if (this.isOpen) this.closePopover();
    }
  }

  ngOnDestroy() { this.destroyPortal(); }
}
