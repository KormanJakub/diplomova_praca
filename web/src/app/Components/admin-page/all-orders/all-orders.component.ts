import {Component, OnInit} from '@angular/core';
import {Order} from "../../../Models/order.model";
import {AdminService} from "../../../Services/admin.service";
import {CurrencyPipe, DecimalPipe, JsonPipe, NgClass, NgForOf} from "@angular/common";
import {EStatus} from "../../../Enums/status-order.enum";
import {TableModule} from "primeng/table";
import {Button} from "primeng/button";
import {Router} from "@angular/router";
import {FormsModule} from "@angular/forms";

interface ManualStatus {
  value: number;
  label: string;
  bgClass: string;
}

@Component({
  selector: 'app-all-orders',
  standalone: true,
  imports: [
    CurrencyPipe,
    NgForOf,
    NgClass,
    TableModule,
    JsonPipe,
    Button,
    FormsModule,
    DecimalPipe
  ],
  templateUrl: './all-orders.component.html',
  styleUrl: './all-orders.component.css'
})
export class AllOrdersComponent implements OnInit{

  originalOrders: Order[] = [];
  orders: Order[] = [];
  groupedOrders: { status: ManualStatus, orders: Order[] }[] = [];
  filterValue: string = '';

  manualStatuses: ManualStatus[] = [
    { value: 0, label: 'Rezervované',    bgClass: 'bg-green-600' },
    { value: 1, label: 'Zaplatené',   bgClass: 'bg-blue-600' },
    { value: 2, label: 'Vo výrobe',   bgClass: 'bg-yellow-600' },
    { value: 3, label: 'Pripravené',  bgClass: 'bg-purple-600' },
    { value: 4, label: 'Poslané',     bgClass: 'bg-indigo-600' },
    { value: 5, label: 'Zrušené',     bgClass: 'bg-red-600' },
    { value: 6, label: 'Reklamácia',     bgClass: 'bg-red-600' }
  ];

  constructor(
    private adminService: AdminService,
    private router: Router,
  ) {}

  ngOnInit() {
    this.refreshOrders();
  }

  refreshOrders() {
    this.adminService.getAllOrders().subscribe(response => {
      this.originalOrders = response.Orders;
      this.orders = [...this.originalOrders];
      this.groupOrdersByStatus();
    });
  }

  applyFilter(): void {
    const filter = this.filterValue.trim();
    if (filter === '') {
      this.orders = [...this.originalOrders];
    } else {
      const lowerFilter = filter.toLowerCase();
      const filterNumber = Number(filter);
      this.orders = this.originalOrders.filter(order => {
        if (!isNaN(filterNumber) && order.Id === filterNumber) {
          return true;
        }
        return order.Id.toString().includes(filter) ||
          order.UserId.toString().includes(filter) ||
          order.TotalPrice.toString().includes(filter) ||
          this.getManualStatusLabel(order.StatusOrder).toLowerCase().includes(lowerFilter);
      });
    }
    this.groupOrdersByStatus();
  }



  groupOrdersByStatus(): void {
    this.groupedOrders = this.manualStatuses.map(status => ({
      status,
      orders: this.orders.filter(order => order.StatusOrder === status.value)
    })).filter(group => group.orders.length > 0);
  }

  goToOrderInformation(orderId: number): void {
    this.router.navigate(['/admin/orders', orderId]);
  }

  getManualStatusLabel(status: number): string {
    const mapping = this.manualStatuses.find(item => item.value === status);
    return mapping ? mapping.label : 'Neznámy status';
  }

  public EStatus = EStatus;

}
