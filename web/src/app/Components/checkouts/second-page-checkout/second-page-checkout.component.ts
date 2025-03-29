import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from "@angular/router";
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {CookieService} from "ngx-cookie-service";

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

  constructor(
    private route: ActivatedRoute,
    private cookieService: CookieService,
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.orderId = params['orderId'];
    });

    this.cookieService.delete('CartCustomizations');
  }
}
