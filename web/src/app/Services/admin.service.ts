import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {Tag} from "../Models/tag.model";
import {catchError, Observable, throwError} from "rxjs";
import {environment} from "../../Environments/environment";
import {CookieService} from "ngx-cookie-service";
import {Design} from "../Models/design.model";
import {Product} from "../Models/product.model";
import {AllPairedDesignsResponse, PairedDesign} from "../Models/paired-design.model";
import {NewPairedDesign} from "../Models/new-paired-design.model";
import {Order} from "../Models/order.model";
import {Customization} from "../Models/customization.model";

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  constructor(
    private http: HttpClient,
    private cookieService: CookieService
  ) { }

  //TAG
  getAllTags(): Observable<Tag[]> {
    const token = this.cookieService.get('uiAppToken');
    const url = `${environment.apiUrl}/admin/tag/getAll`;

    const headers = new HttpHeaders()
      .set('Authorization', `Bearer ${token}`);

    return this.http.get<Tag[]>(url, {
      headers
    });
  }

  updateTags(tag: Tag): Observable<Tag> {
    const url = `${environment.apiUrl}/admin/tag/one-update`;
    return this.http.put<Tag>(url, tag);
  }

  createTag(tag: Tag): Observable<Tag> {
    const url = `${environment.apiUrl}/admin/tag/create`;
    return this.http.post<Tag>(url, tag);
  }

  deleteTags(tags: Tag[]): Observable<{ message: string }> {
    const url = `${environment.apiUrl}/admin/tag/remove`;

    return this.http.request<{ message: string }>('DELETE', url, { body: tags });
  }

  //DESIGN
  getAllDesigns(): Observable<Design[]> {
    const url = `${environment.apiUrl}/admin/design/getAll`;

    return this.http.get<Design[]>(url);
  }

  getSpecificDesign(designIds: string[]): Observable<Design[]> {
    const url = `${environment.apiUrl}/admin/design/getSpecific`;
    return this.http.post<Design[]>(url, designIds);
  }

  getSalesSummaryAll(): Observable<{
    soldOrders: number,
    pendingOrders: number,
    makingOrders: number,
    readyOrders: number,
    sendOrders: number,
    cancelOrders: number
  }> {
    return this.http.get<{
      soldOrders: number,
      pendingOrders: number,
      makingOrders: number,
      readyOrders: number,
      sendOrders: number,
      cancelOrders: number
    }>(`${environment.apiUrl}/admin/orders/sales-summary`);
  }

  getLowStockProducts(): Observable<any[]> {
    return this.http.get<any[]>(
      `${environment.apiUrl}/admin/products/low-stock`
    );
  }

  updateDesign(design: Design): Observable<Design> {
    const url = `${environment.apiUrl}/admin/design/update`;
    return this.http.put<Design>(url, design);
  }

  createDesign(design: Design): Observable<Design> {
    const url = `${environment.apiUrl}/admin/design/create`;
    return this.http.post<Design>(url, design);
  }

  deleteDesign(designs: Design[]): Observable<{ message: string }> {
    const url = `${environment.apiUrl}/admin/design/remove`;

    return this.http.request<{ message: string }>('DELETE', url, { body: designs });
  }

  //PRODUCT
  getAllProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${environment.apiUrl}/admin/product/getAll`);
  }

  getProductsByTag(tagId: string): Observable<Product[]> {
    return this.http.get<Product[]>(`${environment.apiUrl}/admin/product/by/${tagId}`);
  }

  createProduct(product: Product, tagId: string): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/admin/product/create/${tagId}`, product);
  }

  updateProduct(product: Product): Observable<any> {
    return this.http.put<any>(`${environment.apiUrl}/admin/product/update`, product);
  }

  removeProduct(productId: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiUrl}/admin/product/remove/${productId}`);
  }

  removeColorForSpecificId(productId: string, color: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiUrl}/admin/product/remove-color/${productId}/${color}`);
  }

  removeSizeForColorOfSpecificId(productId: string, color: string, size: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiUrl}/admin/product/remove-size/${productId}/${color}/${size}`);
  }

  //PAIRED-DESIGN
  getAllPairedDesigns(): Observable<AllPairedDesignsResponse> {
    return this.http.get<AllPairedDesignsResponse>(`${environment.apiUrl}/admin/design/all-paired-designs`);
  }

  createPairedDesigns(newPair: NewPairedDesign): Observable<PairedDesign> {
    return this.http.post<PairedDesign>(`${environment.apiUrl}/admin/design/pair-two-designs`, newPair);
  }

  deletePairedDesigns(ids: string[]): Observable<any> {
    return this.http.request<any>('delete', `${environment.apiUrl}/admin/design/delete-pair-design`, {
      body: ids
    });
  }

  //ORDERS
  getAllOrders(): Observable<{ Orders: Order[], Customization: any[] }> {
    return this.http.get<{ Orders: Order[], Customization: any[] }>(`${environment.apiUrl}/admin/orders`);
  }

  getOrderInformation(orderId: number): Observable<{
    order: Order,
    customizations: Customization[],
    products: Product[],
    designs: Design[]}> {
    return this.http.get<{
      order: Order,
      customizations: Customization[],
      products: Product[],
      designs: Design[]}>(`${environment.apiUrl}/admin/orders/${orderId}`);
  }

  increaseOrderStatus(orderId: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/admin/orders/increase-status/${orderId}`, {});
  }

  decreaseOrderStatus(orderId: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/admin/orders/decrease-status/${orderId}`, {});
  }

  cancelOrder(orderId: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/admin/orders/cancel/${orderId}`, {});
  }

  removeOrder(orderId: number): Observable<any> {
    return this.http.delete(`${environment.apiUrl}/admin/orders/${orderId}`);
  }

  updateOrder(orderId: number, updatedOrder: Order): Observable<any> {
    return this.http.put(`${environment.apiUrl}/admin/orders/${orderId}`, updatedOrder);
  }

  getKpiData(): Observable<{
    totalOrders: number,
    totalRevenue: number,
    averageOrderValue: number,
    newCustomers: number
  }> {
    return this.http.get<{
      totalOrders: number,
      totalRevenue: number,
      averageOrderValue: number,
      newCustomers: number
    }>(`${environment.apiUrl}/admin/kpi`);
  }

  getUserInformation(userId : string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/admin/user/${userId}`);
  }
}
