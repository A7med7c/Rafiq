import { Injectable, inject } from '@angular/core';
import { ActionSheet, ActionSheetButtonStyle } from '@capacitor/action-sheet';
import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';
import { LocalizationService } from './localization.service';
import { Capacitor } from '@capacitor/core';

export interface MediaPickerOptions {
  allowDocuments?: boolean;
  accept?: string;
}

@Injectable({ providedIn: 'root' })
export class MediaPickerService {
  private readonly l10n = inject(LocalizationService);

  /**
   * Prompts the user to select a media source (Camera, Gallery, File).
   * Returns a File object if successful, or null if cancelled/failed.
   */
  async selectMedia(options?: MediaPickerOptions): Promise<File | null> {
    try {
      const t = this.l10n.t().common;
      
      // On web, just fallback to native file picker immediately
      if (!Capacitor.isNativePlatform()) {
        return this.pickFile(options?.accept);
      }
      
      const actionOptions = [
        { title: t.mediaPickerTakePhoto },
        { title: t.mediaPickerChooseGallery }
      ];
      
      if (options?.allowDocuments) {
        actionOptions.push({ title: t.mediaPickerChooseFile });
      }
      
      actionOptions.push({ title: t.mediaPickerCancel });
      
      const result = await ActionSheet.showActions({
        title: t.mediaPickerTitle,
        options: actionOptions
      });

      const selectedIndex = result.index;

      // Cancel button is the last option
      if (selectedIndex === actionOptions.length - 1) {
        return null;
      }

      if (selectedIndex === 0) {
        return await this.takePhoto(CameraSource.Camera);
      } else if (selectedIndex === 1) {
        return await this.takePhoto(CameraSource.Photos);
      } else if (selectedIndex === 2 && options?.allowDocuments) {
        return await this.pickFile(options?.accept);
      }
      
      return null;
    } catch (err) {
      console.error('Media selection failed', err);
      // Fallback in case action sheet fails
      return this.pickFile(options?.accept);
    }
  }

  private async takePhoto(source: CameraSource): Promise<File | null> {
    try {
      const photo = await Camera.getPhoto({
        quality: 60,
        width: 1200,
        allowEditing: false,
        resultType: CameraResultType.Uri,
        source: source
      });

      if (!photo.webPath) {
        return null;
      }

      const response = await fetch(photo.webPath);
      const blob = await response.blob();
      
      const ext = photo.format === 'jpeg' ? 'jpg' : (photo.format || 'jpg');
      const filename = `photo_${new Date().getTime()}.${ext}`;
      
      return new File([blob], filename, { type: blob.type || `image/${ext}` });
    } catch (e: any) {
      // User cancelled or permission denied
      return null;
    }
  }

  private pickFile(accept = 'image/*,application/pdf'): Promise<File | null> {
    return new Promise((resolve) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = accept;
      input.style.display = 'none';
      
      input.onchange = (e: any) => {
        const file = e.target.files?.[0];
        if (file) {
          resolve(file);
        } else {
          resolve(null);
        }
        document.body.removeChild(input);
      };
      
      input.oncancel = () => {
        resolve(null);
        document.body.removeChild(input);
      };

      document.body.appendChild(input);
      input.click();
    });
  }
}
