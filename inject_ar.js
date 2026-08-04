const fs = require('fs');
let arTs = fs.readFileSync('RafiqMobile/src/app/i18n/ar.ts', 'utf8');
const keys = JSON.parse(fs.readFileSync('validation_keys.json', 'utf8'));

const arMap = {
  'TitleCannotBeEmpty': 'العنوان لا يمكن أن يكون فارغاً.',
  'TitleCannotExceed255Characters': 'لا يمكن أن يتجاوز العنوان ٢٥٥ حرفاً.',
  'ProfileIdIsRequired': 'معرف الملف الشخصي مطلوب.',
  'AppointmentDateTimeIsRequired': 'تاريخ ووقت الموعد مطلوبان.',
  'AppointmentDateTimeCannotBeInT': 'لا يمكن أن يكون وقت الموعد في الماضي.',
  'AppointmentTypeMustBeAValidVal': 'نوع الموعد يجب أن يكون قيمة صحيحة.',
  'CustomTypeIsRequiredWhenAppoin': 'النوع المخصص مطلوب عندما يكون الموعد من نوع آخر.',
  'CustomTypeMustBeNullWhenAppoin': 'يجب أن يكون النوع المخصص فارغاً عندما لا يكون الموعد من نوع آخر.',
  'TitleIsRequired': 'العنوان مطلوب.',
  'ProviderIsRequired': 'مقدم الخدمة مطلوب.',
  'ReminderOffsetMinutesCannotBeN': 'وقت التذكير لا يمكن أن يكون سلبياً.',
  'AppointmentIdIsRequired': 'معرف الموعد مطلوب.',
  'PhoneNumberMustBeAValidEgyptia': 'يجب أن يكون رقم الهاتف رقم محمول مصري صحيح.',
  'PasswordsDoNotMatch': 'كلمات المرور غير متطابقة.',
  'InvalidEmailAddress': 'البريد الإلكتروني غير صحيح.',
  'ProfileImageMustBeAJPEGPNGWEBP': 'صورة الملف الشخصي يجب أن تكون JPEG أو PNG أو WEBP أو GIF.',
  'ProfileImageMustNotExceed5MB': 'لا يجب أن يتجاوز حجم الصورة ٥ ميغابايت.',
  'PhoneNumberMustBeAValidEgyptia2': 'يجب أن يكون رقم الهاتف رقم محمول مصري صحيح.',
  'PasswordMustContainAtLeastOneU': 'يجب أن تحتوي كلمة المرور على حرف كبير واحد على الأقل.',
  'PasswordMustContainAtLeastOneL': 'يجب أن تحتوي كلمة المرور على حرف صغير واحد على الأقل.',
  'PasswordMustContainAtLeastOneD': 'يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.',
  'PasswordMustContainAtLeastOneS': 'يجب أن تحتوي كلمة المرور على رمز خاص واحد على الأقل.',
  'ConfirmPasswordMustMatchPasswo': 'تأكيد كلمة المرور يجب أن يتطابق مع كلمة المرور.',
  'NameIsRequired': 'الاسم مطلوب.',
  'NameCannotExceed100Characters': 'لا يمكن أن يتجاوز الاسم ١٠٠ حرف.',
  'PhoneNumberIsRequired': 'رقم الهاتف مطلوب.',
  'RelationIsRequired': 'صلة القرابة مطلوبة.',
  'RelationCannotExceed100Charact': 'لا يمكن أن تتجاوز صلة القرابة ١٠٠ حرف.',
  'DateCannotBeLaterThanToday': 'لا يمكن أن يكون التاريخ بعد اليوم.',
  'AnImageFileIsRequired': 'ملف الصورة مطلوب.',
  'ReminderLogIdIsRequired': 'معرف التذكير مطلوب.',
  'UserMedicineIdIsRequired': 'معرف الدواء مطلوب.',
  'StartDateIsRequired': 'تاريخ البدء مطلوب.',
  'StartDateCannotBeBeforeTodaysD': 'لا يمكن أن يكون تاريخ البدء قبل تاريخ اليوم.',
  'EndDateIsRequired': 'تاريخ الانتهاء مطلوب.',
  'EndDateMustBeGreaterThanOrEqua': 'تاريخ الانتهاء يجب أن يكون مساوياً أو بعد تاريخ البدء.',
  'RepeatTypeMustBeAValidValue': 'نوع التكرار يجب أن يكون قيمة صحيحة.',
  'AtLeastOneReminderTimeIsRequir': 'مطلوب وقت تذكير واحد على الأقل.',
  'OneOrMoreReminderTimesHaveAnIn': 'يوجد أوقات تذكير بصيغة غير صحيحة.',
  'DuplicateReminderTimesAreNotAl': 'لا يُسمح بتكرار أوقات التذكير.',
  'ForOnceRemindersEndDateMustBeE': 'بالنسبة للتذكيرات لمرة واحدة، يجب أن يتطابق تاريخ الانتهاء مع تاريخ البدء.',
  'ForOnceRemindersStartingTodayT': 'بالنسبة للتذكيرات التي تبدأ اليوم، يجب أن يكون الوقت في المستقبل.',
  'IdIsRequired': 'المعرف مطلوب.',
  'DiagnosedDateCannotBeInTheFutu': 'تاريخ التشخيص لا يمكن أن يكون في المستقبل.',
  'DateOfBirthCannotBeInTheFuture': 'تاريخ الميلاد لا يمكن أن يكون في المستقبل.',
  'RelationshipIsRequiredForAMana': 'صلة القرابة مطلوبة للملف المدار.',
  'SelfCannotBeUsedAsTheRelations': "لا يمكن استخدام 'أنا' كصلة قرابة لملف مدار.",
  'ChronicDiseaseDiagnosisDateCan': 'تاريخ تشخيص المرض المزمن لا يمكن أن يكون في المستقبل.',
  'OnlyManagerOrViewerAccessCanBe': 'يمكن فقط طلب صلاحية مشاهد أو مدير للملف الشخصي.',
  'PrescriptionIdIsRequired': 'معرف الروشتة مطلوب.',
  'DoctorNameIsRequired': 'اسم الطبيب مطلوب.',
  'DoctorNameMustNotExceed150Char': 'لا يمكن أن يتجاوز اسم الطبيب ١٥٠ حرفاً.',
  'PatientNameIsRequired': 'اسم المريض مطلوب.',
  'PatientNameMustNotExceed200Cha': 'لا يمكن أن يتجاوز اسم المريض ٢٠٠ حرف.',
  'PrescriptionDateIsRequired': 'تاريخ الروشتة مطلوب.',
  'PrescriptionDateMustBeInYyyyMM': 'يجب أن يكون التاريخ بصيغة yyyy-MM-dd.',
  'AtLeastOneMedicineIDMustBeProv': 'مطلوب دواء واحد على الأقل.',
  'MedicineIDCannotBeEmpty': 'لا يمكن أن يكون معرف الدواء فارغاً.',
  'MedicineNameIsRequired': 'اسم الدواء مطلوب.',
  'MedicineNameMustNotExceed300Ch': 'لا يمكن أن يتجاوز اسم الدواء ٣٠٠ حرف.',
  'DosageMustNotExceed200Characte': 'لا يمكن أن تتجاوز الجرعة ٢٠٠ حرف.',
  'FrequencyIsRequired': 'التكرار مطلوب.',
  'FrequencyMustNotExceed200Chara': 'لا يمكن أن يتجاوز التكرار ٢٠٠ حرف.',
  'DurationIsRequired': 'المدة مطلوبة.',
  'DurationMustNotExceed200Charac': 'لا يمكن أن تتجاوز المدة ٢٠٠ حرف.',
  'InvalidSourceType': 'نوع المصدر غير صحيح.',
  'MedicineIdIsRequired': 'معرف الدواء مطلوب.'
};

let arContent = '  Validation: {\n';
for (const key of Object.keys(keys)) {
    const rawKey = key.split('.')[1];
    arContent += '    ' + rawKey + ': `' + (arMap[rawKey] || keys[key]) + '`,\n';
}
arContent += '  },\n';

arTs = arTs.replace('export const ar: Translations = {', 'export const ar: Translations = {\n' + arContent);
fs.writeFileSync('RafiqMobile/src/app/i18n/ar.ts', arTs);
console.log('ar.ts updated!');
