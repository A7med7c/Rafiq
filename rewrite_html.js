const fs = require('fs');

const html = fs.readFileSync('RafiqMobile/src/app/Components/records-content/records-content.html', 'utf8');
const lines = html.split('\n');
const detailsModalIndex = lines.findIndex(l => l.includes('DETAILS MODAL'));

const newHtml = `
<div class="records-content-host">

  <!-- SEARCH BAR -->
  <div class="fp-search-bar">
    <i class="fa-solid fa-magnifying-glass search-icon"></i>
    <input type="text" [placeholder]="t().records.searchPlaceholder || 'ابحث في السجلات...'" [(ngModel)]="searchQueryValue">
    <button type="button" class="filter-btn-icon" (click)="toggleFilterMenu($event)">
      <i class="fa-solid fa-filter"></i>
    </button>
  </div>

  @if (!compact && !readOnly) {
    @if (!uploadFlowType()) {
      <div class="fp-section-header">
        <h3>أنواع السجلات</h3>
        <a class="fp-view-all">عرض الكل</a>
      </div>
      <div class="fp-types-scroll">
        <div class="fp-type-card fp-bg-cyan" (click)="startUploadFlow('General Medical Document')">
          <div class="fp-type-icon"><i class="fa-solid fa-file-medical"></i></div>
          <div class="fp-type-title">تقارير طبية</div>
          <div class="fp-type-count">{{ generalCount() }} سجل</div>
        </div>
        <div class="fp-type-card fp-bg-orange" (click)="startUploadFlow('Medicine Box')">
          <div class="fp-type-icon"><i class="fa-solid fa-pills"></i></div>
          <div class="fp-type-title">أدوية</div>
          <div class="fp-type-count">{{ medicineCount() }} سجل</div>
        </div>
        <div class="fp-type-card fp-bg-green" (click)="startUploadFlow('Prescription')">
          <div class="fp-type-icon"><i class="fa-solid fa-prescription-bottle-medical"></i></div>
          <div class="fp-type-title">روشتات</div>
          <div class="fp-type-count">{{ prescriptionCount() }} سجل</div>
        </div>
        <div class="fp-type-card fp-bg-purple" (click)="startUploadFlow('X-Ray & Imaging')">
          <div class="fp-type-icon"><i class="fa-solid fa-x-ray"></i></div>
          <div class="fp-type-title">أشعة</div>
          <div class="fp-type-count">{{ imagingCount() }} سجل</div>
        </div>
        <div class="fp-type-card fp-bg-blue" (click)="startUploadFlow('Lab Analysis')">
          <div class="fp-type-icon"><i class="fa-solid fa-flask"></i></div>
          <div class="fp-type-title">تحاليل</div>
          <div class="fp-type-count">{{ labCount() }} سجل</div>
        </div>
      </div>
    } @else {
      <!-- Upload Flow Step 2 (Existing Logic) -->
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
    }
  }

  <!-- RECENT RECORDS -->
  <div class="fp-section-header fp-mt-24">
    <h3>آخر السجلات</h3>
    <a class="fp-view-all">عرض الكل</a>
  </div>
  
  @if (loading()) {
    <div class="table-loader">
      <div class="skl-list">
        @for (s of [1,2,3,4,5]; track s) {
        <div class="skl-row">
          <div class="skl-sq"></div>
          <div class="skl-lines"><div class="skl-line skl-70"></div></div>
          <div class="skl-pill"></div>
        </div>
        }
      </div>
    </div>
  } @else if (filteredRecords().length === 0) {
    <div class="empty-state table-empty" style="background: white; border-radius: 16px; margin-top: 12px; padding: 40px 16px; box-shadow: 0 2px 8px rgba(0,0,0,0.04);">
      <i class="fa-solid fa-folder-open empty-icon"></i>
      <p class="empty-title">{{ t().records.noRecordsFound }}</p>
      <p class="empty-sub">{{ t().records.noRecordsSub }}</p>
    </div>
  } @else {
    <div class="fp-records-list">
      @for (rec of pagedRecords(); track rec.id; let i = $index) {
        <div class="fp-record-item" (click)="viewDetails(rec)">
          <div class="fp-record-menu" (click)="$event.stopPropagation(); toggleActionMenu(rec.id, $event)">
            <i class="fa-solid fa-ellipsis-vertical"></i>
            @if (actionMenuOpen() === rec.id) {
               <div class="record-menu" [class.record-menu-up]="isLastRow(i)" (click)="$event.stopPropagation()">
                  <button type="button" class="record-menu-item" (click)="editRecord(rec)">
                    <i class="fa-solid fa-pen-to-square"></i> {{ t().records.editAction }}
                  </button>
                  <button type="button" class="record-menu-item record-menu-item--danger" (click)="openDeleteModal(rec)">
                    <i class="fa-solid fa-trash-can"></i> {{ t().records.deleteAction }}
                  </button>
               </div>
            }
          </div>
          <div class="fp-record-info">
            <h4>{{ rec.name }}</h4>
            <p>{{ rec.rawRecord.doctorName || rec.rawRecord.labName || rec.rawRecord.hospitalOrClinic || rec.typeLabel || '-' }}</p>
            <span>{{ rec.date }}</span>
          </div>
          <div class="fp-record-file-icon">
             <div class="fp-file-badge" [class.is-pdf]="rec.type === 'general' || rec.type === 'lab' || rec.type === 'prescription'">
               <i class="fa-solid" [class.fa-file-pdf]="rec.type === 'general' || rec.type === 'lab' || rec.type === 'prescription'" [class.fa-file-image]="rec.type === 'imaging'" [class.fa-file-lines]="rec.type === 'medicine'"></i>
               <span>{{ (rec.type === 'general' || rec.type === 'lab' || rec.type === 'prescription') ? 'PDF' : (rec.type === 'imaging' ? 'JPG' : 'DOC') }}</span>
             </div>
          </div>
        </div>
      }
    </div>
  }

  <!-- FLOATING ADD BUTTON -->
  @if (!readOnly && !uploadFlowType()) {
    <button class="fp-fab-btn" assistantAnchor="add-record-button" [class.menu-open]="addRecordMenuOpen()" (click)="toggleAddRecordMenu($event)">
      <i class="fa-solid fa-plus"></i> إضافة سجل جديد
    </button>
    
    @if (addRecordMenuOpen()) {
      <div class="add-record-menu fp-fab-menu">
        <button type="button" class="dropdown-menu-item" (click)="selectUploadType('Lab Analysis')">
          <i class="fa-solid fa-flask"></i><span>{{ t().records.labAnalysisItem }}</span>
        </button>
        <button type="button" class="dropdown-menu-item" (click)="selectUploadType('Prescription')">
          <i class="fa-solid fa-prescription-bottle-medical"></i><span>{{ t().records.prescriptionItem }}</span>
        </button>
        <button type="button" class="dropdown-menu-item" (click)="selectUploadType('X-Ray & Imaging')">
          <i class="fa-solid fa-x-ray"></i><span>{{ t().records.xrayImagingItem }}</span>
        </button>
        <button type="button" class="dropdown-menu-item" (click)="selectUploadType('Medicine Box')">
          <i class="fa-solid fa-pills"></i><span>{{ t().records.medicineBoxItem }}</span>
        </button>
        <button type="button" class="dropdown-menu-item" (click)="selectUploadType('General Medical Document')">
          <i class="fa-solid fa-file-medical"></i><span>{{ t().records.otherMedicalDoc }}</span>
        </button>
      </div>
    }
  }

</div>

`;

const finalLines = newHtml.split('\n').concat(lines.slice(detailsModalIndex));
fs.writeFileSync('RafiqMobile/src/app/Components/records-content/records-content.html', finalLines.join('\n'));
console.log('HTML rewritten');
