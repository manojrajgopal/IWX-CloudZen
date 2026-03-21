import { Component, OnInit, HostListener } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit {
  scrolled = false;
  mobileMenuOpen = false;
  toggleProfileDropdown = false;
  dropdownClosing = false;
  currentUser: any = null;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getUser();
  }

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.scrolled = window.scrollY > 50;
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.closeProfileDropdown();
  }

  onProfileButtonClick() {
    if (this.toggleProfileDropdown) {
      this.closeProfileDropdown();
    } else {
      this.dropdownClosing = false;
      this.toggleProfileDropdown = true;
    }
  }

  closeProfileDropdown() {
    if (!this.toggleProfileDropdown || this.dropdownClosing) return;
    this.dropdownClosing = true;
    setTimeout(() => {
      this.toggleProfileDropdown = false;
      this.dropdownClosing = false;
    }, 200);
  }

  toggleMobileMenu() {
    this.mobileMenuOpen = !this.mobileMenuOpen;
    if (this.mobileMenuOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  }

  closeMobileMenu() {
    this.mobileMenuOpen = false;
    document.body.style.overflow = '';
  }

  logout() {
    this.authService.logout();
    this.closeMobileMenu();
  }
}