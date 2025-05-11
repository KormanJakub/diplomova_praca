import {Component, OnInit} from '@angular/core';
import {Customization} from "../../../Models/customization.model";
import {Order} from "../../../Models/order.model";
import {AdminService} from "../../../Services/admin.service";
import {ActivatedRoute} from "@angular/router";
import {CurrencyPipe, DatePipe, NgIf} from "@angular/common";
import {TableModule} from "primeng/table";
import {Product} from "../../../Models/product.model";
import {Design} from "../../../Models/design.model";
import {environment} from "../../../../Environments/environment";
import {Button, ButtonDirective} from "primeng/button";
import {MessageService} from "primeng/api";
import {ToastModule} from "primeng/toast";
import {User} from "../../../Models/user.model";

interface ManualStatus {
  value: number;
  label: string;
  bgClass: string;
}

@Component({
  selector: 'app-order-information',
  standalone: true,
  imports: [
    CurrencyPipe,
    TableModule,
    DatePipe,
    NgIf,
    Button,
    ToastModule,
    ButtonDirective
  ],
  templateUrl: './order-information.component.html',
  styleUrl: './order-information.component.css',
  providers: [
    MessageService
  ]
})
export class OrderInformationComponent implements OnInit {

  order!: Order;
  customizations: Customization[] = [];
  products: Product[] = [];
  designs: Design[] = [];
  user!: User;
  orderId!: number;

  manualStatuses: ManualStatus[] = [
    { value: 0, label: 'Rezervované',    bgClass: 'green-600' },
    { value: 1, label: 'Zaplatené',   bgClass: 'blue-600' },
    { value: 2, label: 'Vo výrobe',   bgClass: 'yellow-600' },
    { value: 3, label: 'Pripravené',  bgClass: 'purple-600' },
    { value: 4, label: 'Poslané',     bgClass: 'indigo-600' },
    { value: 5, label: 'Zrušené',     bgClass: 'red-600' },
    { value: 6, label: 'Reklamácia',     bgClass: 'bg-red-600' }
  ];

  constructor(
    private route: ActivatedRoute,
    private adminService: AdminService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.orderId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadOrderInformation();
  }

  loadOrderInformation(): void {
    this.adminService.getOrderInformation(this.orderId).subscribe(response => {
      this.order = response.order;
      this.customizations = response.customizations;
      this.products = response.products;
      this.designs = response.designs;

      this.adminService.getUserInformation(this.order.UserId)
        .subscribe({
          next: (env: UserEnvelope) => {
            if (env.userType === 'Normal') {
              this.user = env.data as User;
            } else {
              this.user = env.data as User;
            }
          },
          error: err => console.error(err)
        })
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

  increaseStatus(orderId: number): void {
    this.adminService.increaseOrderStatus(orderId).subscribe(() => {
      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: 'Stav navýšený!',
        life: 3000
      });
      this.loadOrderInformation();
    });
  }

  decreaseStatus(orderId: number): void {
    this.adminService.decreaseOrderStatus(orderId).subscribe(
      () => {
      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: 'Stav znížený!',
        life: 3000
      });
      this.loadOrderInformation();
    });
  }

  cancelOrder(orderId: number): void {
    this.adminService.cancelOrder(orderId).subscribe(
      () => {
      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: 'Objednávka zrušená',
        life: 3000
      });
      this.loadOrderInformation();
    });
  }
}

interface UserEnvelope {
  userType: 'Normal' | 'Guest';
  data: any;
}
