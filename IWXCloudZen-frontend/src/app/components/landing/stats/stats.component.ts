import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stats',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stats.component.html',
  styleUrls: ['./stats.component.css']
})
export class StatsComponent {
  stats = [
    { label: 'Tools & Services', value: '200+' },
    { label: 'Happy Teams', value: '1,200+' },
    { label: 'Cloud Providers', value: '3' },
    { label: 'Countries Served', value: '25+' }
  ];
}