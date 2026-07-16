import { Injectable } from '@angular/core';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

@Injectable({
  providedIn: 'root',
})
export class PdfService {
  private async loadFonts(doc: jsPDF): Promise<void> {
    if (doc.getFontList()['Amiri']) {
      return;
    }
    try {
      const resNormal = await fetch('/Amiri-Regular.ttf');
      const bufNormal = await resNormal.arrayBuffer();
      const base64Normal = this.arrayBufferToBase64(bufNormal);
      doc.addFileToVFS('Amiri-Regular.ttf', base64Normal);
      doc.addFont('Amiri-Regular.ttf', 'Amiri', 'normal');

      const resBold = await fetch('/Amiri-Bold.ttf');
      const bufBold = await resBold.arrayBuffer();
      const base64Bold = this.arrayBufferToBase64(bufBold);
      doc.addFileToVFS('Amiri-Bold.ttf', base64Bold);
      doc.addFont('Amiri-Bold.ttf', 'Amiri', 'bold');
    } catch (e) {
      console.error('Failed to load Amiri fonts', e);
    }
  }

  private arrayBufferToBase64(buffer: ArrayBuffer): string {
    let binary = '';
    const bytes = new Uint8Array(buffer);
    const len = bytes.byteLength;
    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  private processTextForPdf(doc: jsPDF, text: string): string {
    if (!text) return '';
    const hasArabic = /[\u0600-\u06FF]/.test(text);
    if (!hasArabic) return text;

    const shaped = doc.processArabic(text);
    const reversed = shaped.split('').reverse().join('');

    const nonArabicRegex = /([^\u0600-\u06FF\uFB50-\uFDFF\uFE70-\uFEFF]+)/g;
    return reversed.replace(nonArabicRegex, (match) => {
      const chars = match.split('').reverse().map(c => {
        if (c === '(') return ')';
        if (c === ')') return '(';
        if (c === '[') return ']';
        if (c === ']') return '[';
        if (c === '{') return '}';
        if (c === '}') return '{';
        return c;
      });
      return chars.join('');
    });
  }

  private drawText(
    doc: jsPDF,
    text: string,
    x: number,
    y: number,
    options?: any
  ): void {
    if (!text) return;
    const processed = this.processTextForPdf(doc, text);
    const hasArabic = /[\u0600-\u06FF]/.test(text);

    if (hasArabic) {
      const pageWidth = doc.internal.pageSize.getWidth();
      const rightX = pageWidth - 15;
      doc.text(processed, rightX, y, { align: 'right', ...options });
    } else {
      doc.text(processed, x, y, options);
    }
  }

  private drawParagraph(
    doc: jsPDF,
    text: string,
    x: number,
    y: number,
    width: number,
    options?: any
  ): number {
    if (!text) return y;
    const hasArabic = /[\u0600-\u06FF]/.test(text);
    const lines = doc.splitTextToSize(text, width);

    if (hasArabic) {
      const pageWidth = doc.internal.pageSize.getWidth();
      const rightX = pageWidth - 15;
      const processedLines = lines.map((line: string) => this.processTextForPdf(doc, line));
      doc.text(processedLines, rightX, y, { align: 'right', ...options });
    } else {
      doc.text(lines, x, y, options);
    }

    return y + lines.length * 6;
  }

  download(record: any): void {
    this.downloadAsync(record).catch(err => console.error('Error generating PDF', err));
  }

  private async downloadAsync(record: any): Promise<void> {
    const doc = new jsPDF();
    await this.loadFonts(doc);
    doc.setFont('Amiri', 'normal');

    switch (record.type) {
      case 'lab':
        await this.generateLabPdf(doc, record);
        break;
      case 'imaging':
        await this.generateImagingPdf(doc, record);
        break;
      case 'prescription':
        await this.generatePrescriptionPdf(doc, record);
        break;
      case 'medicine':
        await this.generateMedicineBoxPdf(doc, record);
        break;
      case 'general':
        await this.generateGeneralDocumentPdf(doc, record);
        break;
    }
  }

private async addHeader(doc: jsPDF, title: string): Promise<number> {

    const logo = await this.loadLogo();

    const pageWidth = doc.internal.pageSize.getWidth();

    const logoWidth = 20;
    const logoHeight = 20;

    doc.addImage(
        logo,
        'PNG',
        (pageWidth - logoWidth) / 2,
        10,
        logoWidth,
        logoHeight
    );

    doc.setFont('Amiri', 'bold');
    doc.setFontSize(20);

    doc.text('Rafiq AI', pageWidth / 2, 38, {
        align: 'center'
    });

    doc.setFontSize(16);

    doc.text(title, pageWidth / 2, 50, {
        align: 'center'
    });

    doc.setDrawColor(220);

    doc.line(20, 58, pageWidth - 20, 58);

    return 72;
}

private addSummary(
    doc: jsPDF,
    summary: string,
    y: number
): number {

    doc.setFont('Amiri', 'bold');
    doc.setFontSize(14);

    doc.text("AI Summary", 15, y);

    y += 8;

    doc.setFont('Amiri', 'normal');
    doc.setFontSize(11);

    y = this.drawParagraph(doc, summary, 15, y, 180);

    return y + 10;
}

private addFooter(doc: jsPDF) {

    const pageHeight = doc.internal.pageSize.getHeight();

    doc.setFont('Amiri', 'normal');
    doc.setFontSize(10);

    doc.setTextColor(120);

    doc.text(
        "Generated by Rafiq AI",
        15,
        pageHeight - 10
    );

}
  
  private async loadLogo(): Promise<string> {

    const response = await fetch('/images/RafiqLogo.png');

    const blob = await response.blob();

    return new Promise((resolve) => {

      const reader = new FileReader();

      reader.onloadend = () => resolve(reader.result as string);

      reader.readAsDataURL(blob);

    });

  }

  private autoTableArabic(doc: jsPDF, options: any): void {
    const self = this;
    const userDidParseCell = options.didParseCell;
    autoTable(doc, {
      ...options,
      styles: {
        font: 'Amiri',
        fontSize: 10,
        cellPadding: 3,
        ...(options.styles ?? {})
      },
      headStyles: {
        font: 'Amiri',
        ...(options.headStyles ?? {})
      },
      didParseCell: (data: any) => {
        const cellText: string = String(data.cell.text ?? '');
        if (/[\u0600-\u06FF]/.test(cellText)) {
          data.cell.styles.halign = 'right';
          data.cell.text = [self.processTextForPdf(doc, cellText)];
        }
        if (userDidParseCell) userDidParseCell(data);
      }
    });
  }

private async generateLabPdf(doc: jsPDF, record: any) {

    let y = await this.addHeader(
        doc,
        "Laboratory Report"
    );

    doc.setFont('Amiri', 'normal');
    doc.setFontSize(12);

    this.drawText(doc, `Lab Name: ${record.rawRecord.labName}`, 15, y); y += 8;
    this.drawText(doc, `Doctor: ${record.rawRecord.doctorName}`, 15, y); y += 8;
    this.drawText(doc, `Report Date: ${record.rawRecord.reportDate}`, 15, y); y += 15;

    y = this.addSummary(
        doc,
        record.summary,
        y
    );

    this.autoTableArabic(doc, {
      startY: y,

      head: [[
        "Test Name",
        "Value",
        "Unit",
        "Reference Range",
        "Status"
      ]],

      body: (record.rawRecord.results ?? []).map((r: any) => [

        r.testName,

        r.value,

        r.unit,

        r.normalRange,

        r.status

      ]),

      theme: "grid",

      headStyles: {
        fillColor: [52, 152, 219]
      }
    });

    this.addFooter(doc);

    doc.save(`${record.labName ?? 'lab-report'}.pdf`);

}
private async generateImagingPdf(doc: jsPDF, record: any) {

  let y = await this.addHeader(
    doc,
    "Imaging Report"
  );

  doc.setFont('Amiri', 'normal');
  doc.setFontSize(12);

  this.drawText(doc, `Imaging Type: ${record.rawRecord.imagingType ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Body Part: ${record.rawRecord.bodyPart ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Doctor: ${record.rawRecord.doctorName ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Report Date: ${record.rawRecord.reportDate ?? "-"}`, 15, y); y += 15;

  if (record.summary) {
    y = this.addSummary(
      doc,
      record.summary,
      y
    );
  }

  doc.setFont('Amiri', 'bold');
  doc.setFontSize(14);

  this.drawText(doc, "Findings", 15, y);

  y += 8;

  doc.setFont('Amiri', 'normal');
  doc.setFontSize(11);

  y = this.drawParagraph(doc, record.rawRecord.findings ?? "-", 15, y, 180);
  y += 10;

  doc.setFont('Amiri', 'bold');
  doc.setFontSize(14);

  this.drawText(doc, "Impression", 15, y);

  y += 8;

  doc.setFont('Amiri', 'normal');
  doc.setFontSize(11);

  this.drawParagraph(doc, record.rawRecord.impression ?? "-", 15, y, 180);

  this.addFooter(doc);

  doc.save(
    `ImagingReport-${record.rawRecord.imagingType ?? "Report"}.pdf`
  );
}

  private async generatePrescriptionPdf(doc: jsPDF, record: any) {

  let y = await this.addHeader(
    doc,
    "Medical Prescription"
  );

  doc.setFont('Amiri', 'normal');
  doc.setFontSize(12);

  this.drawText(doc, `Doctor: ${record.rawRecord.doctorName ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Patient: ${record.rawRecord.patientName ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Prescription Date: ${record.rawRecord.prescriptionDate ?? "-"}`, 15, y); y += 15;

  if (record.summary) {
    y = this.addSummary(
      doc,
      record.summary,
      y
    );
  }

  this.autoTableArabic(doc, {
    startY: y,

    head: [[
      "Medicine",
      "Dosage",
      "Frequency",
      "Duration",
      "Notes"
    ]],

    body: (record.rawRecord.medicines ?? []).map((m: any) => [

      m.medicineName ?? "-",

      m.dosage ?? "-",

      m.frequency ?? "-",

      m.duration ?? "-",

      m.notes ?? "-"

    ]),

    theme: "grid",

    headStyles: {
      fillColor: [52, 152, 219]
    }
  });

  this.addFooter(doc);

  doc.save(
    `Prescription-${record.rawRecord.patientName ?? "Report"}.pdf`
  );
}
private async generateMedicineBoxPdf(doc: jsPDF, record: any) {

  let y = await this.addHeader(
    doc,
    "Medication Record"
  );

  doc.setFont('Amiri', 'normal');
  doc.setFontSize(12);

  this.drawText(doc, `Medicine Name: ${record.rawRecord.medicineName ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Dosage: ${record.rawRecord.dosage ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Frequency: ${record.rawRecord.frequency ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Duration: ${record.rawRecord.duration ?? "-"}`, 15, y); y += 8;
  this.drawText(doc, `Source: ${record.rawRecord.source ?? "-"}`, 15, y); y += 15;

  if (record.rawRecord.notes) {

    doc.setFont('Amiri', 'bold');
    doc.setFontSize(14);

    this.drawText(doc, "Notes", 15, y);

    y += 8;

    doc.setFont('Amiri', 'normal');
    doc.setFontSize(11);

    y = this.drawParagraph(doc, record.rawRecord.notes, 15, y, 180);
    y += 10;
  }

  this.addFooter(doc);

  doc.save(
    `Medicine-${record.rawRecord.medicineName ?? "Record"}.pdf`
  );
}

private async generateGeneralDocumentPdf(doc: jsPDF, record: any) {

  let y = await this.addHeader(
    doc,
    record.rawRecord.title ?? record.name ?? "Medical Document"
  );

  doc.setFont('Amiri', 'normal');
  doc.setFontSize(12);

  [
    `Document Type: ${record.rawRecord.documentType ?? "-"}`,
    `Doctor Name: ${record.rawRecord.doctorName ?? "-"}`,
    `Hospital/Clinic: ${record.rawRecord.hospitalOrClinic ?? "-"}`,
    `Document Date: ${record.rawRecord.documentDate ?? record.date ?? "-"}`,
  ].forEach(line => {
    this.drawText(doc, line, 15, y);
    y += 8;
  });

  y += 7;

  if (record.rawRecord.description) {
    doc.setFont('Amiri', 'bold');
    doc.setFontSize(14);
    this.drawText(doc, "Description", 15, y);
    y += 8;
    doc.setFont('Amiri', 'normal');
    doc.setFontSize(11);
    y = this.drawParagraph(doc, record.rawRecord.description, 15, y, 180);
    y += 10;
  }

  if (record.summary || record.rawRecord.aiSummary) {
    y = this.addSummary(doc, record.summary || record.rawRecord.aiSummary, y);
  }

  this.addFooter(doc);

  doc.save(
    `Document-${record.rawRecord.title ?? "Record"}.pdf`
  );
}
}
