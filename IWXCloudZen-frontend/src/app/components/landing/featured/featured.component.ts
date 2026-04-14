import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import servicesData from '../../../data/services.json';

@Component({
  selector: 'app-featured',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './featured.component.html',
  styleUrls: ['./featured.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FeaturedComponent implements OnInit {
  featured: any[] = [];

  ngOnInit(): void {
    this.featured = servicesData.slice(0, 6);
  }

  trackService(_: number, service: any): number {
    return service.id;
  }
}