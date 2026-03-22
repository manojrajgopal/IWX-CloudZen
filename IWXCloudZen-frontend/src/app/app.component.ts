import { Component, ChangeDetectorRef } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StartupService } from './services/startup.service';
import { SplashComponent } from './components/startup/splash/splash.component';
import { LoaderComponent } from './components/startup/loader/loader.component';
import { HeaderComponent } from './components/landing/header/header.component';
import { FooterComponent } from './components/landing/footer/footer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    CommonModule,
    SplashComponent,
    LoaderComponent,
    HeaderComponent,
    FooterComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  splashShown: boolean;
  loadingComplete: boolean;

  constructor(private startupService: StartupService, private cdr: ChangeDetectorRef) {
    // BehaviorSubject emits synchronously on subscribe, so values are set before
    // the first change-detection pass — preventing any flash of unstyled content.
    let splashInit = true;
    let loadingInit = false;
    this.startupService.splashShown$.subscribe(v => {
      splashInit = v;
      this.splashShown = v;
    });
    this.startupService.loadingComplete$.subscribe(v => {
      loadingInit = v;
      this.loadingComplete = v;
      // Force AppComponent's template to update immediately when loading
      // completes. Without this, the change is set in memory but Angular
      // only re-renders on the next user-triggered CD pass (e.g. a tap).
      this.cdr.detectChanges();
    });
    this.splashShown = splashInit;
    this.loadingComplete = loadingInit;
  }
}