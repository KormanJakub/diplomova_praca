import { Component } from '@angular/core';
import {Router, RouterLink} from "@angular/router";
import {AuthService} from "../../Services/auth.service";
import {DecodingTokenService} from "../../Services/decoding-token.service";
import {Button} from "primeng/button";
import {NgIf} from "@angular/common";
import {BadgeModule} from "primeng/badge";

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    Button,
    RouterLink,
    NgIf,
    BadgeModule
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent {
  //TODO: Keď budu ostatné komponenty, nech je to ready

  isAdmin: boolean = this.authService.isAdminLoggedIn();
  isLogged: boolean = this.authService.isLoggedIn();

  constructor(private router: Router, private authService: AuthService) {}

  userProfileId() {
    /*TODO:
     - Dekoduj token
     - returni id uživateľa
     */
  }

  routeToUserProfile () {
    //TODO: Az ked bude uzivateľský komponent
  }

  routeToUserOrders() {
    //TODO: Az ked bude uzivateľský komponent
  }

  routeToAdminProfile() {
    //TODO: Az ked bude admin komponent
  }


}
