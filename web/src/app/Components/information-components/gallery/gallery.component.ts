import { Component } from '@angular/core';
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {GaleryCardComponent} from "../../home-page/galery-card/galery-card.component";

@Component({
  selector: 'app-gallery',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent,
    GaleryCardComponent
  ],
  templateUrl: './gallery.component.html',
  styleUrl: './gallery.component.css'
})
export class GalleryComponent {

}
