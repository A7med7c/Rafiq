import { Injectable, inject } from '@angular/core';
import {
  Camera,
  CameraErrorCode,
  CameraResultType,
  CameraSource,
  Photo,
} from '@capacitor/camera';
import { Capacitor } from '@capacitor/core';
import { LocalizationService } from './localization.service';
import { NotificationService } from './notification.service';

export interface ImagePickerOptions {
  accept?: string;
}

@Injectable({ providedIn: 'root' })
export class ImagePickerService {
  private readonly notifications = inject(NotificationService);
  private readonly l10n = inject(LocalizationService);

  async pickImage(options: ImagePickerOptions = {}): Promise<File | null> {
    if (!Capacitor.isNativePlatform()) {
      return this.pickFromBrowser(options.accept);
    }

    try {
      const photo = await Camera.getPhoto({
        source: CameraSource.Prompt,
        resultType: CameraResultType.Uri,
        quality: 100,
        allowEditing: false,
        saveToGallery: false,
      });

      return await this.photoToFile(photo);
    } catch (error: unknown) {
      if (this.isCancellation(error)) {
        return null;
      }

      const copy = this.l10n.t().mediaAccess;
      if (this.isPermissionDenied(error)) {
        this.notifications.showToast(copy.imagePermissionTitle, copy.imagePermissionBody, 'error');
      } else {
        this.notifications.showToast(copy.imagePickerErrorTitle, copy.imagePickerErrorBody, 'error');
      }
      return null;
    }
  }

  private pickFromBrowser(accept = 'image/*'): Promise<File | null> {
    return new Promise<File | null>((resolve) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = accept;
      input.style.display = 'none';

      let settled = false;
      const finish = (file: File | null) => {
        if (settled) return;
        settled = true;
        window.removeEventListener('focus', onWindowFocus);
        input.remove();
        resolve(file);
      };
      const onWindowFocus = () => {
        window.setTimeout(() => finish(input.files?.[0] ?? null), 250);
      };

      input.addEventListener('change', () => finish(input.files?.[0] ?? null), { once: true });
      input.addEventListener('cancel', () => finish(null), { once: true });
      window.addEventListener('focus', onWindowFocus, { once: true });
      document.body.appendChild(input);
      input.click();
    });
  }

  private async photoToFile(photo: Photo): Promise<File> {
    const source = photo.webPath ?? (photo.path ? Capacitor.convertFileSrc(photo.path) : null);
    if (!source) {
      throw new Error('Camera returned no readable image path.');
    }

    const response = await fetch(source);
    if (!response.ok) {
      throw new Error(`Unable to read selected image (${response.status}).`);
    }

    const blob = await response.blob();
    const extension = this.normalizedExtension(photo.format, blob.type);
    const mimeType = blob.type || `image/${extension === 'jpg' ? 'jpeg' : extension}`;
    return new File([blob], `rafiq-image-${Date.now()}.${extension}`, {
      type: mimeType,
      lastModified: Date.now(),
    });
  }

  private normalizedExtension(format: string | undefined, mimeType: string): string {
    const value = (format || mimeType.split('/')[1] || 'jpeg').toLowerCase();
    if (value === 'jpeg' || value === 'jpg') return 'jpg';
    if (value === 'png' || value === 'webp' || value === 'gif') return value;
    return 'jpg';
  }

  private isCancellation(error: unknown): boolean {
    const code = this.errorCode(error);
    if (
      code === CameraErrorCode.TakePhotoCancelled ||
      code === CameraErrorCode.ChooseMediaCancelled ||
      code === CameraErrorCode.EditPhotoCancelled
    ) {
      return true;
    }

    return /cancel(?:led|ed)?/i.test(this.errorMessage(error));
  }

  private isPermissionDenied(error: unknown): boolean {
    const code = this.errorCode(error);
    if (
      code === CameraErrorCode.CameraPermissionDenied ||
      code === CameraErrorCode.GalleryPermissionDenied
    ) {
      return true;
    }

    return /permission|denied|not allowed/i.test(this.errorMessage(error));
  }

  private errorCode(error: unknown): string {
    if (typeof error === 'object' && error !== null && 'code' in error) {
      return String((error as { code?: unknown }).code ?? '');
    }
    return '';
  }

  private errorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    return String(error ?? '');
  }
}
