import {
  Component,
  Input,
  inject,
  signal
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { ErrorService } from '../../../core/api/error.service';
import { ApiError } from '../../../core/api/api-error';
import { DocumentApiService } from '../services/document-api.service';
import {
  ACCEPTED_DOCUMENT_TYPES,
  DOCUMENT_TYPES,
  DocumentReadinessResponse,
  DocumentResponse,
  documentTypeLabel,
  MAX_DOCUMENT_SIZE_BYTES,
  requirementSatisfied
} from '../models/document.models';

@Component({
  selector: 'app-application-documents',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  template: `
    <section class="mt-8 rounded-2xl border border-[#d8d2c4] bg-white p-6 shadow-sm">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 class="text-xl font-semibold text-slate-950">Supporting documents</h2>
          <p class="mt-1 text-sm text-slate-500">
            Upload clear PDF, DOCX, JPG or PNG files up to 15 MB.
          </p>
        </div>
        @if (readiness(); as status) {
          <span
            class="rounded-full px-3 py-1 text-xs font-semibold"
            [class.bg-[#d6eadb]]="status.isReady"
            [class.text-[#1f5132]]="status.isReady"
            [class.bg-amber-100]="!status.isReady"
            [class.text-amber-800]="!status.isReady">
            {{ status.isReady ? 'Ready to submit' : 'Documents outstanding' }}
          </span>
        }
      </div>

      @if (error(); as currentError) {
        <div class="mt-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          <p class="font-medium">{{ currentError.title }}</p>
          <p class="mt-1">{{ currentError.message }}</p>
        </div>
      }

      @if (readiness(); as status) {
        <div class="mt-6 grid gap-3 md:grid-cols-2">
          @for (requirement of status.requirements; track requirement.documentType) {
            <div class="rounded-xl border border-slate-200 p-4">
              <div class="flex items-center justify-between gap-3">
                <p class="text-sm font-semibold text-slate-900">
                  {{ requirement.displayName }}
                </p>
                <span
                  class="text-xs font-semibold"
                  [class.text-[#2f7b4b]]="isSatisfied(requirement)"
                  [class.text-amber-700]="!isSatisfied(requirement)">
                  {{ requirement.uploadedCount }}/{{ requirement.minimumCount }}
                </span>
              </div>
              <p class="mt-1 text-xs text-slate-500">{{ requirement.description }}</p>
            </div>
          }
        </div>
      }

      <div class="mt-6 rounded-xl bg-[#f7f4ed] p-4">
        <div class="flex flex-wrap items-end gap-3">
          <label class="min-w-52 flex-1">
            <span class="block text-xs font-semibold uppercase tracking-wide text-slate-600">
              Document type
            </span>
            <select
              class="mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
              [value]="selectedType()"
              (change)="selectedType.set(selectValue($event))">
              @for (type of documentTypes; track type) {
                <option [value]="type">{{ label(type) }}</option>
              }
            </select>
          </label>
          <label class="min-w-60 flex-[2]">
            <span class="block text-xs font-semibold uppercase tracking-wide text-slate-600">
              File
            </span>
            <input
              #fileInput
              type="file"
              [accept]="acceptedTypes"
              class="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm"
              (change)="selectedFile.set(fileInput.files?.[0] ?? null)">
          </label>
          <button
            type="button"
            (click)="upload()"
            [disabled]="uploading() || !selectedFile()"
            class="rounded-lg bg-[#1f5132] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#285c3a] disabled:opacity-50">
            {{ uploading() ? 'Uploading...' : 'Upload document' }}
          </button>
        </div>
        <p class="mt-2 text-xs text-slate-500">
          Accepted: PDF, DOCX, JPG and PNG. Maximum size: 15 MB.
        </p>
      </div>

      @if (documents().length === 0) {
        <p class="mt-6 rounded-xl border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
          No documents have been uploaded yet.
        </p>
      } @else {
        <div class="mt-6 divide-y divide-slate-100">
          @for (document of documents(); track document.documentId) {
            <div class="flex flex-wrap items-center justify-between gap-4 py-4">
              <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-2">
                  <p class="truncate font-medium text-slate-900">{{ document.fileName }}</p>
                  <span class="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                    v{{ document.versionNumber }}
                  </span>
                </div>
                <p class="mt-1 text-xs text-slate-500">
                  {{ label(document.documentType) }} ·
                  {{ document.fileSizeBytes | number }} bytes ·
                  {{ document.createdAtUtc | date:'mediumDate' }}
                </p>
              </div>
              <div class="flex flex-wrap gap-2">
                <button type="button" (click)="download(document)" class="rounded-lg border border-[#2f7b4b] px-3 py-2 text-sm font-medium text-[#1f5132] hover:bg-[#edf6ee]">
                  Download
                </button>
                <label class="cursor-pointer rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
                  Replace
                  <input type="file" class="hidden" [accept]="acceptedTypes" (change)="replace(document, $event)">
                </label>
                <button type="button" (click)="remove(document)" class="rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-50">
                  Remove
                </button>
              </div>
            </div>
          }
        </div>
      }
    </section>
  `
})
export class ApplicationDocumentsComponent {
  @Input({ required: true }) applicationId!: string;

  private readonly api = inject(DocumentApiService);
  private readonly errorService = inject(ErrorService);

  readonly documents = signal<DocumentResponse[]>([]);
  readonly readiness = signal<DocumentReadinessResponse | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly selectedType = signal<string>(DOCUMENT_TYPES[0]);
  readonly uploading = signal(false);
  readonly error = signal<ApiError | null>(null);

  readonly documentTypes = DOCUMENT_TYPES;
  readonly acceptedTypes = ACCEPTED_DOCUMENT_TYPES.join(',');
  readonly label = documentTypeLabel;

  constructor() {
  }

  isSatisfied = requirementSatisfied;

  ngOnInit(): void {
    this.reload();
  }

  selectValue(event: Event): string {
    return (event.target as HTMLSelectElement).value;
  }

  reload(): void {
    this.api.list(this.applicationId).subscribe({
      next: documents => this.documents.set(documents),
      error: error => this.showError(error)
    });
    this.api.readiness(this.applicationId).subscribe({
      next: readiness => this.readiness.set(readiness),
      error: error => this.showError(error)
    });
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file) return;
    if (!this.isValidFile(file)) {
      this.error.set({
        status: 400,
        title: 'File cannot be uploaded',
        message: 'Choose a PDF, DOCX, JPG or PNG file no larger than 15 MB.',
        validationErrors: {}
      });
      return;
    }

    this.mutate(this.api.upload(this.applicationId, this.selectedType(), file));
  }

  replace(document: DocumentResponse, event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (!this.isValidFile(file)) {
      this.error.set({
        status: 400,
        title: 'File cannot be uploaded',
        message: 'Choose a PDF, DOCX, JPG or PNG file no larger than 15 MB.',
        validationErrors: {}
      });
      return;
    }
    this.mutate(this.api.replace(this.applicationId, document, file));
  }

  remove(document: DocumentResponse): void {
    if (!window.confirm(`Remove ${document.fileName}?`)) return;
    this.uploading.set(true);
    this.error.set(null);
    this.api.delete(this.applicationId, document).pipe(
      finalize(() => this.uploading.set(false))
    ).subscribe({
      next: () => this.reload(),
      error: error => this.showError(error)
    });
  }

  download(file: DocumentResponse): void {
    this.api.download(this.applicationId, file.documentId).subscribe({
      next: response => {
        const url = URL.createObjectURL(response.body);
        const anchor = window.document.createElement('a');
        anchor.href = url;
        anchor.download = this.fileName(response.headers.get('content-disposition')) ?? file.fileName;
        anchor.click();
        URL.revokeObjectURL(url);
      },
      error: error => this.showError(error)
    });
  }

  private mutate(request: ReturnType<DocumentApiService['upload']>): void {
    this.uploading.set(true);
    this.error.set(null);
    request.pipe(finalize(() => this.uploading.set(false))).subscribe({
      next: () => {
        this.selectedFile.set(null);
        this.reload();
      },
      error: error => this.showError(error)
    });
  }

  private isValidFile(file: File): boolean {
    return file.size <= MAX_DOCUMENT_SIZE_BYTES &&
      ACCEPTED_DOCUMENT_TYPES.includes(file.type as typeof ACCEPTED_DOCUMENT_TYPES[number]);
  }

  private fileName(header: string | null): string | null {
    const match = header?.match(/filename="?([^"]+)"?/i);
    return match?.[1] ?? null;
  }

  private showError(error: HttpErrorResponse): void {
    this.error.set(this.errorService.fromHttpError(error));
  }
}
