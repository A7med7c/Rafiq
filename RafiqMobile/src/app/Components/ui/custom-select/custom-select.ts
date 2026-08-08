import {
  Component,
  Input,
  Output,
  EventEmitter,
  forwardRef,
  ElementRef,
  HostListener,
  inject,
  OnDestroy,
  PLATFORM_ID,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LocalizationService } from '../../../Services/localization.service';

export interface SelectOption {
  label: string;
  value: any;
  icon?: string;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-select.html',
  styleUrl: './custom-select.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomSelectComponent),
      multi: true
    }
  ]
})
export class CustomSelectComponent implements ControlValueAccessor, OnDestroy {
  protected readonly l10n = inject(LocalizationService);
  private readonly elRef = inject(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);

  @Input() options: SelectOption[] = [];
  @Input() placeholder: string = '';
  @Input() disabled: boolean = false;
  @Input() invalid: boolean = false;

  @Output() valueChange = new EventEmitter<any>();

  selectedValue: any = null;
  isOpen: boolean = false;

  // Portal menu rendered on body
  private menuEl: HTMLElement | null = null;
  private scrollHandler: (() => void) | null = null;
  private resizeHandler: (() => void) | null = null;

  private onChange: (val: any) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(val: any): void {
    this.selectedValue = val;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  toggleDropdown(): void {
    if (this.disabled) return;
    if (this.isOpen) {
      this.closeMenu();
    } else {
      this.openMenu();
    }
  }

  openMenu(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.isOpen = true;
    this.createPortalMenu();
  }

  closeMenu(): void {
    this.isOpen = false;
    this.destroyPortalMenu();
    this.onTouched();
  }

  private createPortalMenu(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.destroyPortalMenu();

    const triggerEl = this.elRef.nativeElement.querySelector('.custom-select-trigger') as HTMLElement;
    if (!triggerEl) return;
    const rect = triggerEl.getBoundingClientRect();
    const isRtl = this.l10n.isRtl?.() ?? document.dir === 'rtl';

    this.menuEl = document.createElement('div');
    this.menuEl.className = 'custom-select-portal-menu';
    this.menuEl.style.cssText = `
      position: fixed;
      top: ${rect.bottom + 4}px;
      left: ${rect.left}px;
      width: ${rect.width}px;
      z-index: 9999999;
      background: #ffffff;
      border: 2px solid #0EAFD7;
      border-radius: 16px;
      box-shadow: 0 12px 32px rgba(14, 175, 215, 0.18);
      padding: 6px;
      max-height: 240px;
      overflow-y: auto;
      font-family: inherit;
      box-sizing: border-box;
      animation: selectMenuIn 0.18s cubic-bezier(0.16, 1, 0.3, 1) forwards;
    `;

    // Inject keyframes if not present
    if (!document.getElementById('custom-select-kf')) {
      const style = document.createElement('style');
      style.id = 'custom-select-kf';
      style.textContent = `
        @keyframes selectMenuIn {
          from { opacity: 0; transform: translateY(-6px); }
          to   { opacity: 1; transform: translateY(0); }
        }
        .custom-select-portal-menu .csp-item {
          display: flex; align-items: center; justify-content: space-between;
          padding: 12px 14px; border-radius: 12px; font-size: 0.95rem;
          font-weight: 400; color: #1e293b; cursor: pointer;
          transition: background 0.15s ease, color 0.15s ease;
          font-family: inherit;
        }
        .custom-select-portal-menu .csp-item:hover { background: #f0f9ff; color: #0EAFD7; }
        .custom-select-portal-menu .csp-item.selected { background: #0EAFD7; color: #fff; font-weight: 500; }
        .custom-select-portal-menu .csp-left { display: flex; align-items: center; gap: 10px; }
        .custom-select-portal-menu .csp-check { font-size: 0.9rem; flex-shrink: 0; margin-inline-start: 8px; }
      `;
      document.head.appendChild(style);
    }

    this.options.forEach(opt => {
      const item = document.createElement('div');
      item.className = 'csp-item' + (opt.value === this.selectedValue ? ' selected' : '');
      item.setAttribute('dir', isRtl ? 'rtl' : 'ltr');

      const left = document.createElement('div');
      left.className = 'csp-left';
      if (opt.icon) {
        const icon = document.createElement('i');
        icon.className = opt.icon + ' item-icon';
        left.appendChild(icon);
      }
      const text = document.createElement('span');
      text.textContent = opt.label;
      left.appendChild(text);
      item.appendChild(left);

      if (opt.value === this.selectedValue) {
        const check = document.createElement('i');
        check.className = 'fa-solid fa-check csp-check';
        item.appendChild(check);
      }

      item.addEventListener('click', (e) => {
        e.stopPropagation();
        this.selectedValue = opt.value;
        this.onChange(opt.value);
        this.valueChange.emit(opt.value);
        this.closeMenu();
      });

      this.menuEl!.appendChild(item);
    });

    document.body.appendChild(this.menuEl);

    // Reposition on scroll/resize
    this.scrollHandler = () => this.repositionMenu();
    this.resizeHandler = () => this.repositionMenu();
    window.addEventListener('scroll', this.scrollHandler, true);
    window.addEventListener('resize', this.resizeHandler);
  }

  private repositionMenu(): void {
    if (!this.menuEl) return;
    const triggerEl = this.elRef.nativeElement.querySelector('.custom-select-trigger') as HTMLElement;
    if (!triggerEl) return;
    const rect = triggerEl.getBoundingClientRect();
    this.menuEl.style.top = `${rect.bottom + 4}px`;
    this.menuEl.style.left = `${rect.left}px`;
    this.menuEl.style.width = `${rect.width}px`;
  }

  private destroyPortalMenu(): void {
    if (this.menuEl) {
      this.menuEl.remove();
      this.menuEl = null;
    }
    if (this.scrollHandler) {
      window.removeEventListener('scroll', this.scrollHandler, true);
      this.scrollHandler = null;
    }
    if (this.resizeHandler) {
      window.removeEventListener('resize', this.resizeHandler);
      this.resizeHandler = null;
    }
  }

  getSelectedLabel(): string {
    const found = this.options.find(o => o.value === this.selectedValue);
    return found ? found.label : '';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elRef.nativeElement.contains(event.target) &&
        !this.menuEl?.contains(event.target as Node)) {
      if (this.isOpen) {
        this.closeMenu();
      }
    }
  }

  ngOnDestroy(): void {
    this.destroyPortalMenu();
  }
}
