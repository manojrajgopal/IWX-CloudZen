import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'iwx-theme';

  private _isDark = new BehaviorSubject<boolean>(this.loadPreference());
  readonly isDark$ = this._isDark.asObservable();

  get isDark(): boolean {
    return this._isDark.value;
  }

  constructor() {
    this.applyTheme(this._isDark.value);
  }

  toggle(): void {
    const next = !this._isDark.value;
    this._isDark.next(next);
    localStorage.setItem(this.STORAGE_KEY, next ? 'dark' : 'light');
    this.applyTheme(next);
  }

  private loadPreference(): boolean {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (saved) return saved === 'dark';
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }

  private applyTheme(dark: boolean): void {
    if (dark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }
}
