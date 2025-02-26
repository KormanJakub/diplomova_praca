import {Component, OnInit} from '@angular/core';
import {Product} from "../../../../Models/product.model";
import {TableModule} from "primeng/table";
import {CurrencyPipe, DatePipe, NgClass, NgIf} from "@angular/common";
import {AdminService} from "../../../../Services/admin.service";
import {Design} from "../../../../Models/design.model";

@Component({
  selector: 'app-all-products',
  standalone: true,
  imports: [
    TableModule,
    NgClass,
    CurrencyPipe,
    DatePipe,
    NgIf
  ],
  templateUrl: './all-products.component.html',
  styleUrl: './all-products.component.css'
})
export class AllProductsComponent implements OnInit{

  products: Product[] = [];

  expandedProducts: { [key: string]: boolean } = {};

  constructor(private adminService: AdminService) {
  }

  ngOnInit(): void {
    this.refreshProduct();
  }

  refreshProduct() {
    this.adminService.getAllProducts().subscribe({
      next: (products: Product[]) => {
        this.products = products;
      }, error: (err) => {
        console.error(err);
      }
    })
  }

  expandedColors: { [productId: string]: { [colorName: string]: boolean } } = {};


  toggleProduct(product: Product) {
    this.expandedProducts[product.Id] = !this.expandedProducts[product.Id];
  }

  toggleColor(product: Product, color: any) {
    if (!this.expandedColors[product.Id]) {
      this.expandedColors[product.Id] = {};
    }
    this.expandedColors[product.Id][color.Name] = !this.expandedColors[product.Id][color.Name];
  }

  isColorExpanded(product: Product, color: any): boolean {
    return this.expandedColors[product.Id] && this.expandedColors[product.Id][color.Name];
  }
}
