import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'iwx-theme';

  private _isDark = new BehaviorSubject<boolean>(this.loadPreference());
  readonly isDark$ = this._isDark.asObservable();

  /**
   * Tracks whether dark mode was force-enabled for a session.
   * null  → no session active
   * true  → we switched to dark FOR the session (must restore on end)
   * false → user was already in dark mode; nothing to restore
   */
  private _sessionOverrideActive: boolean | null = null;

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

  /**
   * Called when an EC2 session connects.
   * If the user is in light mode, switches to dark and remembers to restore.
   * If already in dark mode, does nothing and marks "no restore needed".
   */
  enableDarkForSession(): void {
    if (this._isDark.value) {
      // Already dark — user preference, don't touch anything
      this._sessionOverrideActive = false;
    } else {
      // Was light → switch to dark for the session
      this._sessionOverrideActive = true;
      this._isDark.next(true);
      this.applyTheme(true);
      // NOTE: intentionally NOT updating localStorage so the user's real preference is preserved
    }
  }

  /**
   * Called when an EC2 session ends (disconnect / navigate away).
   * Restores light mode only if we were the ones who switched to dark.
   */
  restoreFromSession(): void {
    if (this._sessionOverrideActive === true) {
      this._isDark.next(false);
      this.applyTheme(false);
    }
    this._sessionOverrideActive = null;
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
