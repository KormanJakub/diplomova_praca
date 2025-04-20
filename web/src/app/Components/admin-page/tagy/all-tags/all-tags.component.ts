import {Component, OnInit} from '@angular/core';
import {Tag} from "../../../../Models/tag.model";
import {AdminService} from "../../../../Services/admin.service";
import {ToastModule} from "primeng/toast";
import {ToolbarModule} from "primeng/toolbar";
import {Button} from "primeng/button";
import {TableModule} from "primeng/table";
import {DatePipe} from "@angular/common";
import {MessageService} from "primeng/api";
import {Router} from "@angular/router";
import {DialogModule} from "primeng/dialog";
import {FormsModule} from "@angular/forms";
import {ChipsModule} from "primeng/chips";

@Component({
  selector: 'app-all-tags',
  standalone: true,
  imports: [
    ToastModule,
    ToolbarModule,
    Button,
    TableModule,
    DatePipe,
    DialogModule,
    FormsModule,
    ChipsModule
  ],
  templateUrl: './all-tags.component.html',
  styleUrl: './all-tags.component.css',
  providers: [
    MessageService
  ]
})
export class AllTagsComponent implements OnInit {

  tag: any = {};
  tags: Tag[] = [];
  selectedTags: Tag[] = [];
  tagDialog: boolean = false;
  tagDialogHeader: string = '';

  constructor(
    private adminService: AdminService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.refreshTags();
  }

  refreshTags() {
    this.adminService.getAllTags().subscribe({
      next: (tags: Tag[]) => {
        this.tags = tags;
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  openNew() {
    this.tag = {};
    this.tagDialogHeader = 'Nový Tag';
    this.tagDialog = true;
  }

  hideDialog() {
    this.tagDialog = false;
  }

  editTag(tag: any) {
    this.tag = { ...tag };
    this.tagDialogHeader = 'Uprav Tag';
    this.tagDialog = true;
  }

  submitTag() {
    if (this.tag.Id) {
      this.adminService.updateTags(this.tag).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Tag Updated', life: 3000 });
          this.refreshTags();
        },
        error: (err) => {
          console.error(err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Tag Update Failed', life: 3000 });
        }
      })
    } else {
      this.adminService.createTag(this.tag).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Tag Created', life: 3000 });
          this.refreshTags();
        },
        error: (err) => {
          console.error(err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Tag Creation Failed', life: 3000 });
        }
      })
    }
    this.tagDialog = false;
  }

  deleteSelectedTags() {
    this.adminService.deleteTags(this.selectedTags).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Successful', detail: 'Tag(s) Deleted', life: 3000 });
        this.refreshTags();
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Deletion Failed', life: 3000 });
      }
    });
  }
}
