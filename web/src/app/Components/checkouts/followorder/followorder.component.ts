import {Component, OnInit} from '@angular/core';
import {Order} from "../../../Models/order.model";
import {User} from "../../../Models/user.model";
import {Customization} from "../../../Models/customization.model";
import {Product} from "../../../Models/product.model";
import {Design} from "../../../Models/design.model";
import {environment} from "../../../../Environments/environment";
import {ActivatedRoute} from "@angular/router";
import {GuestService} from "../../../Services/guest.service";
import {CurrencyPipe, DatePipe, NgIf} from "@angular/common";
import {PrimeTemplate} from "primeng/api";
import {TableModule} from "primeng/table";
import {FooterComponent} from "../../footer/footer.component";
import {HeaderComponent} from "../../header/header.component";

@Component({
  selector: 'app-followorder',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    NgIf,
    PrimeTemplate,
    TableModule,
    FooterComponent,
    HeaderComponent
  ],
  templateUrl: './followorder.component.html',
  styleUrl: './followorder.component.css'
})
export class FolloworderComponent implements OnInit {
  order!: Order;
  user!: User;
  customizations: Customization[] = [];
  products: Product[] = [];
  designs: Design[] = [];
  followToken: string = "";

  manualStatuses: ManualStatus[] = [
    { value: 0, label: 'Rezervované',    bgClass: 'green-600' },
    { value: 1, label: 'Zaplatené',   bgClass: 'blue-600' },
    { value: 2, label: 'Vo výrobe',   bgClass: 'yellow-600' },
    { value: 3, label: 'Pripravené',  bgClass: 'purple-600' },
    { value: 4, label: 'Poslané',     bgClass: 'indigo-600' },
    { value: 5, label: 'Zrušené',     bgClass: 'red-600' }
  ];

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.followToken = params['followToken'];
    });
    this.loadOrderInformation();
  }

  constructor(
    private route: ActivatedRoute,
    private guestService: GuestService
  ) {}

  loadOrderInformation(): void {
    this.guestService.followOrder(this.followToken).subscribe(response => {
      console.log(response)
      this.order = response.order;
      this.customizations = response.customizations;
      this.products = response.products;
      this.designs = response.designs;
      this.user = response.user;
    });
  }

  getDesignPath(designId: string): string {
    const design = this.designs.find(d => d.Id === designId);
    return design && design.PathOfFile
      ? environment.apiUrl + design.PathOfFile
      : 'assets/images/placeholder-design.png';
  }

  getProductColorPath(productId: string, productColor: string): string {
    const product = this.products.find(p => p.Id.toString() === productId);
    if (product && product.Colors) {
      const colorInfo = product.Colors.find(c => c.Name.toLowerCase() === productColor.toLowerCase());
      return colorInfo && colorInfo.PathOfFile
        ? environment.apiUrl + colorInfo.PathOfFile
        : 'assets/images/placeholder-product.png';
    }
    return 'assets/images/placeholder-product.png';
  }


  getDesignName(designId: string): string {
    const design = this.designs.find(d => d.Id === designId);
    return design ? design.Name : '';
  }

  getProductName(productId: string): string {
    const product = this.products.find(p => p.Id.toString() === productId);
    return product ? product.Name : '';
  }

  getManualStatusLabel(status: number): string {
    const mapping = this.manualStatuses.find(item => item.value === status);
    return mapping ? mapping.label : 'Neznámy status';
  }
}

interface ManualStatus {
  value: number;
  label: string;
  bgClass: string;
}
