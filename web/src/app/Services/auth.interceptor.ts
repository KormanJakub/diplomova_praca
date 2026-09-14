import {inject} from '@angular/core';
import {HttpInterceptorFn} from '@angular/common/http';
import {CookieService} from 'ngx-cookie-service';
import {environment} from '../../Environments/environment';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(CookieService).get('uiAppToken');
  if (!token || !request.url.startsWith(`${environment.apiUrl}/`) ||
      request.headers.has('Authorization')) {
    return next(request);
  }
  return next(request.clone({setHeaders: {Authorization: `Bearer ${token}`}}));
};
