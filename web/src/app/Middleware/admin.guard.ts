import { Injectable } from '@angular/core';
import {ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import { Observable } from 'rxjs';
import {AuthService} from "../Services/auth.service";

@Injectable({
  providedIn: 'root'
})

export class AdminGuard implements CanActivate {

  constructor(private auth: AuthService, private router: Router) { }

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    if (this.auth.isAdminLoggedIn()) {
      return true;
    } else {
      this.router.navigate(["/"]);
      return false;
    }
  }
}
