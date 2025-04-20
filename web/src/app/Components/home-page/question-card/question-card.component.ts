import { Component } from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import {FormsModule} from "@angular/forms";
import {InputTextareaModule} from "primeng/inputtextarea";

@Component({
  selector: 'app-question-card',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule,
    InputTextareaModule,
    FormsModule
  ],
  templateUrl: './question-card.component.html',
  styleUrl: './question-card.component.css'
})
export class QuestionCardComponent {

}
