import {Component, OnInit} from '@angular/core';
import {Button} from "primeng/button";
import {CurrencyPipe, DatePipe} from "@angular/common";
import {DialogModule} from "primeng/dialog";
import {Footer, MessageService, PrimeTemplate} from "primeng/api";
import {PaginatorModule} from "primeng/paginator";
import {TableModule} from "primeng/table";
import {ToastModule} from "primeng/toast";
import {ToolbarModule} from "primeng/toolbar";
import {FileService} from "../../../../Services/file.service";
import {environment} from "../../../../../Environments/environment";
import {FileModel} from "../../../../Models/file.model";

@Component({
  selector: 'app-all-gallery',
  standalone: true,
  imports: [
    Button,
    CurrencyPipe,
    DatePipe,
    DialogModule,
    Footer,
    PaginatorModule,
    PrimeTemplate,
    TableModule,
    ToastModule,
    ToolbarModule
  ],
  templateUrl: './all-gallery.component.html',
  styleUrl: './all-gallery.component.css',
  providers: [
    MessageService
  ]
})
export class AllGalleryComponent implements OnInit {
  file: any = {};
  files: FileModel[] = [];
  selectedFile: FileModel[] = [];

  selectedFileImage: File | null = null;

  fileDialog: boolean = false;
  fileDialogHeader: string = '';

  constructor(
    private messageService: MessageService,
    private fileService: FileService
  ) { }


    ngOnInit(): void {
      this.refreshGallery();
    }

    refreshGallery() {
      this.fileService.getFiles().subscribe({
        next: (file: FileModel[]) => {
          this.files = file;
        }, error: (err) => {
          console.error(err);
        }
      })
    }

    openNew() {
      this.file = {};
      this.fileDialogHeader = 'Nový obrázok';
      this.fileDialog = true;
    }

  deleteSelectedGallery() {
    this.fileService.deleteFiles(this.selectedFile).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'File(s) Vymazaný', life: 3000 });
        this.refreshGallery();
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Deletion Failed', life: 3000 });
      }
    })
  }

  editGallery(gallery: any) {
    this.file = { ...gallery};
    this.fileDialogHeader = 'Uprav obrázok';
    this.fileDialog = true;
  }

  onImageSelected(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      this.selectedFileImage = event.target.files[0];
    }
  }

  hideDialog() {
    this.fileDialog = false;
  }

  saveGallery() {
    if (this.selectedFileImage) {
      const formData = new FormData();
      formData.append('Files', this.selectedFileImage);

      this.fileService.uploadFile(formData).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Design aktualizovaný', life: 3000 });
          this.refreshGallery();
        },
        error: (err) => {
          console.error(err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Chyba pri uploadovaní obrázku', life: 3000 });
        }
      });
    } else {

    }
  }

  protected readonly environment = environment;
}
