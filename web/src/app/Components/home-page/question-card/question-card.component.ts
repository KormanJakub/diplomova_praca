import { Component } from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";

@Component({
  selector: 'app-question-card',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule
  ],
  templateUrl: './question-card.component.html',
  styleUrl: './question-card.component.css'
})
export class QuestionCardComponent {

}
