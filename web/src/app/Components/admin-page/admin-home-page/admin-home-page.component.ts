import { Component } from '@angular/core';
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {LeftNavigateComponent} from "../left-navigate/left-navigate.component";
import {
  AdminImportantInformationsComponent
} from "../admin-important-informations/admin-important-informations.component";
import {RouterOutlet} from "@angular/router";
import {UserLeftNavigateComponent} from "../../user-page/user-left-navigate/user-left-navigate.component";

@Component({
  selector: 'app-admin-home-page',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent,
    LeftNavigateComponent,
    AdminImportantInformationsComponent,
    RouterOutlet,
    UserLeftNavigateComponent
  ],
  templateUrl: './admin-home-page.component.html',
  styleUrl: './admin-home-page.component.css'
})
export class AdminHomePageComponent {

}
