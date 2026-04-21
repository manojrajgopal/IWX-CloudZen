import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { MappedAddressesResponse, NetworkInterfacesResponse, FlatResource } from '../../../../models/cloud-services.model';
import { getResourceConfig, getResourceRoute, getStateClass } from '../shared/graph-node/graph-node.component';

@Component({
  selector: 'app-vpc-network-info',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './vpc-network-info.component.html',
  styleUrls: ['./vpc-network-info.component.css']
})
export class VpcNetworkInfoComponent implements OnInit {
  loading = true;
  error: string | null = null;
  vpcId = '';
  accountId = 0;

  mappedAddresses: MappedAddressesResponse | null = null;
  networkInterfaces: NetworkInterfacesResponse | null = null;

  constructor(
    private route: ActivatedRoute,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    this.vpcId = this.route.snapshot.paramMap.get('vpcId') || '';
    this.accountId = Number(this.route.snapshot.queryParamMap.get('accountId') || 0);

    if (!this.vpcId || !this.accountId) {
      this.error = 'Missing VPC ID or Account ID';
      this.loading = false;
      return;
    }
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    let loaded = 0;
    const checkDone = () => { loaded++; if (loaded >= 2) this.loading = false; };

    this.cloudServicesService.getMappedPublicAddresses(this.vpcId, this.accountId).subscribe({
      next: (res) => { this.mappedAddresses = res; checkDone(); },
      error: () => checkDone()
    });

    this.cloudServicesService.getVpcNetworkInterfaces(this.vpcId, this.accountId).subscribe({
      next: (res) => { this.networkInterfaces = res; checkDone(); },
      error: () => checkDone()
    });
  }

  getResourceConfig(type: string) { return getResourceConfig(type); }
  getStateClass(state: string) { return getStateClass(state); }
  getResourceRoute(type: string, dbId: number) { return getResourceRoute(type, dbId); }
}
