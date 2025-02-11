import { Component } from '@angular/core';
import {DividerModule} from "primeng/divider";

@Component({
  selector: 'app-first-card',
  standalone: true,
  imports: [
    DividerModule
  ],
  templateUrl: './first-card.component.html',
  styleUrl: './first-card.component.css'
})
export class FirstCardComponent {

}
