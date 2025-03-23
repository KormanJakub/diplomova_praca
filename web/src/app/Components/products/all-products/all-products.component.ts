import {Component, OnInit} from '@angular/core';
import {PublicService} from "../../../Services/public.service";
import {ActivatedRoute, Router} from "@angular/router";
import {Product} from "../../../Models/product.model";
import {CommonModule, CurrencyPipe, NgForOf, NgIf, SlicePipe} from "@angular/common";
import {environment} from "../../../../Environments/environment";

@Component({
  selector: 'app-all-products',
  standalone: true,
  imports: [
    NgForOf,
    CommonModule,
    CurrencyPipe,
    SlicePipe,
    NgIf
  ],
  templateUrl: './all-products.component.html',
  styleUrl: './all-products.component.css'
})
export class AllProductsComponent implements OnInit {

  products: Product[] = [];
  tagName: string = "";

  constructor(
    private publicService: PublicService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit() {
    this.refreshProducts();
  }

  refreshProducts() {
    this.route.queryParams.subscribe(params => {

      if (params['tagName'] !== undefined ) {
        this.tagName = params['tagName'];
      } else {
        this.tagName = 'Všetky produkty';
      }

      this.publicService.filterProducts(params['tagName']).subscribe(data => {
        this.products = data;
      });
    });
  }

  redirectToProduct(productId: string, colorName: string): void {
    this.router.navigate(['/product', productId], { queryParams: { color: colorName } });
  }

  protected readonly environment = environment;
}
