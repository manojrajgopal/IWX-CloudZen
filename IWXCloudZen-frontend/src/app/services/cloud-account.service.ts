import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CloudAccount, CloudProvider } from '../models/cloud-account.model';

export interface ConnectAccountRequest {
  Provider: string;
  AccountName: string;
  AccessKey: string;
  SecretKey: string;
  TenantId: string | null;
  ClientId: string | null;
  ClientSecret: string | null;
  Region: string;
  MakeDefault: boolean;
}

export interface ConnectAccountResponse {
  message: string;
  data: CloudAccount;
}

@Injectable({
  providedIn: 'root'
})
export class CloudAccountService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getAccounts(): Observable<CloudAccount[]> {
    return this.http.get<CloudAccount[]>(`${this.apiUrl}/api/cloud/accounts`);
  }

  getDefaultAccount(): Observable<CloudAccount> {
    return this.http.get<CloudAccount>(`${this.apiUrl}/api/cloud/accounts/default`);
  }

  getProviders(): Observable<CloudProvider[]> {
    return this.http.get<CloudProvider[]>(`${this.apiUrl}/api/cloud/providers`);
  }

  connectAccount(request: ConnectAccountRequest): Observable<ConnectAccountResponse> {
    return this.http.post<ConnectAccountResponse>(`${this.apiUrl}/api/cloud/connect`, request);
  }

  setDefaultAccount(accountId: number): Observable<ConnectAccountResponse> {
    return this.http.post<ConnectAccountResponse>(`${this.apiUrl}/api/cloud/accounts/${accountId}/default`, {});
  }
}
