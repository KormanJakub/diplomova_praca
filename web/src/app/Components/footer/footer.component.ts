import {Component, NgModule} from '@angular/core';
import {ExtraOptions, RouterLink, RouterModule, Routes} from "@angular/router";
import {FormsModule} from "@angular/forms";
import {PublicService} from "../../Services/public.service";
import {ButtonModule} from "primeng/button";
import {DialogModule} from "primeng/dialog";

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    DialogModule,
    ButtonModule
  ],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.css'
})
export class FooterComponent {
  email = '';
  displaySuccess = false;
  displayError = false;
  errorMessage = '';

  constructor(
    private publicService: PublicService
  ) {}

  receiveNews() {
    this.publicService.receiveNews(this.email).subscribe({
      next: () => {
        this.displaySuccess = true;
      },
      error: err => {
        this.errorMessage = err.error?.error || 'Neznáma chyba';
        this.displayError = true;
      }
    });
  }

  closeDialogs() {
    this.displaySuccess = this.displayError = false;
  }
}
