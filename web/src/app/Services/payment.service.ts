import { Injectable } from '@angular/core';
import {environment} from "../../Environments/environment";
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {Observable} from "rxjs";
import {CookieService} from "ngx-cookie-service";
import {PaymentRequestModel} from "../Requests/paymentrequest";

export interface PaymentRequest {
  productName: string;
  amount: number;
  quantity: number;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient, private cookieService: CookieService) { }

  createStripeSession(request: PaymentRequestModel): Observable<any> {

    const token = this.cookieService.get('uiAppToken');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    return this.http.post<any>(
      `${this.baseUrl}/payment/create-checkout-session`,
      request,
      { headers }
    );
  }

  verifyPayment(sessionId: string): Observable<any> {
    const token = this.cookieService.get('uiAppToken');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    return this.http.post<any>(
      `${this.baseUrl}/payment/verify-payment?sessionId=${sessionId}`,
      {},
      { headers }
    );
  }
}
