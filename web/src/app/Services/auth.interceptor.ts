import {HttpInterceptorFn} from '@angular/common/http';
import {environment} from '../../Environments/environment';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(`${environment.apiUrl}/`)) {
    return next(request);
  }

  // Authentication is carried only by the API's HttpOnly cookie. JavaScript
  // never receives or reads the bearer token.
  return next(request.clone({withCredentials: true}));
};
