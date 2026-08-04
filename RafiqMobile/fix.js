const fs = require('fs');
const path = require('path');
const p = path.resolve('src/app/Components/records-content/records-content.html');
let html = fs.readFileSync(p, 'utf8');

const replacement = `  } @else {
  <section class="upload-flow-step2">
    <div class="uf-header">
      <h1 class="uf-title">{{ getUploadFlowTitle() }}</h1>
      <button type="button" class="uf-back" (click)="uploadFlowType.set(null)"><i class="fa-solid fa-angle-left"></i></button>
    </div>
    <div class="uf-body">
      <div class="uf-type-banner">
        <p class="uf-type-label">النوع: <span>{{ getUploadFlowTitle() }}</span></p>
        <p class="uf-type-sub">يتم تحديد النوع تلقائيا</p>
      </div>

      <div class="uf-upload-box">
        <div class="uf-upload-icon-circle"><i class="fa-solid fa-cloud-arrow-up"></i></div>
        <h3>ارفع صورة {{ getUploadFlowTitle() }}</h3>
        <p>أو اختر من جهازك</p>

        <div class="uf-upload-actions">
           <button type="button" (click)="triggerUploadMethod('scan')"><i class="fa-solid fa-print"></i><span>مسح ضوئي</span></button>
           <button type="button" (click)="triggerUploadMethod('file')"><i class="fa-solid fa-folder"></i><span>اختيار ملف</span></button>
           <button type="button" (click)="triggerUploadMethod('camera')"><i class="fa-solid fa-camera"></i><span>التقاط صورة</span></button>
        </div>
      </div>

      <div class="uf-separator"><span>أو</span></div>

      <button type="button" class="uf-manual-btn" (click)="triggerManualEntry()">
         <div class="uf-manual-content">
            <h4><i class="fa-solid fa-pen"></i> إدخال البيانات يدويا</h4>
            <p>اكتب البيانات يدويا بدون صورة</p>
         </div>
      </button>
    </div>
  </section>
  }`;

// Replace everything from `<div class="uf-upload-actions">` to the closing `  }` that terminates the else.
// Wait, the duplicate grid was removed by my previous multi_replace_file_content!
// Let's just replace the broken text using a simpler regex.
// The file currently has:
// <div class="uf-upload-actions">
// ... weird text ...
// </div>
// ...
// </div>
// </section>
// }
// So we can replace `<div class="uf-upload-actions">` up to `</section>\n  }`.

html = html.replace(/<div class="uf-upload-actions">[\s\S]*?<\/section>\s*\}/, replacement);
fs.writeFileSync(p, html, 'utf8');
console.log('Fixed HTML.');
