import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StartupService } from '../../../services/startup.service';
import { StartupSoundService } from '../../../services/startup-sound.service';

@Component({
  selector: 'app-splash',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './splash.component.html',
  styleUrls: ['./splash.component.css']
})
export class SplashComponent implements OnInit {
  showMessage = false;

  constructor(
    private startupService: StartupService,
    private soundService: StartupSoundService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Show bottom message after 1.5 seconds.
    // ChangeDetectorRef.detectChanges() ensures the opacity update is applied
    // even if Angular's Zone.js patch hasn't started a CD cycle yet.
    setTimeout(() => {
      this.showMessage = true;
      this.cdr.detectChanges();
    }, 1500);
  }

  handleStart(): void {
    this.soundService.play();
    this.startupService.finishSplash();
  }
}