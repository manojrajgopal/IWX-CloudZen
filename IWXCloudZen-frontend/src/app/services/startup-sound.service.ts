import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class StartupSoundService {
  private audio: HTMLAudioElement;

  constructor() {
    this.audio = new Audio('assets/audio/startup-sound.mp3');
    this.audio.volume = 0.6;
    this.audio.preload = 'auto';
  }

  play(): void {
    this.audio.play().catch(err => console.warn('Startup sound blocked:', err));
  }
}