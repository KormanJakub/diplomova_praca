import {Component, OnInit} from '@angular/core';
import {PublicService} from "../../../Services/public.service";
import {Product, Color} from "../../../Models/product.model";
import {DecimalPipe, NgForOf, NgIf} from "@angular/common";
import {environment} from "../../../../Environments/environment";
import {RouterLink} from "@angular/router";

@Component({
  selector: 'app-best-sale',
  standalone: true,
  imports: [
    NgForOf,
    NgIf,
    DecimalPipe,
    RouterLink
  ],
  templateUrl: './best-sale.component.html',
  styleUrls: ['./best-sale.component.css']
})
export class BestSaleComponent implements OnInit{
  products: Product[] = [];
  displayedProducts: Product[] = [];
  environment = environment;

  private placeholderPath = 'assets/placeholder.png';

  private placeholderColor: Color = {
    Name:     'Placeholder',
    FileId:   null,
    PathOfFile: this.placeholderPath,
    Sizes:    []
  };

  private placeholderProduct: Product = {
    Id:          '',
    TagId:       '',
    TagName:     '',
    Name:        '',
    Description: '',
    Colors:      [ this.placeholderColor ],
    Price:       0,
    CreatedAt:   new Date().toISOString(),
    UpdatedAt:   new Date().toISOString()
  };

  constructor(private publicService: PublicService) {}

  ngOnInit(): void {
    this.publicService.bestThreeProducts().subscribe({
      next: (prods) => {
        this.products = prods;
        this.updateDisplayedProducts();
      },
      error: (err) => {
        console.error(err);
        this.updateDisplayedProducts();
      }
    });
  }

  private updateDisplayedProducts(): void {
    const slice = (this.products || []).slice(0, 3);

    while (slice.length < 3) {
      slice.push({ ...this.placeholderProduct });
    }

    this.displayedProducts = slice;
  }

  imageUrl(product: Product): string {
    const p = product.Colors[0]?.PathOfFile;
    return p ? (p.startsWith('http') ? p : this.environment.apiUrl + p)
      : this.placeholderPath;
  }
}
