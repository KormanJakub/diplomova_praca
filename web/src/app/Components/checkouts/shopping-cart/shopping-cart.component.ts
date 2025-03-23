import {Component, OnInit} from '@angular/core';
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {TableModule} from "primeng/table";
import {CurrencyPipe, NgIf} from "@angular/common";
import {Button, ButtonDirective} from "primeng/button";
import {ToastModule} from "primeng/toast";
import {MessageService} from "primeng/api";
import {ActivatedRoute, Router} from "@angular/router";

@Component({
  selector: 'app-shopping-cart',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent,
    TableModule,
    NgIf,
    ButtonDirective,
    CurrencyPipe,
    Button,
    ToastModule
  ],
  templateUrl: './shopping-cart.component.html',
  styleUrl: './shopping-cart.component.css',
  providers: [MessageService]
})
export class ShoppingCartComponent implements OnInit {

  cookieName: string = 'CartCustomizations';
  cookieValue: string = '';
  productData: any[] = [];

  constructor(
    private messageService: MessageService,
    private router: Router) {}

  ngOnInit(): void {
    this.cookieValue = this.getCookieValue(this.cookieName);

    if (this.cookieValue) {
      const decoded = decodeURIComponent(this.cookieValue);
      this.productData = JSON.parse(decoded);
    }
  }

  private getCookieValue(name: string): string {
    const cookieArr = document.cookie.split(';');
    for (let cookie of cookieArr) {
      const [key, value] = cookie.trim().split('=');
      if (key === name) {
        return value;
      }
    }
    return '';
  }

  private setCookieValue(name: string, value: string, days: number = 7): void {
    const encodedValue = encodeURIComponent(value);
    let expires = '';
    if (days) {
      const date = new Date();
      date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
      expires = `; expires=${date.toUTCString()}`;
    }
    document.cookie = `${name}=${encodedValue}${expires}; path=/`;
  }

  deleteItem(item: any): void {
    const index = this.productData.indexOf(item);
    if (index >= 0) {
      this.productData.splice(index, 1);
      this.setCookieValue(this.cookieName, JSON.stringify(this.productData));
    }

    this.messageService.add({
      severity: 'success',
      summary: 'Úspech',
      detail: 'Produkt odstranený z košíka!'
    });
  }

  get totalProductPrice(): number {
    return this.productData?.reduce((acc, item) => acc + Number(item.ProductPrice), 0) || 0;
  }

  get totalDesignPrice(): number {
    return this.productData?.reduce((acc, item) => acc + Number(item.DesignPrice), 0) || 0;
  }

  get totalUserDescPrice(): number {
    return this.productData?.reduce((acc, item) => acc + Number(item.UserDescriptionPrice), 0) || 0;
  }

  get totalOverall(): number {
    return this.totalProductPrice + this.totalDesignPrice + this.totalUserDescPrice;
  }

  continueToCheckout() {
    this.router.navigate(['/check-out']);
  }

}
