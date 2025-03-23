import {Component, OnInit} from '@angular/core';
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from "@angular/forms";
import {MessageService} from "primeng/api";
import {AuthService} from "../../../Services/auth.service";
import {Router} from "@angular/router";
import {CheckoutService} from "../../../Services/checkout.service";
import {CurrencyPipe, NgForOf, NgIf} from "@angular/common";
import {ToastModule} from "primeng/toast";
import {MessageModule} from "primeng/message";
import {Button} from "primeng/button";
import {RadioButtonModule} from "primeng/radiobutton";
import {InputTextModule} from "primeng/inputtext";
import {CustomizationRequest} from "../../../Requests/customizationrequest";
import {PaymentService} from "../../../Services/payment.service";
import {PaymentRequestModel} from "../../../Requests/paymentrequest";

@Component({
  selector: 'app-first-page-checkout',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent,
    CurrencyPipe,
    ToastModule,
    NgIf,
    MessageModule,
    Button,
    ReactiveFormsModule,
    InputTextModule,
    RadioButtonModule,
    FormsModule,
    NgForOf
  ],
  templateUrl: './first-page-checkout.component.html',
  styleUrl: './first-page-checkout.component.css',
  providers: [
    MessageService
  ]
})
export class FirstPageCheckoutComponent implements OnInit {

  billingForm!: FormGroup;
  isLoggedIn: boolean = false;
  userData: any = {};
  cartItems: any[] = [];
  totalOrderPrice: number = 0;
  selectedPaymentMethod: string = 'stripe';
  isProcessing: boolean = false;
  productData: any[] = [];
  cookieName: string = 'CartCustomizations';
  cookieValue: string = '';
  customRequests!: CustomizationRequest;

  constructor(
    private fb: FormBuilder,
    private messageService: MessageService,
    private checkoutService: CheckoutService,
    private authService: AuthService,
    private router: Router,
    private paymentService: PaymentService,
  ) {
    this.billingForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      address: ['', Validators.required],
      country: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      zip: ['', Validators.required],
      phoneNumber: ['', Validators.required]
    });
  }

  login() {
    this.router.navigate(['/login']);
  }

  ngOnInit(): void {
    this.authService.isLoggedInUser().subscribe(loggedIn => {
      this.isLoggedIn = loggedIn;
      if (this.isLoggedIn) {
        this.authService.getUserProfile().subscribe(user => {
          this.userData = user;
          this.billingForm.patchValue({
            firstName: user.firstName,
            lastName: user.lastName,
            address: user.address,
            country: user.country,
            email: user.email,
            zip: user.zip,
            phoneNumber: user.phoneNumber
          });
        });
      }
    });

    this.cookieValue = this.getCookieValue(this.cookieName);

    if (this.cookieValue) {
      const decoded = decodeURIComponent(this.cookieValue);
      this.productData = JSON.parse(decoded);
    }
  }

  calculateTotal(): void {
    this.totalOrderPrice = this.cartItems.reduce((acc, item) => {
      let productPrice = parseFloat(item.ProductPrice) || 0;
      let designPrice = parseFloat(item.DesignPrice) || 0;
      let userDescPrice = item.UserDescriptionPrice ? parseFloat(item.UserDescriptionPrice) : 0;
      return acc + productPrice + designPrice + userDescPrice;
    }, 0);
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

  onPaymentMethodChange(method: string): void {
    this.selectedPaymentMethod = method;
  }

  proceedToPayment(): void {
    if (this.billingForm.invalid) {
      this.messageService.add({ severity: 'error', summary: 'Chyba', detail: 'Vyplňte prosím všetky fakturačné údaje.' });
      return;
    }

    this.isProcessing = true;

    const customRequests: CustomizationRequest[] = this.productData.map(item => ({
      DesignId: item.DesignId,
      ProductId: item.ProductId,
      UserDescription: item.UserDescription,
      ProductColorName: item.ProductColorName,
      ProductSize: item.ProductSize
    }));

    // Krok 1: Vytvorenie customizácií na BE
    this.checkoutService.createCustomizations(customRequests).subscribe(customResponse => {
      // Z customResponse extrahujeme pole successCustomizations
      const successCustomizations = customResponse.SuccessCustomization;
      // Z každého objektu vyberieme iba jeho Id
      const customizationIds = successCustomizations.map((cust: any) => cust.Id);

      // Krok 2: Vytvorenie objednávky s customizáciami
      this.checkoutService.createOrder(customizationIds).subscribe(orderResponse => {
        // Krok 3: Podľa zvolenej platobnej metódy spustíme príslušný endpoint
        if (this.selectedPaymentMethod === 'stripe') {
          const paymentRequest: PaymentRequestModel = {
            ProductName: "Objednávka #" + orderResponse.orderId, // prípadne uprav podľa potrieb, napr. z prvého produktu
            Amount: this.totalOverall,
            Quantity: 1
          };

          this.paymentService.createStripeSession(paymentRequest).subscribe((paymentResponse: any) => {
            // Presmerovanie na Stripe checkout URL
            window.location.href = paymentResponse.url;
          }, err => {
            this.isProcessing = false;
            this.messageService.add({
              severity: 'error',
              summary: 'Chyba',
              detail: 'Chyba pri vytváraní Stripe platby.'
            });
          });
        } else {
          // Ak je zvolená platba na IBAN, navigujeme na príslušnú stránku
          this.router.navigate(['/iban-payment'], { queryParams: { orderId: orderResponse.orderId } });
        }
      }, err => {
        this.isProcessing = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Chyba',
          detail: 'Chyba pri vytváraní objednávky.'
        });
      });
    }, err => {
      this.isProcessing = false;
      this.messageService.add({
        severity: 'error',
        summary: 'Chyba',
        detail: 'Chyba pri vytváraní customizácie.'
      });
    });
  }
}

