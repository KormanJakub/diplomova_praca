import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from "@angular/router";
import {CookieService} from "ngx-cookie-service";
import {FooterComponent} from "../../footer/footer.component";
import {HeaderComponent} from "../../header/header.component";
import {PaymentService} from "../../../Services/payment.service";

@Component({
  selector: 'app-third-page-checkout',
  standalone: true,
  imports: [
    FooterComponent,
    HeaderComponent
  ],
  templateUrl: './third-page-checkout.component.html',
  styleUrl: './third-page-checkout.component.css'
})
export class ThirdPageCheckoutComponent implements OnInit  {
  orderId: string = "";

  constructor(
    private route: ActivatedRoute,
    private cookieService: CookieService,
    private paymentService: PaymentService,
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.orderId = params['order_id'];
    });

    this.paymentService.confirmPayment(this.orderId).subscribe();

    this.cookieService.delete('CartCustomizations');
  }
}
