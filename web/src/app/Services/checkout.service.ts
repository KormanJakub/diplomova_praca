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

  createOrder(
    customizationIds: string[],
    paymentMethod: string = 'Stripe',
    deliveryMethod: string = 'HomeDelivery',
    packetaPointId?: string,
    packetaPointName?: string,
    packetaPointAddress?: string
  ): Observable<any> {
    const token = this.cookieService.get('uiAppToken');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    let url = `${this.baseUrl}/user/make-order?paymentMethod=${encodeURIComponent(paymentMethod)}&deliveryMethod=${encodeURIComponent(deliveryMethod)}`;
    if (packetaPointId) {
      url += `&packetaPointId=${encodeURIComponent(packetaPointId)}`;
    }
    if (packetaPointName) {
      url += `&packetaPointName=${encodeURIComponent(packetaPointName)}`;
    }
    if (packetaPointAddress) {
      url += `&packetaPointAddress=${encodeURIComponent(packetaPointAddress)}`;
    }

    return this.http.post<any>(
      url,
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

}
