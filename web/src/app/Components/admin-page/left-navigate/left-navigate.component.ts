import {Component, OnInit} from '@angular/core';
import {MenuModule} from "primeng/menu";
import { MenuItem } from 'primeng/api';
import {BadgeModule} from "primeng/badge";
import {NgIf} from "@angular/common";

@Component({
  selector: 'app-left-navigate',
  standalone: true,
  imports: [
    MenuModule,
    BadgeModule,
    NgIf
  ],
  templateUrl: './left-navigate.component.html',
  styleUrl: './left-navigate.component.css'
})
export class LeftNavigateComponent implements OnInit{
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
              routerLink: "/admin/orders"
            },
          ]
        },
        {
          label: 'Tagy',
          items: [
            {
              label: 'Všetky',
              icon: 'pi pi-search',
              routerLink: "/admin/tags"
            },
          ]
        },
        {
          label: 'Produkty',
          items: [
            {
              label: 'Všetky',
              icon: 'pi pi-search',
              routerLink: "/admin/products"
            },
            ]
        },
        {
          label: 'Dizajn',
          items: [
            {
              label: 'Všetky',
              icon: 'pi pi-search',
              routerLink: "/admin/designs"
            },
          ]
        },
        {
          label: 'Párové dizajny',
          items: [
            {
              label: 'Všetky',
              icon: 'pi pi-search',
              routerLink: "/admin/paired-designs"
            }
          ]
        },
        {
          label: 'Galéria',
          items: [
            {
              label: 'Všetky',
              icon: 'pi pi-search',
              routerLink: "/admin/gallery"
            },
          ]
        },
        {
          separator: true
        }
      ];
    }

}
