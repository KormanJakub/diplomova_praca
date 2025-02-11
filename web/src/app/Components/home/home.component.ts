import { Component } from '@angular/core';
import {HeaderComponent} from "../header/header.component";
import {CarouselModule} from "primeng/carousel";
import {FirstCardComponent} from "../home-page/first-card/first-card.component";

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    HeaderComponent,
    CarouselModule,
    FirstCardComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {

}
