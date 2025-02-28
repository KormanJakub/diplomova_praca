import {Component, OnInit} from '@angular/core';
import {UserService} from "../../../Services/user.service";
import {User} from "../../../Models/user.model";
import {Order} from "../../../Models/order.model";
import {Router} from "@angular/router";
import {Customization} from "../../../Models/customization.model";
import {Design} from "../../../Models/design.model";
import {Product} from "../../../Models/product.model";
import {environment} from "../../../../Environments/environment";
import {Button} from "primeng/button";
import {CurrencyPipe, NgClass, NgForOf} from "@angular/common";
import {PrimeTemplate} from "primeng/api";
import {TableModule} from "primeng/table";

@Component({
  selector: 'app-user-orders',
  standalone: true,
  imports: [
    Button,
    CurrencyPipe,
    NgForOf,
    PrimeTemplate,
    TableModule,
    NgClass
  ],
  templateUrl: './user-orders.component.html',
  styleUrl: './user-orders.component.css'
})
export class UserOrdersComponent implements OnInit {

  orders: Order[] = [];
  customizations: Customization[] = [];
  designs: Design[] = [];
  products: Product[] = [];
  orderId!: number;

  groupedOrders: { status: ManualStatus, orders: Order[] }[] = [];

  manualStatuses: ManualStatus[] = [
    { value: 0, label: 'Prijaté',    bgClass: 'bg-green-600' },
    { value: 1, label: 'Zaplatené',   bgClass: 'bg-blue-600' },
    { value: 2, label: 'Vo výrobe',   bgClass: 'bg-yellow-600' },
    { value: 3, label: 'Pripravené',  bgClass: 'bg-purple-600' },
    { value: 4, label: 'Poslané',     bgClass: 'bg-indigo-600' },
    { value: 5, label: 'Zrušené',     bgClass: 'bg-red-600' }
  ];

  constructor(
    private userService: UserService,
    private router: Router,
  ) { }

  ngOnInit() {
    this.refreshData();
  }

  refreshData() {
    this.userService.getOrders().subscribe({
      next: (response) => {
        this.orders = response.orders;
        this.products = response.products;
        this.customizations = response.customizations;
        this.designs = response.designs;
        this.groupOrdersByStatus();
      }
    })
  }

  goToOrderInformation(orderId: number): void {
    this.router.navigate(['/user/my-order', orderId]);
  }

  groupOrdersByStatus(): void {
    this.groupedOrders = this.manualStatuses.map(status => ({
      status,
      orders: this.orders.filter(order => order.StatusOrder === status.value)
    })).filter(group => group.orders.length > 0);
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
