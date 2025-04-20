import {Component, OnInit} from '@angular/core';
import {PublicService} from "../../../Services/public.service";
import {Gallery} from "../../../Models/gallery.model";
import {environment} from "../../../../Environments/environment";
import {NgForOf} from "@angular/common";
import {animate, style, transition, trigger} from "@angular/animations";
import {interval, Subscription} from "rxjs";

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

  currentIndex = 0;

  get translatePercent(): number {
    return (this.currentIndex * 100) / this.slidesPerView;
  }

  slidesPerView = 4;

  private autoplaySub?: Subscription;

  constructor(private publicService: PublicService) {}

  ngOnInit(): void {
    this.publicService.galleryShowCaseForHome().subscribe({
      next: (g) => {
        this.gallery = g;
        this.setupSlidesPerView();
        this.startAutoplay();
      },
      error: (err) => console.error(err)
    });

    window.addEventListener('resize', this.setupSlidesPerView.bind(this));
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.setupSlidesPerView.bind(this));
    this.autoplaySub?.unsubscribe();
  }

  private setupSlidesPerView() {
    const w = window.innerWidth;
    if (w <= 400)      this.slidesPerView = 1;
    else if (w <= 600) this.slidesPerView = 2;
    else if (w <= 900) this.slidesPerView = 3;
    else               this.slidesPerView = 4;
  }

  next(): void {
    if (!this.gallery.length) return;
    this.currentIndex =
      this.currentIndex < this.gallery.length - this.slidesPerView
        ? this.currentIndex + 1
        : 0;
  }

  prev(): void {
    if (!this.gallery.length) return;
    this.currentIndex =
      this.currentIndex > 0
        ? this.currentIndex - 1
        : this.gallery.length - this.slidesPerView;
  }

  startAutoplay() {
    this.autoplaySub?.unsubscribe();
    this.autoplaySub = interval(3000).subscribe(() => this.next());
  }

  pauseAutoplay() {
    this.autoplaySub?.unsubscribe();
  }
}
