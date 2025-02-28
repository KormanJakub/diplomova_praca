import {Component, OnInit} from '@angular/core';
import {Button} from "primeng/button";
import {CurrencyPipe, DatePipe, NgIf} from "@angular/common";
import {MessageService, PrimeTemplate} from "primeng/api";
import {TableModule} from "primeng/table";
import {environment} from "../../../../Environments/environment";
import {Order} from "../../../Models/order.model";
import {Customization} from "../../../Models/customization.model";
import {Product} from "../../../Models/product.model";
import {Design} from "../../../Models/design.model";
import {ActivatedRoute} from "@angular/router";
import {AdminService} from "../../../Services/admin.service";
import {UserService} from "../../../Services/user.service";
import {User} from "../../../Models/user.model";

@Component({
  selector: 'app-user-order-info',
  standalone: true,
    imports: [
        Button,
        CurrencyPipe,
        DatePipe,
        NgIf,
        PrimeTemplate,
        TableModule
    ],
  templateUrl: './user-order-info.component.html',
  styleUrl: './user-order-info.component.css'
})
export class UserOrderInfoComponent implements OnInit {

  order!: Order;
  user!: User;
  customizations: Customization[] = [];
  products: Product[] = [];
  designs: Design[] = [];
  orderId!: number;

  manualStatuses: ManualStatus[] = [
    { value: 0, label: 'Prijaté',    bgClass: 'green-600' },
    { value: 1, label: 'Zaplatené',   bgClass: 'blue-600' },
    { value: 2, label: 'Vo výrobe',   bgClass: 'yellow-600' },
    { value: 3, label: 'Pripravené',  bgClass: 'purple-600' },
    { value: 4, label: 'Poslané',     bgClass: 'indigo-600' },
    { value: 5, label: 'Zrušené',     bgClass: 'red-600' }
  ];

  constructor(
    private route: ActivatedRoute,
    private userService: UserService
  ) {}


  ngOnInit(): void {
    this.orderId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadOrderInformation();
  }

  loadOrderInformation(): void {
    this.userService.getOrdersById(this.orderId).subscribe(response => {
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
