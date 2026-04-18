import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import testimonialsData from '../../../data/testimonials.json';

@Component({
  selector: 'app-testimonials',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './testimonials.component.html',
  styleUrls: ['./testimonials.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TestimonialsComponent implements OnInit {
  testimonials: any[] = [];
  current = 0;

  ngOnInit(): void {
    this.testimonials = testimonialsData;
  }

  prev() {
    this.current = (this.current - 1 + this.testimonials.length) % this.testimonials.length;
  }

  next() {
    this.current = (this.current + 1) % this.testimonials.length;
  }
}