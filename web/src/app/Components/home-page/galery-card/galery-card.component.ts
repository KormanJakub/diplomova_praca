import {Component, OnInit} from '@angular/core';
import {PublicService} from "../../../Services/public.service";
import {Gallery} from "../../../Models/gallery.model";
import {environment} from "../../../../Environments/environment";
import {NgForOf} from "@angular/common";
import {animate, style, transition, trigger} from "@angular/animations";

@Component({
  selector: 'app-galery-card',
  standalone: true,
  imports: [
    NgForOf
  ],
  animations: [
    trigger('fadeInSide', [
      transition('* => next', [
        style({ opacity: 0, transform: 'translateX(100%)' }),
        animate('2s ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
      ]),
      transition('* => prev', [
        style({ opacity: 0, transform: 'translateX(-100%)' }),
        animate('2s ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
      ])
    ])
  ],
  templateUrl: './galery-card.component.html',
  styleUrl: './galery-card.component.css'
})
export class GaleryCardComponent implements OnInit {

  gallery: Gallery[] = [];
  environment = environment;
  currentIndex: number = 0;
  direction: 'next' | 'prev' | '' = '';

  constructor(
    private publicService: PublicService
  ) {}

  ngOnInit(): void {
    // @ts-ignore
    this.publicService.galleryShowCaseForHome().subscribe({
      next: (gallery) => {
        this.gallery = gallery;
      },
      error: (err) => {
        console.log(err);
      }
    })
  }

  get visibleImages() {
    return this.gallery.slice(this.currentIndex, this.currentIndex + 4);
  }

  next(): void {
    this.direction = 'next';
    if (this.currentIndex < this.gallery.length - 4) {
      this.currentIndex++;
    } else {
      this.currentIndex = 0;
    }
  }

  prev(): void {
    this.direction = 'prev';
    if (this.currentIndex > 0) {
      this.currentIndex--;
    } else {
      this.currentIndex = this.gallery.length - 4;
    }
  }

}
