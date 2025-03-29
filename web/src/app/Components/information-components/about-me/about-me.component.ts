import { Component } from '@angular/core';
import {FooterComponent} from "../../footer/footer.component";
import {GaleryCardComponent} from "../../home-page/galery-card/galery-card.component";
import {HeaderComponent} from "../../header/header.component";
import {SecondCardComponent} from "../../home-page/second-card/second-card.component";
import {ThirdCardComponent} from "../../home-page/third-card/third-card.component";

@Component({
  selector: 'app-about-me',
  standalone: true,
  imports: [
    FooterComponent,
    GaleryCardComponent,
    HeaderComponent,
    SecondCardComponent,
    ThirdCardComponent
  ],
  templateUrl: './about-me.component.html',
  styleUrl: './about-me.component.css'
})
export class AboutMeComponent {

}
