import {Component, OnInit} from '@angular/core';
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {ActivatedRoute} from "@angular/router";
import {CookieService} from "ngx-cookie-service";
import {PaymentService} from "../../../Services/payment.service";

@Component({
  selector: 'app-cancellationoforder',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent
  ],
  templateUrl: './cancellationoforder.component.html',
  styleUrl: './cancellationoforder.component.css'
})
export class CancellationoforderComponent implements OnInit {

  cancellationToken: string = "";

  constructor(
    private route: ActivatedRoute,
    private cookieService: CookieService,
    private paymentService: PaymentService,
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.cancellationToken = params['cancellationToken'];
    });

    this.paymentService.cancelOrder(this.cancellationToken).subscribe();

    this.cookieService.delete('CartCustomizations');
  }
}
