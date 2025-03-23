import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {environment} from "../../Environments/environment";
import {Observable} from "rxjs";
import {CookieService} from "ngx-cookie-service";

@Injectable({
  providedIn: 'root'
})
export class CheckoutService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient, private cookieService: CookieService) { }

  createCustomizations(customizations: any[]): Observable<any> {
    const token = this.cookieService.get('uiAppToken');

    const headers = new HttpHeaders()
      .set('Authorization', `Bearer ${token}`);

    return this.http.post<any>(`${this.baseUrl}/user/make-customization`, customizations, { headers });
  }

  createOrder(customizationIds: string[]): Observable<any> {
    const token = this.cookieService.get('uiAppToken');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
    return this.http.post<any>(
      `${this.baseUrl}/user/make-order`,
      JSON.stringify(customizationIds),
      { headers }
    );
  }

  createGuestCustomizations(customizations: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/guest/make-customization-without-register`, customizations);
  }

  createGuestOrder(orderRequest: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/guest/make-order-without-register`, orderRequest);
  }

  createStripeSession(paymentRequest: { orderId: number, amount: number }): Observable<any> {
    const token = this.cookieService.get('uiAppToken');

    const headers = new HttpHeaders()
      .set('Authorization', `Bearer ${token}`);

    return this.http.post<any>(`${this.baseUrl}/payment/create-checkout-session`, paymentRequest, { headers });
  }
}
