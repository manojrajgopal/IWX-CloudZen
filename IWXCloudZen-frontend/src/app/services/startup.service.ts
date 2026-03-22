import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class StartupService {
  private readonly SESSION_KEY = 'iwx_startup_shown';

  private splashShownSubject: BehaviorSubject<boolean>;
  private loadingCompleteSubject: BehaviorSubject<boolean>;

  splashShown$: Observable<boolean>;
  loadingComplete$: Observable<boolean>;

  constructor() {
    // Show startup only the first time per browser tab session.
    // sessionStorage is cleared when the tab is closed, so reopening shows it again.
    const alreadyShown = sessionStorage.getItem(this.SESSION_KEY) === 'true';

    this.splashShownSubject = new BehaviorSubject<boolean>(!alreadyShown);
    this.loadingCompleteSubject = new BehaviorSubject<boolean>(alreadyShown);

    this.splashShown$ = this.splashShownSubject.asObservable();
    this.loadingComplete$ = this.loadingCompleteSubject.asObservable();
  }

  finishSplash(): void {
    sessionStorage.setItem(this.SESSION_KEY, 'true');
    this.splashShownSubject.next(false);
    // No fixed timer here — the LoaderComponent calls completeLoading()
    // once its progress bar visually reaches 100%, ensuring the animation
    // always finishes before the home page appears.
  }

  /** Called by LoaderComponent when the progress bar reaches 100%. */
  completeLoading(): void {
    this.loadingCompleteSubject.next(true);
  }
}