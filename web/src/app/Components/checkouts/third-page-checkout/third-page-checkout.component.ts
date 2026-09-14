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
  status = 'Overujeme platbu…';

  constructor(
    private route: ActivatedRoute,
    private cookieService: CookieService,
    private paymentService: PaymentService,
  ) {}

  ngOnInit(): void {
    const sessionId = this.route.snapshot.queryParamMap.get('session_id');
    if (!sessionId) {
      this.status = 'Platbu sa nepodarilo overiť.';
      return;
    }
    this.paymentService.verifyPayment(sessionId).subscribe({
      next: () => {
        this.status = '✅ Objednávka bola úspešne zaplatená!';
        this.cookieService.delete('CartCustomizations');
      },
      error: () => this.status = 'Platbu sa nepodarilo overiť. Kontaktujte nás s údajmi zo Stripe.'
    });
  }
}
