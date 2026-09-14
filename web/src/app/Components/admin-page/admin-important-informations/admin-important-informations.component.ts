import {Component, ElementRef, OnInit, ViewChild} from '@angular/core';
import {AdminService} from "../../../Services/admin.service";
import {Chart, ChartConfiguration} from 'chart.js/auto';
import {FooterComponent} from "../../footer/footer.component";
import {HeaderComponent} from "../../header/header.component";
import {CurrencyPipe, NgForOf} from "@angular/common";
import {TableModule} from "primeng/table";
import {FormsModule} from "@angular/forms";
import {InputNumberModule} from "primeng/inputnumber";
import {Button} from "primeng/button";
import {ToastModule} from "primeng/toast";
import {MessageService} from "primeng/api";

import {InputTextModule} from "primeng/inputtext";

@Component({
  selector: 'app-admin-important-informations',
  standalone: true,
  imports: [
    FooterComponent,
    HeaderComponent,
    NgForOf,
    TableModule,
    CurrencyPipe,
    FormsModule,
    InputNumberModule,
    InputTextModule,
    Button,
    ToastModule
  ],
  templateUrl: './admin-important-informations.component.html',
  styleUrl: './admin-important-informations.component.css',
  providers: [MessageService]
})
export class AdminImportantInformationsComponent implements OnInit {
  @ViewChild('salesChart') salesChartRef!: ElementRef;
  salesChart!: Chart;

  totalOrders: number = 0;
  totalRevenue: number = 0;
  averageOrderValue: number = 0;
  newCustomers: number = 0;

  soldOrders: number = 0;
  pendingOrders: number = 0;
  makingOrders: number = 0;
  readyOrders: number = 0;
  sendOrders: number = 0;
  cancelOrders: number = 0;
  lowStockProducts: any[] = [];

  cashOnDeliveryFee: number = 1.0;
  packetaApiKey: string = '';
  isSavingSettings: boolean = false;

  constructor(
    private adminService: AdminService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadKpiData();
    this.loadSalesSummaryAll();
    this.loadLowStockProducts();
    this.loadSettings();
  }

  loadSettings(): void {
    this.adminService.getSettings().subscribe({
      next: (settings: any) => {
        if (settings && typeof settings.cashOnDeliveryFee === 'number') {
          this.cashOnDeliveryFee = settings.cashOnDeliveryFee;
        }
        if (settings && settings.packetaApiKey) {
          this.packetaApiKey = settings.packetaApiKey;
        }
      },
      error: (err) => console.error(err)
    });
  }

  saveSettings(): void {
    if (this.cashOnDeliveryFee < 0) {
      this.messageService.add({
        severity: 'error',
        summary: 'Chyba',
        detail: 'Poplatok za dobierku nemôže byť záporný.'
      });
      return;
    }

    this.isSavingSettings = true;
    this.adminService.updateSettings(this.cashOnDeliveryFee, this.packetaApiKey).subscribe({
      next: () => {
        this.isSavingSettings = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Úspech',
          detail: 'Nastavenia boli úspešne uložené.'
        });
      },
      error: (err) => {
        this.isSavingSettings = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Chyba',
          detail: err?.error?.error || 'Nepodarilo sa uložiť nastavenia.'
        });
      }
    });
  }

  loadKpiData(): void {
    this.adminService.getKpiData().subscribe(data => {
      this.totalOrders = data.totalOrders;
      this.totalRevenue = data.totalRevenue;
      this.averageOrderValue = data.averageOrderValue;
      this.newCustomers = data.newCustomers;
    });
  }

  loadSalesSummaryAll(): void {
    this.adminService.getSalesSummaryAll().subscribe(summary => {
      this.soldOrders = summary.soldOrders;
      this.pendingOrders = summary.pendingOrders;
      this.makingOrders = summary.makingOrders;
      this.readyOrders = summary.readyOrders;
      this.sendOrders = summary.sendOrders;
      this.cancelOrders = summary.cancelOrders;
      this.initializeChart();
    });
  }

  loadLowStockProducts(): void {
    this.adminService.getLowStockProducts().subscribe(products => {
      this.lowStockProducts = products;
    });
  }

  initializeChart(): void {
    const data = {
      labels: ['Zaplatené', 'Prijaté', 'Vo výrobe', 'Pripravené', 'Poslané', 'Zrušené'],
      datasets: [{
        label: 'Stav objednávok',
        data: [
          this.soldOrders,
          this.pendingOrders,
          this.makingOrders,
          this.readyOrders,
          this.sendOrders,
          this.cancelOrders
        ],
        backgroundColor: [
          '#4caf50',  // zelená pre zaplatené
          '#ffc107',  // žltá pre prijaté
          '#2196f3',  // modrá pre vo výrobe
          '#9c27b0',  // fialová pre pripravené
          '#03a9f4',  // svetlomodrá pre poslané
          '#f44336'   // červená pre zrušené
        ]
      }]
    };

    const config: ChartConfiguration = {
      type: 'doughnut',
      data: data,
      options: {
        responsive: true,
        plugins: {
          legend: {
            position: 'right'
          },
          title: {
            display: true,
            text: 'Prehľad objednávok'
          }
        }
      }
    };

    this.salesChart = new Chart(this.salesChartRef.nativeElement, config);
  }
}
