import { Component, OnInit, OnDestroy, ElementRef, ViewChild, HostListener, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { GlobalSearchService } from './global-search.service';
import { SearchResult, SearchResultCategory } from './search-result.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-global-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './global-search.component.html',
  styleUrls: ['./global-search.component.css']
})
export class GlobalSearchComponent implements OnInit, OnDestroy {
  query = '';
  cloud = 'all';
  isOpen = false;
  results: SearchResult[] = [];
  loading = false;
  highlightIndex = -1;
  cachedGroups: { category: SearchResultCategory; label: string; items: SearchResult[] }[] = [];

  private searchSubject = new Subject<string>();
  private subs: Subscription[] = [];

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @Output() searched = new EventEmitter<{ query: string; cloud: string }>();

  clouds = [
    { value: 'all', label: 'All Clouds' },
    { value: 'aws', label: 'AWS' },
    { value: 'azure', label: 'Azure' },
    { value: 'gcp', label: 'GCP' },
  ];

  constructor(
    private searchService: GlobalSearchService,
    private router: Router,
    private elRef: ElementRef
  ) {}

  ngOnInit(): void {
    this.searchService.clearResults();
    this.subs.push(
      this.searchSubject.pipe(
        debounceTime(300),
        distinctUntilChanged()
      ).subscribe(q => {
        if (q.trim().length >= 2) {
          this.searchService.search({ query: q, cloud: this.cloud, category: 'all' });
        } else {
          this.results = [];
          this.isOpen = false;
        }
      }),
      this.searchService.results$.subscribe(results => {
        this.results = results;
        this.rebuildGroups();
        this.isOpen = results.length > 0 || this.loading;
        this.highlightIndex = -1;
      }),
      this.searchService.loading$.subscribe(loading => {
        this.loading = loading;
        if (loading && this.query.trim().length >= 2) {
          this.isOpen = true;
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.searchService.clearResults();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    if (!this.elRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  onInput(): void {
    this.searchSubject.next(this.query);
  }

  onCloudChange(): void {
    if (this.query.trim().length >= 2) {
      this.searchService.search({ query: this.query, cloud: this.cloud, category: 'all' });
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (!this.isOpen) return;

    const maxIdx = Math.min(this.results.length, 8) - 1;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.highlightIndex = Math.min(this.highlightIndex + 1, maxIdx);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.highlightIndex = Math.max(this.highlightIndex - 1, -1);
        break;
      case 'Enter':
        event.preventDefault();
        if (this.highlightIndex >= 0 && this.highlightIndex < this.results.length) {
          this.selectResult(this.results[this.highlightIndex]);
        } else {
          this.submitFullSearch();
        }
        break;
      case 'Escape':
        this.isOpen = false;
        this.highlightIndex = -1;
        break;
    }
  }

  selectResult(result: SearchResult): void {
    const route = result.route;
    this.isOpen = false;
    this.searchService.navigateTo(route);
  }

  submitFullSearch(): void {
    if (!this.query.trim()) return;
    this.isOpen = false;
    this.searched.emit({ query: this.query, cloud: this.cloud });
    this.router.navigate(['/search'], {
      queryParams: { q: this.query, cloud: this.cloud }
    });
  }

  private rebuildGroups(): void {
    const groups = new Map<SearchResultCategory, SearchResult[]>();
    const limited = this.results.slice(0, 8);
    for (const r of limited) {
      if (!groups.has(r.category)) groups.set(r.category, []);
      groups.get(r.category)!.push(r);
    }
    this.cachedGroups = Array.from(groups.entries()).map(([cat, items]) => ({
      category: cat,
      label: this.searchService.getCategoryLabel(cat),
      items,
    }));
  }

  getResultIndex(result: SearchResult): number {
    return this.results.indexOf(result);
  }
}
