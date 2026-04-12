export interface CloudAccount {
  id: number;
  provider: string;
  accountName: string;
  region: string;
  isDefault: boolean;
  createdAt: string;
  lastValidatedAt: string;
}

export interface CloudProvider {
  value: string;
  label: string;
  requiredFields: string[];
}
