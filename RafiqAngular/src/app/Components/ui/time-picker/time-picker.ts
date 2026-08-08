import {
  Component,
  Input,
  Output,
  EventEmitter,
  forwardRef,
  ElementRef,
  HostListener,
  inject,
  OnInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LocalizationService } from '../../../Services/localization.service';

@Component({
  selector: 'app-time-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './time-picker.html',
  styleUrl: './time-picker.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TimePickerComponent),
      multi: true
    }
  ]
})
export class TimePickerComponent implements ControlValueAccessor, OnInit {
  protected readonly l10n = inject(LocalizationService);
  private readonly elRef = inject(ElementRef);

  @Input() placeholder: string = '--:-- --';
  @Input() disabled: boolean = false;
  @Input() invalid: boolean = false;

  @Output() valueChange = new EventEmitter<string>();

  isOpen: boolean = false;
  value: string = ''; // 24h "HH:mm" or "HH:mm AM/PM"

  hoursList: string[] = Array.from({ length: 12 }, (_, i) => (i + 1).toString().padStart(2, '0'));
  minutesList: string[] = Array.from({ length: 60 }, (_, i) => i.toString().padStart(2, '0'));
  periodsList: string[] = ['AM', 'PM'];

  selectedHour: string = '04';
  selectedMinute: string = '00';
  selectedPeriod: string = 'PM';

  private onChange: (val: string) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    this.parseValue(this.value);
  }

  writeValue(val: string): void {
    this.value = val || '';
    this.parseValue(this.value);
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

  togglePopover(): void {
    if (this.disabled) return;
    this.isOpen = !this.isOpen;
    if (!this.isOpen) {
      this.onTouched();
    }
  }

  parseValue(val: string): void {
    if (!val) {
      const now = new Date();
      let h = now.getHours();
      const m = now.getMinutes();
      this.selectedPeriod = h >= 12 ? 'PM' : 'AM';
      h = h % 12 || 12;
      this.selectedHour = h.toString().padStart(2, '0');
      this.selectedMinute = m.toString().padStart(2, '0');
      return;
    }

    // Try parsing "HH:mm" or "hh:mm AM/PM"
    const match = val.match(/(\d{1,2}):(\d{2})\s*(AM|PM)?/i);
    if (match) {
      let h = parseInt(match[1], 10);
      const m = match[2];
      let p = match[3] ? match[3].toUpperCase() : null;

      if (!p) {
        p = h >= 12 ? 'PM' : 'AM';
        h = h % 12 || 12;
      }
      this.selectedHour = h.toString().padStart(2, '0');
      this.selectedMinute = m;
      this.selectedPeriod = p;
    }
  }

  getDisplayValue(): string {
    if (!this.value) return '';
    return `${this.selectedHour}:${this.selectedMinute} ${this.selectedPeriod}`;
  }

  getPrevHour(): string {
    const idx = this.hoursList.indexOf(this.selectedHour);
    const prevIdx = (idx - 1 + 12) % 12;
    return this.hoursList[prevIdx];
  }

  getNextHour(): string {
    const idx = this.hoursList.indexOf(this.selectedHour);
    const nextIdx = (idx + 1) % 12;
    return this.hoursList[nextIdx];
  }

  getPrevMinute(): string {
    const idx = this.minutesList.indexOf(this.selectedMinute);
    const prevIdx = (idx - 1 + 60) % 60;
    return this.minutesList[prevIdx];
  }

  getNextMinute(): string {
    const idx = this.minutesList.indexOf(this.selectedMinute);
    const nextIdx = (idx + 1) % 60;
    return this.minutesList[nextIdx];
  }

  getPrevPeriod(): string {
    return this.selectedPeriod === 'AM' ? 'PM' : 'AM';
  }

  getNextPeriod(): string {
    return this.selectedPeriod === 'AM' ? 'PM' : 'AM';
  }

  stepHour(delta: number): void {
    const idx = this.hoursList.indexOf(this.selectedHour);
    let newIdx = (idx + delta) % 12;
    if (newIdx < 0) newIdx += 12;
    this.selectedHour = this.hoursList[newIdx];
  }

  stepMinute(delta: number): void {
    const idx = this.minutesList.indexOf(this.selectedMinute);
    let newIdx = (idx + delta) % 60;
    if (newIdx < 0) newIdx += 60;
    this.selectedMinute = this.minutesList[newIdx];
  }

  togglePeriod(): void {
    this.selectedPeriod = this.selectedPeriod === 'AM' ? 'PM' : 'AM';
  }

  selectHour(h: string): void {
    this.selectedHour = h;
  }

  selectMinute(m: string): void {
    this.selectedMinute = m;
  }

  selectPeriod(p: string): void {
    this.selectedPeriod = p;
  }

  confirm(): void {
    // Format to HH:mm (24h) for backend & standard input, but display formatted 12h
    let hInt = parseInt(this.selectedHour, 10);
    if (this.selectedPeriod === 'PM' && hInt < 12) hInt += 12;
    if (this.selectedPeriod === 'AM' && hInt === 12) hInt = 0;
    const hh24 = hInt.toString().padStart(2, '0');
    const formatted = `${hh24}:${this.selectedMinute}`;

    this.value = formatted;
    this.onChange(formatted);
    this.valueChange.emit(formatted);
    this.isOpen = false;
    this.onTouched();
  }

  clear(): void {
    this.value = '';
    this.onChange('');
    this.valueChange.emit('');
    this.isOpen = false;
    this.onTouched();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elRef.nativeElement.contains(event.target)) {
      if (this.isOpen) {
        this.isOpen = false;
        this.onTouched();
      }
    }
  }
}
