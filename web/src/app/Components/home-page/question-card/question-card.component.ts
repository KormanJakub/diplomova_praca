import {Component, ElementRef, ViewChild} from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import {FormsModule} from "@angular/forms";
import {InputTextareaModule} from "primeng/inputtextarea";
import {NgForOf, NgIf} from "@angular/common";
import {DialogModule} from "primeng/dialog";

@Component({
  selector: 'app-question-card',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule,
    InputTextareaModule,
    FormsModule,
    NgIf,
    NgForOf,
    DialogModule
  ],
  templateUrl: './question-card.component.html',
  styleUrl: './question-card.component.css'
})
export class QuestionCardComponent {
  name = '';
  email = '';
  message = '';
  files: File[] = [];
  dragOver = false;

  displayDialog = false;

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.dragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.dragOver = false;
  }

  onFileDrop(event: DragEvent) {
    event.preventDefault();
    this.dragOver = false;
    if (event.dataTransfer?.files.length) {
      this.addFiles(Array.from(event.dataTransfer.files));
    }
  }

  onFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length) {
      this.addFiles(Array.from(input.files));
    }
  }

  addFiles(newFiles: File[]) {
    newFiles.forEach(file => {
      if (!this.files.some(f => f.name === file.name && f.size === file.size)) {
        this.files.push(file);
      }
    });
  }

  removeFile(index: number) {
    this.files.splice(index, 1);
  }

  onSubmit() {
    this.displayDialog = true;
  }

  onDialogClose() {
    this.displayDialog = false;
    this.clearForm();
  }

  clearForm() {
    this.name = '';
    this.email = '';
    this.message = '';
    this.files = [];
  }
}
