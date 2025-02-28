import { Component } from '@angular/core';
import {FooterComponent} from "../../footer/footer.component";
import {HeaderComponent} from "../../header/header.component";
import {LeftNavigateComponent} from "../../admin-page/left-navigate/left-navigate.component";
import {RouterOutlet} from "@angular/router";
import {UserLeftNavigateComponent} from "../user-left-navigate/user-left-navigate.component";

@Component({
  selector: 'app-user-home-page',
  standalone: true,
  imports: [
    FooterComponent,
    HeaderComponent,
    LeftNavigateComponent,
    RouterOutlet,
    UserLeftNavigateComponent
  ],
  templateUrl: './user-home-page.component.html',
  styleUrl: './user-home-page.component.css'
})
export class UserHomePageComponent {

}
