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

export interface MonthOption {
  value: number; // 1-12
  nameAr: string;
  nameEn: string;
}

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './date-picker.html',
  styleUrl: './date-picker.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DatePickerComponent),
      multi: true
    }
  ]
})
export class DatePickerComponent implements ControlValueAccessor, OnInit {
  protected readonly l10n = inject(LocalizationService);
  private readonly elRef = inject(ElementRef);

  @Input() placeholder: string = 'mm/dd/yyyy';
  @Input() disabled: boolean = false;
  @Input() invalid: boolean = false;
  @Input() minYear: number = 1940;
  @Input() maxYear: number = new Date().getFullYear() + 10;

  @Output() valueChange = new EventEmitter<string>();

  isOpen: boolean = false;
  value: string = '';

  yearsList: number[] = [];
  monthsList: MonthOption[] = [
    { value: 1,  nameAr: 'يناير',   nameEn: 'January' },
    { value: 2,  nameAr: 'فبراير',  nameEn: 'February' },
    { value: 3,  nameAr: 'مارس',   nameEn: 'March' },
    { value: 4,  nameAr: 'أبريل',   nameEn: 'April' },
    { value: 5,  nameAr: 'مايو',   nameEn: 'May' },
    { value: 6,  nameAr: 'يونيو',   nameEn: 'June' },
    { value: 7,  nameAr: 'يوليو',   nameEn: 'July' },
    { value: 8,  nameAr: 'أغسطس',  nameEn: 'August' },
    { value: 9,  nameAr: 'سبتمبر',  nameEn: 'September' },
    { value: 10, nameAr: 'أكتوبر',  nameEn: 'October' },
    { value: 11, nameAr: 'نوفمبر', nameEn: 'November' },
    { value: 12, nameAr: 'ديسمبر', nameEn: 'December' }
  ];

  daysList: number[] = Array.from({ length: 31 }, (_, i) => i + 1);

  selectedDay: number = 1;
  selectedMonth: number = 1;
  selectedYear: number = new Date().getFullYear();

  private onChange: (val: string) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    this.initYears();
    this.parseValue(this.value);
  }

  initYears(): void {
    const years: number[] = [];
    for (let y = this.maxYear; y >= this.minYear; y--) {
      years.push(y);
    }
    this.yearsList = years;
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
      this.selectedYear = now.getFullYear();
      this.selectedMonth = now.getMonth() + 1;
      this.selectedDay = now.getDate();
      return;
    }

    const match = val.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    if (match) {
      this.selectedYear = parseInt(match[1], 10);
      this.selectedMonth = parseInt(match[2], 10);
      this.selectedDay = parseInt(match[3], 10);
    }
  }

  getDisplayValue(): string {
    if (!this.value) return '';
    const d = this.selectedDay.toString().padStart(2, '0');
    const m = this.selectedMonth.toString().padStart(2, '0');
    return `${m}/${d}/${this.selectedYear}`;
  }

  getMonthName(m: MonthOption): string {
    return this.l10n.isRtl() ? m.nameAr : m.nameEn;
  }

  getDaysInMonth(year: number, month: number): number {
    return new Date(year, month, 0).getDate();
  }

  getPrevYear(): number {
    return this.selectedYear + 1 <= this.maxYear ? this.selectedYear + 1 : this.minYear;
  }

  getNextYear(): number {
    return this.selectedYear - 1 >= this.minYear ? this.selectedYear - 1 : this.maxYear;
  }

  getPrevMonthName(): string {
    const prevM = this.selectedMonth > 1 ? this.selectedMonth - 1 : 12;
    const opt = this.monthsList.find(m => m.value === prevM);
    return opt ? this.getMonthName(opt) : '';
  }

  getCurrentMonthName(): string {
    const opt = this.monthsList.find(m => m.value === this.selectedMonth);
    return opt ? this.getMonthName(opt) : '';
  }

  getNextMonthName(): string {
    const nextM = this.selectedMonth < 12 ? this.selectedMonth + 1 : 1;
    const opt = this.monthsList.find(m => m.value === nextM);
    return opt ? this.getMonthName(opt) : '';
  }

  getPrevDay(): number {
    const maxDays = this.getDaysInMonth(this.selectedYear, this.selectedMonth);
    return this.selectedDay > 1 ? this.selectedDay - 1 : maxDays;
  }

  getNextDay(): number {
    const maxDays = this.getDaysInMonth(this.selectedYear, this.selectedMonth);
    return this.selectedDay < maxDays ? this.selectedDay + 1 : 1;
  }

  selectDay(d: number): void {
    this.selectedDay = d;
  }

  selectMonth(m: number): void {
    this.selectedMonth = m;
    const maxDays = this.getDaysInMonth(this.selectedYear, this.selectedMonth);
    if (this.selectedDay > maxDays) this.selectedDay = maxDays;
  }

  selectYear(y: number): void {
    this.selectedYear = y;
    const maxDays = this.getDaysInMonth(this.selectedYear, this.selectedMonth);
    if (this.selectedDay > maxDays) this.selectedDay = maxDays;
  }

  stepDay(delta: number): void {
    const maxDays = this.getDaysInMonth(this.selectedYear, this.selectedMonth);
    let newDay = this.selectedDay + delta;
    if (newDay < 1) newDay = maxDays;
    if (newDay > maxDays) newDay = 1;
    this.selectedDay = newDay;
  }

  stepMonth(delta: number): void {
    let newM = this.selectedMonth + delta;
    if (newM < 1) newM = 12;
    if (newM > 12) newM = 1;
    this.selectMonth(newM);
  }

  stepYear(delta: number): void {
    let newY = this.selectedYear + delta;
    if (newY < this.minYear) newY = this.maxYear;
    if (newY > this.maxYear) newY = this.minYear;
    this.selectYear(newY);
  }

  confirm(): void {
    const y = this.selectedYear;
    const m = this.selectedMonth.toString().padStart(2, '0');
    const d = this.selectedDay.toString().padStart(2, '0');
    const formatted = `${y}-${m}-${d}`;

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
