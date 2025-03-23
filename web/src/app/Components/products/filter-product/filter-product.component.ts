import {Component, OnInit} from '@angular/core';
import { Tag } from '../../../Models/tag.model';
import {PublicService} from "../../../Services/public.service";
import {BadgeModule} from "primeng/badge";
import {MenuModule} from "primeng/menu";
import {NgIf} from "@angular/common";
import {MenuItem} from "primeng/api";
import {Router} from "@angular/router";

@Component({
  selector: 'app-filter-product',
  standalone: true,
  imports: [
    BadgeModule,
    MenuModule,
    NgIf
  ],
  templateUrl: './filter-product.component.html',
  styleUrl: './filter-product.component.css'
})
export class FilterProductComponent implements OnInit {

  tags: Tag[] = [];
  items: MenuItem[] | undefined;

  constructor(
    private publicService: PublicService,
    private router: Router,
  )
  {}

  ngOnInit() {
    this.refreshTags();
  }

  refreshTags() {
    this.publicService.allTags().subscribe({
      next: (tags: Tag[]) => {
        this.tags = tags;

        this.items = [
          {
            separator: true
          },
          {
            label: 'Ponúkané produkty',
            items: this.tags.map(tag => ({
              label: tag.Name,
              icon: 'pi pi-search',
              routerLink: '',
              command: () => {
                this.router.navigate(['/products'], {queryParams: {tagName: tag.Name}});
              }
            }))
          }
        ]
      }
    })
  }
}
