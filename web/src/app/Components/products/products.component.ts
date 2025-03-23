import { Component } from '@angular/core';
import {HeaderComponent} from "../header/header.component";
import {FooterComponent} from "../footer/footer.component";
import {GaleryCardComponent} from "../home-page/galery-card/galery-card.component";
import {QuestionsByCardComponent} from "../home-page/questions-by-card/questions-by-card.component";
import {AllProductsComponent} from "./all-products/all-products.component";
import {LeftNavigateComponent} from "../admin-page/left-navigate/left-navigate.component";
import {RouterOutlet} from "@angular/router";
import {FilterProductComponent} from "./filter-product/filter-product.component";

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent,
    GaleryCardComponent,
    QuestionsByCardComponent,
    AllProductsComponent,
    LeftNavigateComponent,
    RouterOutlet,
    FilterProductComponent
  ],
  templateUrl: './products.component.html',
  styleUrl: './products.component.css'
})
export class ProductsComponent {

}
