export type SearchResultCategory =
  | 'cluster'
  | 'bucket'
  | 'vpc'
  | 'ecr'
  | 'ecs'
  | 'subnet'
  | 'security-group'
  | 'log-group'
  | 'ec2'
  | 'file'
  | 'service'
  | 'cloud'
  | 'page';

export interface SearchResult {
  category: SearchResultCategory;
  title: string;
  subtitle: string;
  icon: string;
  route: string;
  meta?: Record<string, string>;
}

export interface SearchFilter {
  query: string;
  cloud: string;
  category: SearchResultCategory | 'all';
}
