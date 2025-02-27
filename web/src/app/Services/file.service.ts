import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {environment} from "../../Environments/environment";
import {Gallery} from "../Models/gallery.model";
import {FileModel} from "../Models/file.model";
import {Design} from "../Models/design.model";

@Injectable({
  providedIn: 'root'
})
export class FileService {

  constructor(
    private http: HttpClient
  ) { }

  uploadFile(formData: FormData): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/file/uploadFile`, formData);
  }

  getFiles(): Observable<any> {
    return this.http.get<FileModel[]>(`${environment.apiUrl}/file/getAll`);
  }

  deleteFiles(files: FileModel[]): Observable<{ message: string }> {
    const url = `${environment.apiUrl}/file/remove`;

    return this.http.request<{ message: string }>('DELETE', url, { body: files });
  }
}
