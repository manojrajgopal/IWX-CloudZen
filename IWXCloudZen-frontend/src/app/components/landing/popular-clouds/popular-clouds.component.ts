import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import cloudsData from '../../../data/clouds.json';

@Component({
  selector: 'app-popular-clouds',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './popular-clouds.component.html',
  styleUrls: ['./popular-clouds.component.css']
})
export class PopularCloudsComponent implements OnInit {
  clouds: any[] = [];

  ngOnInit(): void {
    this.clouds = cloudsData;
  }
}