import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { RecruitmentProcessService } from '../../core/services/recruitment-process/recruitment-process-service';
import { SelectionRoundDto } from '../../core/models/selection-round-dto';

@Component({
  selector: 'app-round-editor-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, DragDropModule],
  templateUrl: './round-editor-dialog.component.html',
  styleUrls: ['./round-editor-dialog.component.scss']
})
export class RoundEditorDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<RoundEditorDialogComponent>);
  private recruitmentService = inject(RecruitmentProcessService);
  public data: { processId: string, rounds: SelectionRoundDto[] } = inject(MAT_DIALOG_DATA);

  form!: FormGroup;
  loading = signal<boolean>(false);
  processId: string = '';

  constructor() {
    this.processId = this.data.processId;
    this.form = this.fb.group({
      rounds: this.fb.array([])
    });

    if (this.data.rounds && this.data.rounds.length > 0) {
      this.data.rounds.forEach(r => this.addRound(r));
    } else {
      this.addRound(); // add one empty by default
    }
  }

  get rounds() {
    return this.form.get('rounds') as FormArray;
  }

  addRound(round?: SelectionRoundDto) {
    const roundGroup = this.fb.group({
      id: [round?.id || null],
      title: [round?.title || '', Validators.required],
      description: [round?.description || '']
    });
    this.rounds.push(roundGroup);
  }

  removeRound(index: number) {
    this.rounds.removeAt(index);
  }

  drop(event: CdkDragDrop<any[]>) {
    // moveItemInArray mutates the array in-place, but since form.controls is an array of form subgroups we can use it on form controls.
    const dir = event.currentIndex > event.previousIndex ? 1 : -1;
    
    // U FormArray ne radi obično pomeranje, moramo pomerati ručno
    const current = this.rounds.at(event.previousIndex);
    this.rounds.removeAt(event.previousIndex);
    this.rounds.insert(event.currentIndex, current);
  }

  save() {
    if (this.form.invalid) return;
    this.loading.set(true);
    
    // Map with order index
    const mappedRounds: SelectionRoundDto[] = this.rounds.value.map((r: any, index: number) => ({
        id: r.id,
        title: r.title,
        description: r.description,
        orderIndex: index + 1
    }));

    this.recruitmentService.updateRounds(this.processId, mappedRounds).subscribe({
      next: () => {
         this.loading.set(false);
         this.dialogRef.close(true); // Return true means success
      },
      error: (err) => {
         console.error('Error saving rounds', err);
         this.loading.set(false);
      }
    });
  }
}