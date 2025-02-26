import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {Tag} from "../Models/tag.model";
import {catchError, Observable, throwError} from "rxjs";
import {environment} from "../../Environments/environment";
import {CookieService} from "ngx-cookie-service";
import {Design} from "../Models/design.model";
import {Product} from "../Models/product.model";

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  constructor(
    private http: HttpClient,
    private cookieService: CookieService
  ) { }

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

  getAllDesigns(): Observable<Design[]> {
    const url = `${environment.apiUrl}/admin/design/getAll`;

    return this.http.get<Design[]>(url);
  }

  updateDesign(design: Design): Observable<Design> {
    const url = `${environment.apiUrl}/admin/tag/one-update`;
    return this.http.put<Design>(url, design);
  }

  createDesign(design: Design): Observable<Design> {
    const url = `${environment.apiUrl}/admin/tag/create`;
    return this.http.post<Design>(url, design);
  }

  deleteDesign(designs: Design[]): Observable<{ message: string }> {
    const url = `${environment.apiUrl}/admin/tag/remove`;

    return this.http.request<{ message: string }>('DELETE', url, { body: designs });
  }

  getAllProducts(): Observable<Product[]> {
    const url = `${environment.apiUrl}/admin/product/getAll`;

    return this.http.get<Product[]>(url);
  }
}
