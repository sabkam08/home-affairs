import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { API_CONFIG } from '../../../core/api/api-config';
import {
  DocumentReadinessResponse,
  DocumentResponse
} from '../models/document.models';

@Injectable({
  providedIn: 'root'
})
export class DocumentApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(API_CONFIG);

  list(applicationId: string) {
    return this.http.get<DocumentResponse[]>(
      `${this.config.baseUrl}/applications/${applicationId}/documents`
    );
  }

  upload(
    applicationId: string,
    documentType: string,
    file: File
  ) {
    const form = new FormData();

    form.append('DocumentType', documentType);

    // File retains browser-supplied MIME type.
    form.append('File', file, file.name);

    return this.http.post<DocumentResponse>(
      `${this.config.baseUrl}/applications/${applicationId}/documents`,
      form
    );
  }

  download(
    applicationId: string,
    documentId: string
  ) {
    return this.http.get(
      `${this.config.baseUrl}/applications/${applicationId}/documents/${documentId}/content`,
      {
        responseType: 'blob',
        observe: 'response'
      }
    );
  }

  versions(
    applicationId: string,
    documentId: string
  ) {
    return this.http.get<DocumentResponse[]>(
      `${this.config.baseUrl}/applications/${applicationId}/documents/${documentId}/versions`
    );
  }

  replace(
    applicationId: string,
    document: DocumentResponse,
    file: File
  ) {
    const form = new FormData();

    form.append(
      'RowVersion',
      document.rowVersion
    );

    form.append(
      'File',
      file,
      file.name
    );

    return this.http.post<DocumentResponse>(
      `${this.config.baseUrl}/applications/${applicationId}/documents/${document.documentId}/versions`,
      form
    );
  }

  delete(
    applicationId: string,
    document: DocumentResponse
  ) {
    return this.http.delete<void>(
      `${this.config.baseUrl}/applications/${applicationId}/documents/${document.documentId}`,
      {
        body: {
          rowVersion: document.rowVersion
        }
      }
    );
  }

  readiness(applicationId: string) {
    return this.http.get<DocumentReadinessResponse>(
      `${this.config.baseUrl}/applications/${applicationId}/document-readiness`
    );
  }

  extractText(
    applicationId: string,
    documentId: string
  ) {
    return this.http.post<unknown>(
      `${this.config.baseUrl}/applications/${applicationId}/documents/${documentId}/extract-text`,
      {}
    );
  }

  getText(
    applicationId: string,
    documentId: string
  ) {
    return this.http.get<unknown>(
      `${this.config.baseUrl}/applications/${applicationId}/documents/${documentId}/text`
    );
  }

  search(
    applicationId: string,
    query: string
  ) {
    return this.http.get<unknown>(
      `${this.config.baseUrl}/applications/${applicationId}/document-search`,
      {
        params: {
          q: query
        }
      }
    );
  }
}