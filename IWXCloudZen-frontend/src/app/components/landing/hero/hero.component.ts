import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GlobalSearchComponent } from '../../../global-search/global-search.component';

@Component({
  selector: 'app-hero',
  standalone: true,
  imports: [CommonModule, GlobalSearchComponent],
  templateUrl: './hero.component.html',
  styleUrls: ['./hero.component.css']
})
export class HeroComponent {}