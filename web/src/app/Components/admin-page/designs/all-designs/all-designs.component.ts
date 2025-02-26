import {Component, OnInit} from '@angular/core';
import {Design} from "../../../../Models/design.model";
import {AdminService} from "../../../../Services/admin.service";
import {Footer, MessageService, PrimeTemplate} from "primeng/api";
import {Button} from "primeng/button";
import {DatePipe} from "@angular/common";
import {DialogModule} from "primeng/dialog";
import {PaginatorModule} from "primeng/paginator";
import {TableModule} from "primeng/table";
import {ToastModule} from "primeng/toast";
import {ToolbarModule} from "primeng/toolbar";

@Component({
  selector: 'app-all-designs',
  standalone: true,
  imports: [
    Button,
    DatePipe,
    DialogModule,
    Footer,
    PaginatorModule,
    PrimeTemplate,
    TableModule,
    ToastModule,
    ToolbarModule
  ],
  templateUrl: './all-designs.component.html',
  styleUrl: './all-designs.component.css',
  providers: [
    MessageService
  ]
})
export class AllDesignsComponent implements OnInit{
  design: any = {};
  designs: Design[] = [];
  selectedDesigns: Design[] = [];

  designDialog: boolean = false;
  designDialogHeader: string = '';

  constructor(
    private adminService: AdminService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.refreshDesigns();
  }

  refreshDesigns() {
    this.adminService.getAllDesigns().subscribe({
      next: (designs: Design[]) => {
        this.designs = designs;
      }, error: (err) => {
        console.error(err);
      }
    });
  }

  openNew() {
    this.design = {};
    this.designDialogHeader = 'Novy dizajny';
    this.designDialog = true;
  }

  hideDialog() {
    this.designDialog = false;
  }

  editDesign(desing: any) {
    this.design = { ...desing};
    this.designDialogHeader = 'Uprav dizajn';
    this.designDialog = true;
  }

  submitDesign() {
    if (this.design.Id) {
      this.adminService.updateDesign(this.design).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Tag Updated', life: 3000 });
          this.refreshDesigns();
        },
        error: (err) => {
          console.error(err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Tag Update Failed', life: 3000 });
        }
      })
    } else {
      this.adminService.createDesign(this.design).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Tag Created', life: 3000 });
          this.refreshDesigns();
        },
        error: (err) => {
          console.error(err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Tag Creation Failed', life: 3000 });
        }
      })
    }
    this.designDialog = false;
  }

  deleteSelectedDesigns() {
    this.adminService.deleteDesign(this.selectedDesigns).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Tag(s) Deleted', life: 3000 });
        this.refreshDesigns();
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Deletion Failed', life: 3000 });
      }
    });
  }

}
