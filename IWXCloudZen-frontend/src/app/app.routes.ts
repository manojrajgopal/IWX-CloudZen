import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { CloudStorageComponent } from './pages/cloud-storage/cloud-storage.component';
import { ClustersComponent } from './pages/clusters/clusters.component';
import { VpcsComponent } from './pages/vpcs/vpcs.component';
import { EcrComponent } from './pages/ecr/ecr.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'dashboard/cloud-storage', component: CloudStorageComponent },
  { path: 'dashboard/clusters', component: ClustersComponent },
  { path: 'dashboard/vpcs', component: VpcsComponent },
  { path: 'dashboard/ecr', component: EcrComponent },
  { path: 'profile', component: ProfileComponent },
  { path: '**', redirectTo: '' }
];