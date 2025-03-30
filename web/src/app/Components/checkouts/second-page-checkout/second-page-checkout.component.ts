import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from "@angular/router";
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {CookieService} from "ngx-cookie-service";
import {GuestService} from "../../../Services/guest.service";
import {Order} from "../../../Models/order.model";
import {environment} from "../../../../Environments/environment";

@Component({
  selector: 'app-second-page-checkout',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent
  ],
  templateUrl: './second-page-checkout.component.html',
  styleUrl: './second-page-checkout.component.css'
})
export class SecondPageCheckoutComponent implements OnInit {
  orderId: string | null = null;

  numOrderId!: number;

  order!: Order

  orderTrackingUrl = environment.feUrl + "/follow-order";

  constructor(
    private route: ActivatedRoute,
    private cookieService: CookieService,
    private guestService: GuestService,
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.orderId = params['orderId'];
      this.numOrderId = Number(params['orderId']);
    });

    this.cookieService.delete('CartCustomizations');
    this.loadOrderData();
  }

  loadOrderData(): void {
    this.guestService.orderInformation(this.numOrderId).subscribe(res => {
      this.order = res;
    })
  }
}
