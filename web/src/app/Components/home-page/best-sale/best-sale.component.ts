import {Component, OnInit} from '@angular/core';
import {PublicService} from "../../../Services/public.service";
import {Product} from "../../../Models/product.model";
import {NgForOf, NgIf} from "@angular/common";
import {environment} from "../../../../Environments/environment";

@Component({
  selector: 'app-best-sale',
  standalone: true,
  imports: [
    NgForOf,
    NgIf
  ],
  templateUrl: './best-sale.component.html',
  styleUrl: './best-sale.component.css'
})
export class BestSaleComponent implements OnInit{

  products: Product[] = [];
  environment = environment;

  constructor(
    private publicService: PublicService
  ) {}

  ngOnInit(): void {
    // @ts-ignore
    this.publicService.bestThreeProducts().subscribe({
      next: (products) => {
        this.products = products;
      },
      error: (err) => {
        console.log(err);
      }
    })
  }

  data() {
    console.log(this.products);
  }
}
