import {Component, OnInit} from '@angular/core';
import {AdminService} from "../../../../Services/admin.service";
import {Footer, MessageService, PrimeTemplate} from "primeng/api";
import {AllPairedDesignsResponse, PairedDesign, PairedDesignWithDesigns} from "../../../../Models/paired-design.model";
import {Design} from "../../../../Models/design.model";
import {CurrencyPipe, DatePipe, NgForOf, NgIf} from "@angular/common";
import {environment} from "../../../../../Environments/environment";
import {Button} from "primeng/button";
import {DialogModule} from "primeng/dialog";
import {PaginatorModule} from "primeng/paginator";
import {TableModule} from "primeng/table";
import {ToastModule} from "primeng/toast";
import {ToolbarModule} from "primeng/toolbar";

@Component({
  selector: 'app-all-paired-designs',
  standalone: true,
  imports: [
    NgForOf,
    Button,
    CurrencyPipe,
    DatePipe,
    DialogModule,
    Footer,
    PaginatorModule,
    PrimeTemplate,
    TableModule,
    ToastModule,
    ToolbarModule,
    NgIf
  ],
  templateUrl: './all-paired-designs.component.html',
  styleUrl: './all-paired-designs.component.css',
  providers: [
    MessageService
  ]
})
export class AllPairedDesignsComponent implements OnInit {

  pairedDesignsWithDesigns: PairedDesignWithDesigns[] = [];
  selectedPairedDesign: PairedDesign[] = [];

  pairedDesign: any = {};
  editDialog: boolean = false;
  pairedDesignDialogHeader: string = '';

  newDialog: boolean = false;
  newPairedDesign: any = { Name: '', Designs: [null, null] };

  availableDesigns: Design[] = [];

  constructor(
  private adminService: AdminService,
  private messageService: MessageService
  ) { }

  ngOnInit() {
    this.refreshPairedDesigns();
    this.loadAvailableDesigns();
  }

  refreshPairedDesigns() {
    this.adminService.getAllPairedDesigns().subscribe({
      next: (response: AllPairedDesignsResponse) => {
        this.pairedDesignsWithDesigns = response.PairedDesign.map(pd => ({
          ...pd,
          Designs: response.Design.filter(d => pd.DesignIds.includes(d.Id))
        }));
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  loadAvailableDesigns() {
    this.adminService.getAllDesigns().subscribe({
      next: (designs: Design[]) => {
        this.availableDesigns = designs;
      },
      error: (err) => {
        console.error('Error loading designs', err);
      }
    });
  }

  openNew() {
    this.newPairedDesign = { Name: '', Designs: [null, null] };
    this.newDialog = true;
  }

  editDesign(pairedDesign: any) {
    this.pairedDesign = { ...pairedDesign };
    this.pairedDesignDialogHeader = 'Uprav dizajn';
    this.editDialog = true;
  }

  closeEditDialog() {
    this.editDialog = false;
    this.pairedDesign = {};
  }

  closeNewDialog() {
    this.newDialog = false;
    this.newPairedDesign = { Name: '', Designs: [null, null] };
  }

  saveEdit() {
    console.log('Saving edited paired design:', this.pairedDesign);
    this.refreshPairedDesigns();
    this.editDialog = false;
  }

  saveNew() {
    const newPair = {
      Name: this.newPairedDesign.Name,
      DesignIds: [
        this.newPairedDesign.Designs[0] ? this.newPairedDesign.Designs[0].Id : null,
        this.newPairedDesign.Designs[1] ? this.newPairedDesign.Designs[1].Id : null
      ]
    };

    this.adminService.createPairedDesigns(newPair).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Párový dizajn bol vytvorený', life: 3000 });
        this.refreshPairedDesigns();
      }, error: (err) => {
        console.error("Error saving edited paired design:", err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Chyba pri vytváraniu páru', life: 3000 });
      }
    })

    this.refreshPairedDesigns();
    this.newDialog = false;
  }

  deleteSelectedPairedDesigns() {
    if (this.selectedPairedDesign && this.selectedPairedDesign.length > 0) {
      const ids = this.selectedPairedDesign.map(pd => pd.Id);
      this.adminService.deletePairedDesigns(ids).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Dizajn pár vymazaný', life: 3000 });
          this.refreshPairedDesigns();
        },
        error: (err) => {
          console.error('Error deleting paired designs:', err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Chyba pri vymazávaní páru', life: 3000 });
        }
      });
    }
  }


  protected readonly environment = environment;
}
