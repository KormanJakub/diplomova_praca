import {Component, OnInit} from '@angular/core';
import {CarouselModule} from "primeng/carousel";

@Component({
  selector: 'app-welcome',
  standalone: true,
  imports: [
    CarouselModule
  ],
  templateUrl: './welcome.component.html',
  styleUrl: './welcome.component.css'
})
export class WelcomeComponent implements OnInit {
  welcome: WelcomePhotos[] = [
    {
      image: 'login.jpg',
      title: 'Vlastný design!'
    },
    {
      image: 'regtister.WEBP',
      title: 'Párové mikiny!'
    },
    {
      image: 'fotka1.jpg',
      title: 'Mikiny obľúbených postavičiek!'
    },
    {
      image: 'fotka2.JPG',
      title: 'Rodinné mikiny!'
    },
    {
      image: 'fotka3.jpg',
      title: 'Krstné košielky!'
    },
  ]

  constructor() {}

  ngOnInit(): void {}
}

export interface WelcomePhotos {
  title: string;
  image: string;
}
