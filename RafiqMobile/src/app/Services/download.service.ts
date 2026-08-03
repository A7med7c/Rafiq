import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { Filesystem, Directory } from '@capacitor/filesystem';
import { Share } from '@capacitor/share';

@Injectable({ providedIn: 'root' })
export class DownloadService {

  /**
   * Downloads a Blob as a named file.
   * - Web: triggers browser download via a temporary object URL (existing behavior).
   * - Mobile (Capacitor): writes the file to app cache, then opens the native share sheet
   *   so the user can save to Files / Downloads or open in any app.
   */
  async download(blob: Blob, filename: string): Promise<void> {
    if (!Capacitor.isNativePlatform()) {
      this.webDownload(blob, filename);
      return;
    }
    await this.mobileDownload(blob, filename);
  }

  private webDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }

  private async mobileDownload(blob: Blob, filename: string): Promise<void> {
    const base64 = await this.blobToBase64(blob);
    const { uri } = await Filesystem.writeFile({
      path: filename,
      data: base64,
      directory: Directory.Cache,
      recursive: true,
    });
    await Share.share({
      title: filename,
      url: uri,
      dialogTitle: filename,
    });
  }

  private blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => {
        const dataUrl = reader.result as string;
        resolve(dataUrl.split(',')[1]);
      };
      reader.onerror = reject;
      reader.readAsDataURL(blob);
    });
  }
}
