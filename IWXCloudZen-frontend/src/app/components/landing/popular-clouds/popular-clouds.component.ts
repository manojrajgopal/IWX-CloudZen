import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import cloudsData from '../../../data/clouds.json';

@Component({
  selector: 'app-popular-clouds',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './popular-clouds.component.html',
  styleUrls: ['./popular-clouds.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PopularCloudsComponent implements OnInit {
  clouds: any[] = [];

  ngOnInit(): void {
    this.clouds = cloudsData;
  }

  trackCloud(_: number, cloud: any): number {
    return cloud.id;
  }
}