import {Component, OnInit} from '@angular/core';
import {HeaderComponent} from "../../header/header.component";
import {FooterComponent} from "../../footer/footer.component";
import {GaleryCardComponent} from "../../home-page/galery-card/galery-card.component";
import {Product} from "../../../Models/product.model";
import {Design} from "../../../Models/design.model";
import {ActivatedRoute} from "@angular/router";
import {PublicService} from "../../../Services/public.service";
import {CurrencyPipe, NgForOf, SlicePipe} from "@angular/common";
import {environment} from "../../../../Environments/environment";
import {FormsModule} from "@angular/forms";
import {QuestionsByCardComponent} from "../../home-page/questions-by-card/questions-by-card.component";
import {CookieService} from "ngx-cookie-service";
import {CustomizationRequest} from "../../../Requests/customizationrequest";
import {MessageService} from "primeng/api";
import {ToastModule} from "primeng/toast";

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [
    HeaderComponent,
    FooterComponent,
    GaleryCardComponent,
    CurrencyPipe,
    NgForOf,
    FormsModule,
    SlicePipe,
    QuestionsByCardComponent,
    ToastModule
  ],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css',
  providers: [MessageService]
})
export class ProductDetailComponent implements OnInit {

  customReq!: CookiesCustomizationRequest;
  customReqs: CookiesCustomizationRequest[] = [];
  product!: Product;
  designs: Design[] = [];

  selectedDesignId: string = '';
  selectedSize: string = '';
  selectedCustom!: string;

  constructor(
    private route: ActivatedRoute,
    private publicService: PublicService,
    private cookiesService: CookieService,
    private messageService: MessageService
  ) {}

  ngOnInit() {
    this.loadProductDetail();
    this.loadDesigns();
  }

  loadProductDetail() {
    const id = this.route.snapshot.paramMap.get('id');
    const color = this.route.snapshot.queryParamMap.get('color') || '';
    if (id) {
      this.publicService.productDetail(id, color).subscribe({
        next: (prod: Product) => {
          this.product = prod;
        },
        error: err => {
          console.error('Chyba pri načítavaní produktu:', err);
        }
      });
    } else {
      console.error('Nebolo zadané ID produktu.');
    }
  }

  loadDesigns() {
    this.publicService.getDesigns().subscribe({
      next: (designs: Design[]) => {
        this.designs = designs;
      }
    })
  }

  selectDesign(designId: string): void {
    this.selectedDesignId = designId;
  }

  saveCustomization() {
    const id: string = this.route.snapshot.paramMap.get('id') ?? '';
    const color: string = this.route.snapshot.queryParamMap.get('color') ?? '';

    const newCustomization: CookiesCustomizationRequest = {
      DesignId: this.selectedDesignId,
      DesignURL: '',
      DesignPrice: '',
      ProductId: id,
      ProductURL: '',
      ProductPrice: '',
      UserDescription: this.selectedCustom,
      ProductColorName: color,
      UserDescriptionPrice: '',
      ProductSize: this.selectedSize,
    };

    const selectedDesign = this.designs.find(d => d.Id === this.selectedDesignId);
    if (selectedDesign) {
      newCustomization.DesignURL = this.environment.apiUrl + selectedDesign.PathOfFile;
      newCustomization.DesignPrice = selectedDesign.Price.toString();
    }

    newCustomization.ProductURL = this.environment.apiUrl + this.product.Colors[0].PathOfFile;
    newCustomization.ProductPrice = this.product.Price.toString();

    if (newCustomization.UserDescription && newCustomization.UserDescription.trim() !== '') {
      newCustomization.UserDescriptionPrice = '2';
    }

    const existingCustomizations = this.cookiesService.get('CartCustomizations');
    if (existingCustomizations) {
      try {
        this.customReqs = JSON.parse(existingCustomizations);
      } catch (e) {
        this.customReqs = [];
      }
    } else {
      this.customReqs = [];
    }

    this.customReqs.push(newCustomization);

    const customReqsJson = JSON.stringify(this.customReqs);
    this.cookiesService.set('CartCustomizations', customReqsJson, 7, '/');

    this.messageService.add({
      severity: 'success',
      summary: 'Úspech',
      detail: 'Produkt pridaný do košíka!'
    });
  }

  protected readonly environment = environment;
}

export interface CookiesCustomizationRequest {
  DesignId: string;
  DesignURL: string;
  DesignPrice: string;
  ProductId: string;
  ProductURL: string;
  ProductPrice: string;
  UserDescription: string;
  UserDescriptionPrice?: string;
  ProductColorName: string;
  ProductSize: string;
}
