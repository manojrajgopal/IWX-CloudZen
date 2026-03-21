import { Component, OnInit, HostListener } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';

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
  menuClosing = false;
  toggleProfileDropdown = false;
  dropdownClosing = false;
  currentUser: any = null;
  private menuCloseTimer: ReturnType<typeof setTimeout> | null = null;

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
    if (this.mobileMenuOpen && !this.menuClosing) {
      // Menu is open — start close animation
      this.closeMobileMenu();
    } else {
      // Menu is closed or mid-close — cancel any pending close and open immediately
      if (this.menuCloseTimer) {
        clearTimeout(this.menuCloseTimer);
        this.menuCloseTimer = null;
      }
      this.mobileMenuOpen = true;
      this.menuClosing = false;
      document.body.style.overflow = 'hidden';
    }
  }

  closeMobileMenu() {
    if (!this.mobileMenuOpen || this.menuClosing) return;
    this.menuClosing = true;
    document.body.style.overflow = '';
    this.menuCloseTimer = setTimeout(() => {
      this.mobileMenuOpen = false;
      this.menuClosing = false;
      this.menuCloseTimer = null;
    }, 350);
  }

  logout() {
    this.authService.logout();
    this.closeMobileMenu();
  }
}