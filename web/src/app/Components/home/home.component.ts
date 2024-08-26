import { Component } from '@angular/core';
import {HeaderComponent} from "../header/header.component";
import {FooterComponent} from "../footer/footer.component";
import {CarouselModule} from "primeng/carousel";
import {WelcomeComponent} from "../welcome/welcome.component";
import {ContactComponent} from "../contact/contact.component";

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent,
    CarouselModule,
    WelcomeComponent,
    ContactComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {

}
