import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeroComponent } from '../../components/hero/hero.component';
import { StatsComponent } from '../../components/stats/stats.component';
import { FeaturedComponent } from '../../components/featured/featured.component';
import { PopularCloudsComponent } from '../../components/popular-clouds/popular-clouds.component';
import { TestimonialsComponent } from '../../components/testimonials/testimonials.component';
import { HowItWorksComponent } from '../../components/how-it-works/how-it-works.component';
import { NewsletterComponent } from '../../components/newsletter/newsletter.component';

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