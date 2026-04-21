import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { ThemeService } from '../../../services/theme.service';
import { Subscription } from 'rxjs';

export interface MenuItem {
  label: string;
  route?: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit, OnDestroy {
  scrolled = false;
  mobileMenuOpen = false;
  menuClosing = false;
  toggleProfileDropdown = false;
  dropdownClosing = false;
  currentUser: any = null;
  private menuCloseTimer: ReturnType<typeof setTimeout> | null = null;
  private userSub!: Subscription;

  // Dropdown chain state
  activeDropdown: string | null = null;
  activeSubMenus: Set<string> = new Set();
  private dropdownCloseTimer: ReturnType<typeof setTimeout> | null = null;

  // Mobile accordion state
  mobileExpandedMenus: Set<string> = new Set();

  // Menu structure with nested children (chain)
  menuItems: MenuItem[] = [
    {
      label: 'Services',
      children: [
        {
          label: 'Compute',
          children: [
            { label: 'EC2 Instances', route: '/dashboard/ec2-instances' },
            { label: 'ECS', route: '/dashboard/ecs' },
            { label: 'Clusters', route: '/dashboard/clusters' }
          ]
        },
        {
          label: 'Networking',
          children: [
            { label: 'VPCs', route: '/dashboard/vpcs' },
            { label: 'Subnets', route: '/dashboard/subnets' },
            { label: 'Security Groups', route: '/dashboard/security-groups' },
            { label: 'Internet Gateways', route: '/dashboard/internet-gateways' }
          ]
        },
        {
          label: 'Storage',
          children: [
            {
              label: 'Cloud Storage',
              route: '/dashboard/cloud-storage',
              children: [
                { label: 'Create', route: '/dashboard/cloud-storage/create' }
              ]
            },
            { label: 'ECR', route: '/dashboard/ecr' }
          ]
        },
        {
          label: 'Monitoring',
          children: [
            { label: 'CloudWatch Logs', route: '/dashboard/cloudwatch-logs' }
          ]
        },
        {
          label: 'Analytics',
          children: [
            { label: 'Resource Graph', route: '/dashboard/resource-graph' }
          ]
        }
      ]
    }
  ];

  constructor(
    private authService: AuthService,
    public themeService: ThemeService
  ) {}

  ngOnInit(): void {
    this.userSub = this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.scrolled = window.scrollY > 50;
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.closeProfileDropdown();
    this.closeAllDropdowns();
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
      this.mobileExpandedMenus.clear();
    }, 350);
  }

  // --- Desktop dropdown chain ---
  openDropdown(label: string) {
    if (this.dropdownCloseTimer) {
      clearTimeout(this.dropdownCloseTimer);
      this.dropdownCloseTimer = null;
    }
    this.activeDropdown = label;
    this.activeSubMenus.clear();
  }

  closeAllDropdowns() {
    this.activeDropdown = null;
    this.activeSubMenus.clear();
  }

  scheduleCloseDropdowns() {
    this.dropdownCloseTimer = setTimeout(() => {
      this.closeAllDropdowns();
    }, 150);
  }

  cancelCloseDropdowns() {
    if (this.dropdownCloseTimer) {
      clearTimeout(this.dropdownCloseTimer);
      this.dropdownCloseTimer = null;
    }
  }

  openSubMenu(path: string) {
    this.cancelCloseDropdowns();
    // Remove sibling sub-menus at the same depth
    const depth = path.split('/').length;
    const toRemove: string[] = [];
    this.activeSubMenus.forEach(p => {
      if (p.split('/').length >= depth) toRemove.push(p);
    });
    toRemove.forEach(p => this.activeSubMenus.delete(p));
    this.activeSubMenus.add(path);
  }

  isSubMenuOpen(path: string): boolean {
    return this.activeSubMenus.has(path);
  }

  // --- Mobile accordion ---
  toggleMobileExpand(path: string) {
    if (this.mobileExpandedMenus.has(path)) {
      // Close this and all children
      const toRemove: string[] = [];
      this.mobileExpandedMenus.forEach(p => {
        if (p === path || p.startsWith(path + '/')) toRemove.push(p);
      });
      toRemove.forEach(p => this.mobileExpandedMenus.delete(p));
    } else {
      this.mobileExpandedMenus.add(path);
    }
  }

  isMobileExpanded(path: string): boolean {
    return this.mobileExpandedMenus.has(path);
  }

  logout() {
    this.closeProfileDropdown();
    this.authService.logout();
    this.closeMobileMenu();
  }
}