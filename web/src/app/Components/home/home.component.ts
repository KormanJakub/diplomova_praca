import { Component } from '@angular/core';
import {HeaderComponent} from "../header/header.component";
import {CarouselModule} from "primeng/carousel";
import {FirstCardComponent} from "../home-page/first-card/first-card.component";
import {BestSaleComponent} from "../home-page/best-sale/best-sale.component";
import {SecondCardComponent} from "../home-page/second-card/second-card.component";
import {ThirdCardComponent} from "../home-page/third-card/third-card.component";
import {GaleryCardComponent} from "../home-page/galery-card/galery-card.component";
import {ReviewCardComponent} from "../home-page/review-card/review-card.component";
import {StepCardComponent} from "../home-page/step-card/step-card.component";
import {StartCardComponent} from "../home-page/start-card/start-card.component";
import {QuestionCardComponent} from "../home-page/question-card/question-card.component";
import {QuestionsByCardComponent} from "../home-page/questions-by-card/questions-by-card.component";
import {FooterComponent} from "../footer/footer.component";

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    HeaderComponent,
    CarouselModule,
    FirstCardComponent,
    BestSaleComponent,
    SecondCardComponent,
    ThirdCardComponent,
    GaleryCardComponent,
    ReviewCardComponent,
    StepCardComponent,
    StartCardComponent,
    QuestionCardComponent,
    QuestionsByCardComponent,
    FooterComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {

}
