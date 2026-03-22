import { Component, OnInit, OnDestroy, AfterViewInit, ElementRef, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StartupService } from '../../../services/startup.service';
import { StartupSoundService } from '../../../services/startup-sound.service';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './loader.component.html',
  styleUrls: ['./loader.component.css']
})
export class LoaderComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('cloudPath') cloudPath!: ElementRef<SVGPathElement>;
  progress = 0;
  private interval: any;

  constructor(
    private startupService: StartupService,
    private soundService: StartupSoundService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.soundService.play();

    // Progress bar: 2% every 50ms = 100% in 2500ms.
    // The loader itself signals completion so the bar always visually
    // reaches 100% before the home page is revealed.
    const step = 2;
    const intervalTime = 50;
    this.interval = setInterval(() => {
      this.progress = Math.min(this.progress + step, 100);
      this.cdr.detectChanges(); // paint the bar width immediately each tick
      if (this.progress >= 100) {
        clearInterval(this.interval);
        // Small pause so the user sees the full bar before navigation.
        setTimeout(() => this.startupService.completeLoading(), 400);
      }
    }, intervalTime);
  }

  ngAfterViewInit(): void {
    // Decorative SVG cloud drawing — guard against environments where
    // getTotalLength() may return 0 or throw before the element is painted.
    try {
      const path = this.cloudPath?.nativeElement;
      if (!path) return;
      const length = path.getTotalLength();
      if (length > 0) {
        path.style.strokeDasharray = `${length}`;
        path.style.strokeDashoffset = `${length}`;
        path.getBoundingClientRect(); // force reflow
        path.style.animation = 'draw 2s ease-in-out forwards';
      }
    } catch {
      // Cloud animation is purely decorative — do not let errors here
      // interfere with the progress bar or loading completion.
    }
  }

  ngOnDestroy(): void {
    if (this.interval) clearInterval(this.interval);
  }
}