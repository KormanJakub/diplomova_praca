import {Component, OnInit} from '@angular/core';
import {Color, Product, Size} from "../../../../Models/product.model";
import {TableModule} from "primeng/table";
import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, NgIf} from "@angular/common";
import {AdminService} from "../../../../Services/admin.service";
import {MessageService} from "primeng/api";
import {Button} from "primeng/button";
import {ToastModule} from "primeng/toast";
import {ToolbarModule} from "primeng/toolbar";
import {DialogModule} from "primeng/dialog";
import {FormsModule} from "@angular/forms";
import {FileService} from "../../../../Services/file.service";
import { Tag } from '../../../../Models/tag.model';
import {environment} from "../../../../../Environments/environment";
import {DropdownModule} from "primeng/dropdown";
import {InputTextareaModule} from "primeng/inputtextarea";
import {ChipsModule} from "primeng/chips";

@Component({
  selector: 'app-all-products',
  standalone: true,
  imports: [
    TableModule,
    NgClass,
    CurrencyPipe,
    DatePipe,
    NgIf,
    Button,
    ToastModule,
    ToolbarModule,
    DialogModule,
    FormsModule,
    DropdownModule,
    InputTextareaModule,
    ChipsModule,
    DecimalPipe
  ],
  templateUrl: './all-products.component.html',
  styleUrl: './all-products.component.css',
  providers: [
    MessageService
  ]
})
export class AllProductsComponent implements OnInit{

  tags: Tag[] = [];
  newProduct: Product = {} as Product;
  products: Product[] = [];
  selectedProducts: Product[] = [];
  productDialog: boolean = false;
  currentProduct: Product = {} as Product;

  createProductDialog: boolean = false;

  selectedTag!: Tag;
  selectedNewTag!: Tag;

  selectedColorImage: File | null = null;

  expandedProducts: { [key: string]: boolean } = {};
  expandedColors: { [productId: string]: { [colorName: string]: boolean } } = {};

  colorDialog: boolean = false;
  colorDialogHeader: string = '';
  currentColor: Color = {} as Color;
  editingColor: boolean = false;

  sizeDialog: boolean = false;
  sizeDialogHeader: string = '';
  currentSize: Size = {} as Size;
  editingSize: boolean = false;
  currentColorForSize: Color = {} as Color;

  constructor(
    private adminService: AdminService,
    private messageService: MessageService,
    private fileService: FileService
  ) {}

  ngOnInit(): void {
    this.refreshProduct();

    this.adminService.getAllTags().subscribe({
      next: (tags: Tag[]) => {
        this.tags = tags;
      },
      error: (err) => {
        console.error(err);
      }
    })
  }

  refreshProduct() {
    this.adminService.getAllProducts().subscribe({
      next: (products: Product[]) => {
        this.products = products;
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  onTagChange(event: any): void {
    if (event.value) {
      this.currentProduct.TagId = event.value.Id;
      this.currentProduct.TagName = event.value.Name;
    }
  }

  onTagCreate(event: any): void {
    if (event.value) {
      this.newProduct.TagId = event.value.Id;
      this.newProduct.TagName = event.value.Name;
    }
  }

  toggleProduct(product: Product) {
    this.expandedProducts[product.Id] = !this.expandedProducts[product.Id];
  }

  toggleColor(product: Product, color: Color) {
    if (!this.expandedColors[product.Id]) {
      this.expandedColors[product.Id] = {};
    }
    this.expandedColors[product.Id][color.Name] = !this.expandedColors[product.Id][color.Name];
  }

  isColorExpanded(product: Product, color: Color): boolean {
    return this.expandedColors[product.Id] && this.expandedColors[product.Id][color.Name];
  }

  openNewColor(product: Product) {
    this.currentProduct = product;
    this.currentColor = { Name: '', FileId: null, PathOfFile: null, Sizes: [] };
    this.editingColor = false;
    this.colorDialogHeader = 'Pridať farbu';
    this.colorDialog = true;
  }

  openEditColor(product: Product, color: Color) {
    this.currentProduct = product;
    this.currentColor = { ...color, Sizes: [...(color.Sizes || [])] };
    this.editingColor = true;
    this.colorDialogHeader = 'Upraviť farbu';
    this.colorDialog = true;
  }

  saveColor() {
    if (this.selectedColorImage) {
      const formData = new FormData();
      formData.append('Files', this.selectedColorImage);

      this.fileService.uploadFile(formData).subscribe({
        next: (res: any) => {
          this.currentColor.FileId = res.id;
          this.currentColor.PathOfFile = res.path || '';
          this.continueSaveColor();
        },
        error: (err) => {
          console.error(err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Chyba pri uploadovaní obrázku', life: 3000 });
        }
      });
    } else {
      this.continueSaveColor();
    }
  }

  openEditProduct(product: Product) {
    this.currentProduct = { ...product };
    this.productDialog = true;
  }

  hideProductDialog() {
    this.productDialog = false;
  }

  saveProduct() {
    this.adminService.updateProduct(this.currentProduct).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Produkt aktualizovaný',
          life: 3000
        });
        this.productDialog = false;
        this.refreshProduct();
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Chyba pri aktualizácii produktu',
          life: 3000
        });
      }
    });
  }

  createProduct() {
    this.adminService.createProduct(this.newProduct, this.newProduct.TagId).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Produkt vytvorený',
          life: 3000
        });
        this.createProductDialog = false;
        this.refreshProduct();
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Chyba pri vytvorení produktu',
          life: 3000
        });
      }
    });
  }


  hideCreateProductDialog() {
    this.createProductDialog = false;
  }

  continueSaveColor() {
    if (this.editingColor) {
      const index = this.currentProduct.Colors.findIndex(c => c.Name === this.currentColor.Name);
      if (index !== -1) {
        this.currentProduct.Colors[index] = this.currentColor;
      }
      this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Farba upravená', life: 3000 });
    } else {
      this.currentProduct.Colors.push(this.currentColor);
      this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Farba pridaná', life: 3000 });
    }
    this.colorDialog = false;
    this.selectedColorImage = null;
    this.updateProductColors(this.currentProduct);
  }

  deleteColor(product: Product, color: Color) {
    this.adminService.removeColorForSpecificId(product.Id, color.Name).subscribe({
      next: () => {
        product.Colors = product.Colors.filter(c => c.Name !== color.Name);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Farba odstránená', life: 3000 });
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Chyba pri odstraňovaní farby', life: 3000 });
      }
    });
  }

  hideColorDialog() {
    this.colorDialog = false;
  }

  openNewSize(product: Product, color: Color) {
    this.currentProduct = product;
    this.currentColorForSize = color;
    this.currentSize = { Size: '', Quantity: 0 };
    this.editingSize = false;
    this.sizeDialogHeader = 'Pridať veľkosť';
    this.sizeDialog = true;
  }

  openEditSize(product: Product, color: Color, size: Size) {
    this.currentProduct = product;
    this.currentColorForSize = color;
    this.currentSize = { ...size };
    this.editingSize = true;
    this.sizeDialogHeader = 'Upraviť veľkosť';
    this.sizeDialog = true;
  }

  saveSize() {
    if (!this.currentColorForSize.Sizes) {
      this.currentColorForSize.Sizes = [];
    }

    if (this.editingSize) {
      const index = this.currentColorForSize.Sizes.findIndex(s => s.Size === this.currentSize.Size);
      if (index !== -1) {
        this.currentColorForSize.Sizes[index] = this.currentSize;
      }
      this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Veľkosť upravená', life: 3000 });
    } else {
      this.currentColorForSize.Sizes.push(this.currentSize);
      this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Veľkosť pridaná', life: 3000 });
    }
    this.sizeDialog = false;
    this.updateProductColors(this.currentProduct);
  }

  onImageSelected(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      this.selectedColorImage = event.target.files[0];
    }
  }

  openNewProduct() {
    this.newProduct = {} as Product;
    this.createProductDialog = true;
  }

  deleteSize(product: Product, color: Color, size: Size) {
    this.adminService.removeSizeForColorOfSpecificId(product.Id, color.Name, size.Size).subscribe({
      next: () => {
        color.Sizes = color.Sizes.filter(s => s.Size !== size.Size);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Veľkosť odstránená', life: 3000 });
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Chyba pri odstraňovaní veľkosti', life: 3000 });
      }
    });
  }

  hideSizeDialog() {
    this.sizeDialog = false;
  }

  updateProductColors(product: Product) {
    this.adminService.updateProduct(product).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Produkt aktualizovaný', life: 3000 });
        this.refreshProduct();
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Chyba pri aktualizácii produktu', life: 3000 });
      }
    });
  }

  protected readonly environment = environment;

  deleteSelectedProduct(product: any) {
    this.adminService.removeProduct(product.Id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Produkt odstránený',
          life: 3000
        });
        this.products = this.products.filter(p => p.Id !== product.Id);
        this.refreshProduct();
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Chyba pri odstraňovaní produktu',
          life: 3000
        });
      }
    });
  }
}
