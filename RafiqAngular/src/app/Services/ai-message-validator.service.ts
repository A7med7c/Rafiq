import { Injectable } from '@angular/core';

export interface MessageValidationResult {
  valid: boolean;
  /** i18n key inside `t().aiAssistant` — null when valid */
  errorKey: string | null;
}

// English keyboard rows (forward and backward)
const EN_KEYBOARD_ROWS = [
  'qwertyuiop', 'poiuytrewq',
  'asdfghjkl',  'lkjhgfdsa',
  'zxcvbnm',    'mnbvcxz',
];

// Arabic keyboard rows (forward and backward) — standard Arabic QWERTY layout
const AR_KEYBOARD_ROWS = [
  'ضصثقفغعهخحجد', 'دجحخهعغفقثصض',
  'شسيبلاتنمكط',  'طكمنتالبيسش',
  'ئءؤرلاىةوزظ',  'ظزوةىلارؤءئ',
];

@Injectable({ providedIn: 'root' })
export class AiMessageValidatorService {

  validate(text: string): MessageValidationResult {
    const trimmed = text.trim();

    // 1. Empty
    if (!trimmed) return { valid: false, errorKey: 'validationEmpty' };

    // Remove all whitespace for char-level analysis
    const noSpaces = trimmed.replace(/\s+/g, '');

    // 2. Too short (< 2 non-whitespace chars)
    if (noSpaces.length < 2) return { valid: false, errorKey: 'validationTooShort' };

    // 3. Single character repeated 4+ times (e.g. "aaaaa", "ىىىىى")
    if (/^(.)\1{3,}$/.test(noSpaces))
      return { valid: false, errorKey: 'validationGibberish' };

    // 4. No spaces in a long string (> 15 chars) — real sentences have word breaks
    if (noSpaces.length > 15 && !trimmed.includes(' '))
      return { valid: false, errorKey: 'validationGibberish' };

    // Only run deeper checks on text that is long enough to be meaningful
    if (noSpaces.length >= 6) {

      // 5. Keyboard row smashing (English)
      const lower = noSpaces.toLowerCase();
      for (const row of EN_KEYBOARD_ROWS) {
        if (this.isRowSmash(lower, row))
          return { valid: false, errorKey: 'validationGibberish' };
      }

      // 6. Keyboard row smashing (Arabic)
      for (const row of AR_KEYBOARD_ROWS) {
        if (this.isRowSmash(noSpaces, row))
          return { valid: false, errorKey: 'validationGibberish' };
      }

      // 7. Extremely low character variety (< 20% unique chars for strings ≥ 10 chars)
      if (noSpaces.length >= 10) {
        const uniqueRatio = new Set(noSpaces.toLowerCase()).size / noSpaces.length;
        if (uniqueRatio < 0.20)
          return { valid: false, errorKey: 'validationGibberish' };
      }

      // 8. English consonant soup: < 7% vowels among alpha chars (catches "asdfg", "zxcvb")
      const hasArabic = /[؀-ۿ]/.test(noSpaces);
      if (!hasArabic) {
        const alpha = noSpaces.replace(/[^a-zA-Z]/g, '').toLowerCase();
        if (alpha.length >= 6) {
          const vowels = (alpha.match(/[aeiou]/g) ?? []).length;
          const vowelRatio = vowels / alpha.length;
          if (vowelRatio < 0.07)
            return { valid: false, errorKey: 'validationGibberish' };
        }
      }
    }

    // 9. Excessive word repetition (same word > 70% of 4+ total words)
    const words = trimmed.split(/\s+/);
    if (words.length >= 4) {
      const counts: Record<string, number> = {};
      for (const w of words) {
        const key = w.toLowerCase();
        counts[key] = (counts[key] ?? 0) + 1;
      }
      const maxCount = Math.max(...Object.values(counts));
      if (maxCount / words.length > 0.70)
        return { valid: false, errorKey: 'validationRepetitive' };
    }

    return { valid: true, errorKey: null };
  }

  /** Returns true when ≥ 80% of `input` chars belong to `row` chars. */
  private isRowSmash(input: string, row: string): boolean {
    if (input.length < 5) return false;
    const rowSet = new Set(row);
    const hits = [...input].filter(c => rowSet.has(c)).length;
    return hits / input.length >= 0.80;
  }
}
