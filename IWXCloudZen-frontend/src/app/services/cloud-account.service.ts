import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CloudAccount, CloudProvider } from '../models/cloud-account.model';

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
}
