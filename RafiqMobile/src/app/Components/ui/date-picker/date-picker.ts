import {
  Component, Input, Output, EventEmitter, forwardRef,
  ElementRef, HostListener, inject, OnInit, OnDestroy, PLATFORM_ID, ChangeDetectorRef
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LocalizationService } from '../../../Services/localization.service';

export interface MonthOption {
  value: number;
  nameAr: string;
  nameEn: string;
}

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './date-picker.html',
  styleUrl: './date-picker.css',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => DatePickerComponent),
    multi: true
  }]
})
export class DatePickerComponent implements ControlValueAccessor, OnInit, OnDestroy {
  protected readonly l10n = inject(LocalizationService);
  private readonly elRef = inject(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() placeholder: string = 'mm/dd/yyyy';
  @Input() disabled: boolean = false;
  @Input() invalid: boolean = false;
  @Input() minYear: number = 1940;
  @Input() maxYear: number = new Date().getFullYear() + 10;

  @Output() valueChange = new EventEmitter<string>();

  isOpen = false;
  value = '';

  monthsList: MonthOption[] = [
    { value: 1,  nameAr: 'يناير',   nameEn: 'January' },
    { value: 2,  nameAr: 'فبراير',  nameEn: 'February' },
    { value: 3,  nameAr: 'مارس',    nameEn: 'March' },
    { value: 4,  nameAr: 'أبريل',   nameEn: 'April' },
    { value: 5,  nameAr: 'مايو',    nameEn: 'May' },
    { value: 6,  nameAr: 'يونيو',   nameEn: 'June' },
    { value: 7,  nameAr: 'يوليو',   nameEn: 'July' },
    { value: 8,  nameAr: 'أغسطس',   nameEn: 'August' },
    { value: 9,  nameAr: 'سبتمبر',  nameEn: 'September' },
    { value: 10, nameAr: 'أكتوبر',  nameEn: 'October' },
    { value: 11, nameAr: 'نوفمبر',  nameEn: 'November' },
    { value: 12, nameAr: 'ديسمبر',  nameEn: 'December' },
  ];

  selectedDay   = 1;
  selectedMonth = 1;
  selectedYear  = new Date().getFullYear();

  // DOM portal references
  private pickerEl: HTMLElement | null = null;
  private scrollHandler: (() => void) | null = null;
  private resizeHandler: (() => void) | null = null;

  // Wheel text nodes for live updates
  private domNodes: {
    prevYear?: HTMLElement; curYear?: HTMLElement; nextYear?: HTMLElement;
    prevMonth?: HTMLElement; curMonth?: HTMLElement; nextMonth?: HTMLElement;
    prevDay?: HTMLElement; curDay?: HTMLElement; nextDay?: HTMLElement;
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
    const d = this.selectedDay.toString().padStart(2, '0');
    const m = this.selectedMonth.toString().padStart(2, '0');
    return `${m}/${d}/${this.selectedYear}`;
  }

  private parseValue(val: string) {
    if (!val) {
      const now = new Date();
      this.selectedYear = now.getFullYear();
      this.selectedMonth = now.getMonth() + 1;
      this.selectedDay = now.getDate();
      return;
    }
    const match = val.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    if (match) {
      this.selectedYear  = parseInt(match[1], 10);
      this.selectedMonth = parseInt(match[2], 10);
      this.selectedDay   = parseInt(match[3], 10);
    }
  }

  private monthName(m: number): string {
    const opt = this.monthsList.find(x => x.value === m);
    if (!opt) return '';
    return this.l10n.isRtl() ? opt.nameAr : opt.nameEn;
  }

  private daysInMonth(y: number, m: number) { return new Date(y, m, 0).getDate(); }

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

  // ── Portal build ──────────────────────────────────────────────────────────

  private buildPortal() {
    this.destroyPortal();

    const trigger = this.elRef.nativeElement.querySelector('.date-picker-trigger') as HTMLElement;
    if (!trigger) return;

    const rect = trigger.getBoundingClientRect();
    const isRtl = this.l10n.isRtl();
    const portalWidth = Math.max(rect.width, 270);
    let left = isRtl ? rect.right - portalWidth : rect.left;
    if (typeof window !== 'undefined') {
      left = Math.max(10, Math.min(left, window.innerWidth - portalWidth - 10));
    }

    // Inject shared styles once
    if (!document.getElementById('dp-portal-styles')) {
      const s = document.createElement('style');
      s.id = 'dp-portal-styles';
      s.textContent = `
        .dp-portal {
          position: fixed;
          z-index: 9999999;
          background: #fff;
          border: 1.5px solid #0EAFD7;
          border-radius: 14px;
          box-shadow: 0 8px 24px rgba(14,175,215,0.18);
          padding: 8px 10px;
          box-sizing: border-box;
          font-family: inherit;
          min-width: 270px;
          animation: dpIn 0.18s cubic-bezier(0.16,1,0.3,1) both;
        }
        @keyframes dpIn { from { opacity:0; transform:translateY(-4px); } to { opacity:1; transform:none; } }
        .dp-portal .dp-header {
          display:flex; justify-content:space-around;
          padding:2px 0 4px; font-size:0.78rem; font-weight:700;
          color:#64748b; border-bottom:1px solid #f1f5f9; margin-bottom:2px;
        }
        .dp-portal .dp-hdr { flex:1; text-align:center; }
        .dp-portal .dp-wheels {
          display:flex; align-items:center; justify-content:space-between;
          height:136px; position:relative; padding:4px 0;
        }
        .dp-portal .dp-sel-bar {
          position:absolute; top:50%; left:4px; right:4px;
          transform:translateY(-50%); height:34px;
          background:rgba(14,175,215,0.08); border-radius:8px;
          pointer-events:none; z-index:1;
        }
        .dp-portal .dp-col {
          flex:1; display:flex; flex-direction:column;
          align-items:center; justify-content:space-between;
          height:100%; position:relative; z-index:2; padding:0 4px;
        }
        .dp-portal .dp-arrow {
          background:none; border:none; color:#0EAFD7;
          font-size:0.75rem; padding:2px 4px; cursor:pointer;
          transition:transform 0.15s;
        }
        .dp-portal .dp-arrow:hover { transform:scale(1.15); }
        .dp-portal .dp-list {
          display:flex; flex-direction:column; align-items:center;
          justify-content:space-around; height:88px; width:100%;
        }
        .dp-portal .dp-item {
          font-size:0.82rem; font-weight:400; color:#64748b;
          cursor:pointer; white-space:nowrap; text-overflow:ellipsis;
          overflow:hidden; max-width:100%; text-align:center;
          transition:color 0.15s,opacity 0.15s; user-select:none;
        }
        .dp-portal .dp-item-subtle { opacity:0.45; font-size:0.78rem; }
        .dp-portal .dp-item-subtle:hover { opacity:0.8; color:#0EAFD7; }
        .dp-portal .dp-item-active { color:#0EAFD7; font-weight:500; font-size:0.92rem; opacity:1; }
        .dp-portal .dp-divider { width:1px; height:70%; background:#f1f5f9; }
        .dp-portal .dp-footer {
          display:flex; align-items:center; justify-content:space-between;
          padding-top:6px; border-top:1px solid #f1f5f9; margin-top:4px;
        }
        .dp-portal .dp-btn {
          background:none; border:none; color:#0EAFD7;
          font-weight:600; font-size:0.82rem; cursor:pointer;
          padding:4px 8px; font-family:inherit;
        }
        .dp-portal .dp-btn-confirm { font-weight:700; }
        .dp-portal .dp-btn:hover { opacity:0.8; }
      `;
      document.head.appendChild(s);
    }

    this.pickerEl = document.createElement('div');
    this.pickerEl.className = 'dp-portal';
    this.pickerEl.style.cssText = `
      top: ${rect.bottom + 4}px;
      left: ${left}px;
      right: auto;
      width: ${portalWidth}px;
      min-width: 270px;
      direction: ${isRtl ? 'rtl' : 'ltr'};
    `;

    // Header
    const header = document.createElement('div');
    header.className = 'dp-header';
    const labels = isRtl
      ? ['السنة', 'الشهر', 'اليوم']
      : ['Year', 'Month', 'Day'];
    labels.forEach(l => {
      const h = document.createElement('span');
      h.className = 'dp-hdr';
      h.textContent = l;
      header.appendChild(h);
    });
    this.pickerEl.appendChild(header);

    // Wheels container
    const wheels = document.createElement('div');
    wheels.className = 'dp-wheels';
    const bar = document.createElement('div');
    bar.className = 'dp-sel-bar';
    wheels.appendChild(bar);

    // Build each column
    const yearCol  = this.buildCol('year');
    const divider1 = document.createElement('div');
    divider1.className = 'dp-divider';
    const monthCol = this.buildCol('month');
    const divider2 = document.createElement('div');
    divider2.className = 'dp-divider';
    const dayCol   = this.buildCol('day');

    wheels.appendChild(yearCol);
    wheels.appendChild(divider1);
    wheels.appendChild(monthCol);
    wheels.appendChild(divider2);
    wheels.appendChild(dayCol);
    this.pickerEl.appendChild(wheels);

    // Footer
    const footer = document.createElement('div');
    footer.className = 'dp-footer';

    const confirmBtn = document.createElement('button');
    confirmBtn.type = 'button';
    confirmBtn.className = 'dp-btn dp-btn-confirm';
    confirmBtn.textContent = isRtl ? 'تأكيد' : 'Confirm';
    confirmBtn.addEventListener('click', (e) => { e.stopPropagation(); this.confirm(); });

    const clearBtn = document.createElement('button');
    clearBtn.type = 'button';
    clearBtn.className = 'dp-btn';
    clearBtn.textContent = isRtl ? 'مسح' : 'Clear';
    clearBtn.addEventListener('click', (e) => { e.stopPropagation(); this.clear(); });

    // RTL: confirm left, clear right; LTR: clear left, confirm right
    if (isRtl) {
      footer.appendChild(confirmBtn);
      footer.appendChild(clearBtn);
    } else {
      footer.appendChild(clearBtn);
      footer.appendChild(confirmBtn);
    }
    this.pickerEl.appendChild(footer);

    document.body.appendChild(this.pickerEl);
    this.updateWheelText();

    this.scrollHandler = () => this.repositionPortal();
    this.resizeHandler = () => this.repositionPortal();
    window.addEventListener('scroll', this.scrollHandler, true);
    window.addEventListener('resize', this.resizeHandler);
  }

  private buildCol(type: 'year' | 'month' | 'day'): HTMLElement {
    const col = document.createElement('div');
    col.className = 'dp-col';

    const upBtn = document.createElement('button');
    upBtn.type = 'button';
    upBtn.className = 'dp-arrow';
    upBtn.innerHTML = '<i class="fa-solid fa-chevron-up"></i>';
    upBtn.addEventListener('click', (e) => { e.stopPropagation(); this.step(type, 1); });
    col.appendChild(upBtn);

    const list = document.createElement('div');
    list.className = 'dp-list';

    const prev = document.createElement('div');
    prev.className = 'dp-item dp-item-subtle';
    prev.addEventListener('click', (e) => { e.stopPropagation(); this.step(type, 1); });

    const cur = document.createElement('div');
    cur.className = 'dp-item dp-item-active';

    const next = document.createElement('div');
    next.className = 'dp-item dp-item-subtle';
    next.addEventListener('click', (e) => { e.stopPropagation(); this.step(type, -1); });

    list.appendChild(prev);
    list.appendChild(cur);
    list.appendChild(next);
    col.appendChild(list);

    const downBtn = document.createElement('button');
    downBtn.type = 'button';
    downBtn.className = 'dp-arrow';
    downBtn.innerHTML = '<i class="fa-solid fa-chevron-down"></i>';
    downBtn.addEventListener('click', (e) => { e.stopPropagation(); this.step(type, -1); });
    col.appendChild(downBtn);

    if (type === 'year') {
      this.domNodes.prevYear = prev;
      this.domNodes.curYear  = cur;
      this.domNodes.nextYear = next;
    } else if (type === 'month') {
      this.domNodes.prevMonth = prev;
      this.domNodes.curMonth  = cur;
      this.domNodes.nextMonth = next;
    } else {
      this.domNodes.prevDay = prev;
      this.domNodes.curDay  = cur;
      this.domNodes.nextDay = next;
    }

    return col;
  }

  private step(type: 'year' | 'month' | 'day', delta: number) {
    if (type === 'year') {
      let y = this.selectedYear + delta;
      if (y < this.minYear) y = this.maxYear;
      if (y > this.maxYear) y = this.minYear;
      this.selectedYear = y;
      const max = this.daysInMonth(this.selectedYear, this.selectedMonth);
      if (this.selectedDay > max) this.selectedDay = max;
    } else if (type === 'month') {
      let m = this.selectedMonth + delta;
      if (m < 1) m = 12;
      if (m > 12) m = 1;
      this.selectedMonth = m;
      const max = this.daysInMonth(this.selectedYear, this.selectedMonth);
      if (this.selectedDay > max) this.selectedDay = max;
    } else {
      const max = this.daysInMonth(this.selectedYear, this.selectedMonth);
      let d = this.selectedDay + delta;
      if (d < 1) d = max;
      if (d > max) d = 1;
      this.selectedDay = d;
    }
    this.updateWheelText();
  }

  private updateWheelText() {
    if (!this.pickerEl) return;

    const maxDays = this.daysInMonth(this.selectedYear, this.selectedMonth);

    // Year
    const prevY = this.selectedYear + 1 <= this.maxYear ? this.selectedYear + 1 : this.minYear;
    const nextY = this.selectedYear - 1 >= this.minYear ? this.selectedYear - 1 : this.maxYear;
    if (this.domNodes.prevYear) this.domNodes.prevYear.textContent = String(prevY);
    if (this.domNodes.curYear)  this.domNodes.curYear.textContent  = String(this.selectedYear);
    if (this.domNodes.nextYear) this.domNodes.nextYear.textContent = String(nextY);

    // Month
    const prevM = this.selectedMonth > 1 ? this.selectedMonth - 1 : 12;
    const nextM = this.selectedMonth < 12 ? this.selectedMonth + 1 : 1;
    if (this.domNodes.prevMonth) this.domNodes.prevMonth.textContent = this.monthName(prevM);
    if (this.domNodes.curMonth)  this.domNodes.curMonth.textContent  = this.monthName(this.selectedMonth);
    if (this.domNodes.nextMonth) this.domNodes.nextMonth.textContent = this.monthName(nextM);

    // Day
    const prevD = this.selectedDay > 1 ? this.selectedDay - 1 : maxDays;
    const nextD = this.selectedDay < maxDays ? this.selectedDay + 1 : 1;
    if (this.domNodes.prevDay) this.domNodes.prevDay.textContent = String(prevD).padStart(2, '0');
    if (this.domNodes.curDay)  this.domNodes.curDay.textContent  = String(this.selectedDay).padStart(2, '0');
    if (this.domNodes.nextDay) this.domNodes.nextDay.textContent = String(nextD).padStart(2, '0');
  }

  private repositionPortal() {
    if (!this.pickerEl) return;
    const trigger = this.elRef.nativeElement.querySelector('.date-picker-trigger') as HTMLElement;
    if (!trigger) return;
    const rect = trigger.getBoundingClientRect();
    const isRtl = this.l10n.isRtl();
    const portalWidth = Math.max(rect.width, 270);
    let left = isRtl ? rect.right - portalWidth : rect.left;
    if (typeof window !== 'undefined') {
      left = Math.max(10, Math.min(left, window.innerWidth - portalWidth - 10));
    }

    this.pickerEl.style.top   = `${rect.bottom + 4}px`;
    this.pickerEl.style.left  = `${left}px`;
    this.pickerEl.style.width = `${portalWidth}px`;
  }

  private destroyPortal() {
    if (this.pickerEl) { this.pickerEl.remove(); this.pickerEl = null; }
    this.domNodes = {};
    if (this.scrollHandler) { window.removeEventListener('scroll', this.scrollHandler, true); this.scrollHandler = null; }
    if (this.resizeHandler) { window.removeEventListener('resize', this.resizeHandler); this.resizeHandler = null; }
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  confirm() {
    const y = this.selectedYear;
    const m = this.selectedMonth.toString().padStart(2, '0');
    const d = this.selectedDay.toString().padStart(2, '0');
    const formatted = `${y}-${m}-${d}`;
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
