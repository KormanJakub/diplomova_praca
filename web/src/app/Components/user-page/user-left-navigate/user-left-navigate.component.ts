import {Component, OnInit} from '@angular/core';
import {BadgeModule} from "primeng/badge";
import {MenuModule} from "primeng/menu";
import {NgIf} from "@angular/common";
import {MenuItem} from "primeng/api";

@Component({
  selector: 'app-user-left-navigate',
  standalone: true,
  imports: [
    BadgeModule,
    MenuModule,
    NgIf
  ],
  templateUrl: './user-left-navigate.component.html',
  styleUrl: './user-left-navigate.component.css'
})
export class UserLeftNavigateComponent implements OnInit {

  items: MenuItem[] | undefined;

  ngOnInit(): void {
    this.items = [
      {
        separator: true
      },
      {
        label: 'Objednávky',
        items: [
          {
            label: 'Všetky',
            icon: 'pi pi-search',
            routerLink: "/user/my-orders"
          },
        ]
      },
      {
        label: 'Moj profil',
        items: [
          {
            label: 'Nastavania',
            icon: 'pi pi-cog',
            routerLink: "/user"
          },
        ]
      },
      {
        separator: true
      }
    ];
  }
}
