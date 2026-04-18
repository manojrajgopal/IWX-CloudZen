import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GlobalSearchComponent } from '../../../global-search/global-search.component';

@Component({
  selector: 'app-hero',
  standalone: true,
  imports: [CommonModule, GlobalSearchComponent],
  templateUrl: './hero.component.html',
  styleUrls: ['./hero.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeroComponent {}