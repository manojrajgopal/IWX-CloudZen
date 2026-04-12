import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { GlobalSearchService } from './global-search.service';
import { SearchResult, SearchResultCategory } from './search-result.model';

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-results.component.html',
  styleUrls: ['./search-results.component.css']
})
export class SearchResultsComponent implements OnInit, OnDestroy {
  query = '';
  cloud = 'all';
  selectedCategories: Set<SearchResultCategory> = new Set();
  isAllSelected = true;
  results: SearchResult[] = [];
  loading = false;
  categories: { value: SearchResultCategory | 'all'; label: string }[] = [];
  cachedGroups: { category: SearchResultCategory; label: string; items: SearchResult[] }[] = [];

  private subs: Subscription[] = [];

  constructor(
    private searchService: GlobalSearchService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.categories = this.searchService.getAllCategories();

    this.subs.push(
      this.route.queryParams.subscribe(params => {
        this.query = params['q'] || '';
        this.cloud = params['cloud'] || 'all';
        this.isAllSelected = true;
        this.selectedCategories.clear();
        if (this.query.trim()) {
          this.searchService.search({
            query: this.query,
            cloud: this.cloud,
            category: 'all'
          });
        }
      }),
      this.searchService.results$.subscribe(results => {
        this.results = results;
        this.rebuildGroups();
      }),
      this.searchService.loading$.subscribe(loading => {
        this.loading = loading;
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  onSearch(): void {
    this.router.navigate(['/search'], {
      queryParams: { q: this.query, cloud: this.cloud }
    });
  }

  onCategoryChange(cat: SearchResultCategory | 'all'): void {
    if (cat === 'all') {
      // Clicking All clears all individual selections
      this.isAllSelected = true;
      this.selectedCategories.clear();
    } else {
      // Toggle the category
      if (this.selectedCategories.has(cat)) {
        this.selectedCategories.delete(cat);
      } else {
        this.selectedCategories.add(cat);
      }
      // If nothing selected, revert to all
      if (this.selectedCategories.size === 0) {
        this.isAllSelected = true;
      } else {
        this.isAllSelected = false;
      }
    }
    this.rebuildGroups();
  }

  isCategoryActive(cat: SearchResultCategory | 'all'): boolean {
    if (cat === 'all') return this.isAllSelected;
    return this.selectedCategories.has(cat);
  }

  navigateTo(result: SearchResult): void {
    const route = result.route;
    this.searchService.navigateTo(route);
  }

  filteredResults(): SearchResult[] {
    if (this.isAllSelected || this.selectedCategories.size === 0) return this.results;
    return this.results.filter(r => this.selectedCategories.has(r.category));
  }

  private rebuildGroups(): void {
    const data = this.filteredResults();
    const groups = new Map<SearchResultCategory, SearchResult[]>();
    for (const r of data) {
      if (!groups.has(r.category)) groups.set(r.category, []);
      groups.get(r.category)!.push(r);
    }
    this.cachedGroups = Array.from(groups.entries()).map(([cat, items]) => ({
      category: cat,
      label: this.searchService.getCategoryLabel(cat),
      items,
    }));
  }

  getCategoryCount(cat: SearchResultCategory | 'all'): number {
    if (cat === 'all') return this.results.length;
    return this.results.filter(r => r.category === cat).length;
  }

  get activeCategories() {
    return this.categories.filter(c => this.getCategoryCount(c.value) > 0);
  }
}
