import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {CookieService} from "ngx-cookie-service";
import {Observable} from "rxjs";
import {User} from "../Models/user.model";
import {environment} from "../../Environments/environment";
import {Tag} from "../Models/tag.model";
import {Customization} from "../Models/customization.model";
import {Order} from "../Models/order.model";
import {Design} from "../Models/design.model";
import {Product} from "../Models/product.model";

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private getAuthHeaders(): HttpHeaders {
    const token = this.cookieService.get('uiAppToken') || '';
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  constructor(
    private http: HttpClient,
    private cookieService: CookieService
  ) { }

  getUserProfile(): Observable<User> {
    const url = `${environment.apiUrl}/user/profile`;
    const headers = this.getAuthHeaders();

    return this.http.get<User>(url, {
      headers
    });
  }

  getMyCustomizations(): Observable<Customization[]> {
    const headers = this.getAuthHeaders();
    return this.http.get<Customization[]>(`${environment.apiUrl}/user/my-customizations`,
      { headers });
  }

  getOrders(): Observable<{
    orders: Order[],
    customizations: Customization[],
    designs: Design[],
    products: Product[]
  }> {
    const headers = this.getAuthHeaders();
    return this.http.get<{
      orders: Order[],
      customizations: Customization[],
      designs: Design[],
      products: Product[]
    }>(`${environment.apiUrl}/user/orders`, { headers });
  }

  getOrdersById(id: number): Observable<{
    order: Order,
    customizations: Customization[],
    designs: Design[],
    products: Product[],
    user: User
  }> {
    const headers = this.getAuthHeaders();
    return this.http.get<{
      order: Order,
      customizations: Customization[],
      designs: Design[],
      products: Product[],
      user: User
    }>(`${environment.apiUrl}/user/orders/${id}`, { headers });
  }


  makeOrder(customizationsIds: string[]): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.post(`${environment.apiUrl}/user/make-order`, customizationsIds, { headers });
  }

  cancelOrder(orderId: number): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.post(`${environment.apiUrl}/user/cancel-order/${orderId}`, {}, { headers });
  }

  updateUserProfile(user: User): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.put(`${environment.apiUrl}/user/update`, user, { headers });
  }

  removeUser(): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.delete(`${environment.apiUrl}/user/remove`, { headers });
  }

  makeCustomization(requests: any[]): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.post(`${environment.apiUrl}/user/make-customization`, requests, { headers });
  }
}
