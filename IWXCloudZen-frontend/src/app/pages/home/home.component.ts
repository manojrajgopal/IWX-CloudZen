import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeroComponent } from '../../components/landing/hero/hero.component';
import { StatsComponent } from '../../components/landing/stats/stats.component';
import { FeaturedComponent } from '../../components/landing/featured/featured.component';
import { PopularCloudsComponent } from '../../components/landing/popular-clouds/popular-clouds.component';
import { TestimonialsComponent } from '../../components/landing/testimonials/testimonials.component';
import { HowItWorksComponent } from '../../components/landing/how-it-works/how-it-works.component';
import { NewsletterComponent } from '../../components/landing/newsletter/newsletter.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    HeroComponent,
    StatsComponent,
    FeaturedComponent,
    PopularCloudsComponent,
    TestimonialsComponent,
    HowItWorksComponent,
    NewsletterComponent
  ],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {}