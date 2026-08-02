const fs = require('fs');
let html = fs.readFileSync('src/app/Pages/my-profile/my-profile.html', 'utf-8');

// Replacements
html = html.replace(/<div class="mp-info-list-item">[\s\S]*?<span class="mp-info-list-lbl">{{ t\(\)\.myProfile\.nationality }}<\/span>[\s\S]*?<\/div>/g, '');
html = html.replace(/<div class="mp-info-list-item">[\s\S]*?<span class="mp-info-list-lbl">{{ t\(\)\.myProfile\.language }}<\/span>[\s\S]*?<\/div>/g, '');
html = html.replace(/<div class="mp-info-list-item">[\s\S]*?<span class="mp-info-list-lbl">{{ t\(\)\.myProfile\.address }}<\/span>[\s\S]*?<\/div>/g, '');
html = html.replace(/<div class="mp-form-field">[\s\S]*?<label>{{ t\(\)\.myProfile\.nationality }}<\/label>[\s\S]*?<\/div>/g, '');
html = html.replace(/<div class="mp-form-field">[\s\S]*?<label>{{ t\(\)\.myProfile\.language }}<\/label>[\s\S]*?<\/div>/g, '');
html = html.replace(/<div class="mp-form-field">[\s\S]*?<label>{{ t\(\)\.myProfile\.address }}<\/label>[\s\S]*?<\/div>/g, '');
html = html.replace(/editHealth\(\)/g, 'openEditHealth()');
html = html.replace(/cancelHealth\(\)/g, 'cancelEditHealth()');
html = html.replace(/cancelPersonal\(\)/g, 'cancelEditPersonal()');
html = html.replace(/cancelAllergy\(\)/g, 'cancelAddAllergy()');
html = html.replace(/cancelDisease\(\)/g, 'cancelAddDisease()');
html = html.replace(/cancelContact\(\)/g, 'cancelAddContact()');

html = html.replace(/firstName"/g, 'personalForm.firstName"');
html = html.replace(/lastName"/g, 'personalForm.lastName"');
html = html.replace(/newEmail"/g, 'personalForm.email"');
html = html.replace(/phoneNumber"/g, 'personalForm.phoneNumber"');
html = html.replace(/dob"/g, 'personalForm.dateOfBirth"');
html = html.replace(/gender"/g, 'personalForm.gender"');
html = html.replace(/genderLabel\(\)/g, 'profile()?.gender'); // Just display the string gender

html = html.replace(/bloodType"/g, 'healthForm.bloodType"');
html = html.replace(/height"/g, 'healthForm.height"');
html = html.replace(/weight"/g, 'healthForm.weight"');

html = html.replace(/allergyName"/g, 'newAllergy.name"');
html = html.replace(/allergySeverity"/g, 'newAllergy.severity"');
html = html.replace(/diseaseName"/g, 'newDisease.name"');
html = html.replace(/diseaseStatus"/g, 'newDisease.status"');
html = html.replace(/diseaseNotes"/g, 'newDisease.diagnosedAt"');
html = html.replace(/contactName"/g, 'newContact.name"');
html = html.replace(/contactRel"/g, 'newContact.relation"');
html = html.replace(/contactPhone"/g, 'newContact.phoneNumber"');

// Fix options
html = html.replace(/t\(\)\.myProfile\.bloodUnknown/g, "'Unknown'");
html = html.replace(/t\(\)\.myProfile\.severityMild/g, 't().myProfile.mild');
html = html.replace(/t\(\)\.myProfile\.severityMod/g, 't().myProfile.moderate');
html = html.replace(/t\(\)\.myProfile\.severitySev/g, 't().myProfile.severe');
html = html.replace(/t\(\)\.myProfile\.statusActive/g, 't().myProfile.active');
html = html.replace(/t\(\)\.myProfile\.statusRemission/g, 't().myProfile.controlled');
html = optionReplace(html, 't().myProfile.statusResolved', 't().myProfile.resolved');

html = html.replace(/t\(\)\.myProfile\.saveBtn/g, 't().myProfile.saveChanges');

// Remove emergency notes
html = html.replace(/<div class="mp-form-field">[\s\S]*?<label>{{ t\(\)\.myProfile\.emergencyMedicalNotes }}<\/label>[\s\S]*?<\/div>/g, '');
html = html.replace(/<div class="mp-info-list-item">[\s\S]*?<span class="mp-info-list-lbl">{{ t\(\)\.myProfile\.emergencyNotes }}<\/span>[\s\S]*?<\/div>/g, '');

// Remove reaction
html = html.replace(/<div class="mp-form-field">[\s\S]*?<label>{{ t\(\)\.myProfile\.reaction }}<\/label>[\s\S]*?<\/div>/g, '');
html = html.replace(/<p>{{ a\.reaction \|\| 'No reaction specified' }}<\/p>/g, '');
html = html.replace(/a\.allergen/g, 'a.name');

// Fix severity labels
html = html.replace(/getAllergySeverityLabel\(a\.severity\)/g, 'a.severity');
html = html.replace(/a\.severity===1/g, "a.severity==='Moderate'");
html = html.replace(/a\.severity===2/g, "a.severity==='Severe'");

html = html.replace(/<div class="mp-info-list-item">[\s\S]*?<span class="mp-info-list-lbl">BMI<\/span>[\s\S]*?<\/div>/g, '');

html = html.replace(/<label>{{ t\(\)\.myProfile\.notes }}<\/label>/g, '<label>{{ t().myProfile.diagnosedAt }}</label>');
html = html.replace(/d\.diseaseName/g, 'd.name');
html = html.replace(/d\.notes/g, 'd.diagnosedAt');
html = html.replace(/getDiseaseStatusLabel\(d\.status\)/g, 'd.status');
html = html.replace(/t\(\)\.myProfile\.noDiseases/g, 't().myProfile.noChronicDiseases');

// Contact
html = html.replace(/<div class="mp-form-field">[\s\S]*?<label>{{ t\(\)\.myProfile\.email }}<\/label>[\s\S]*?<input type="email" \[\(ngModel\)\]="newContact\.contactEmail" name="contactEmail">[\s\S]*?<\/div>/g, '');
html = html.replace(/c\.relationship/g, 'c.relation');

html = html.replace(/t\(\)\.myProfile\.deleteModalText/g, 't().myProfile.deleteAccountDesc');
html = html.replace(/t\(\)\.myProfile\.confirmDeleteBtn/g, 't().myProfile.deleteAccountBtn');

html = html.replace(/newAllergy\.name\.trim\(\)/g, "newAllergy.name.trim()");
html = html.replace(/newDisease\.name\.trim\(\)/g, "newDisease.name.trim()");
html = html.replace(/newContact\.name\.trim\(\)/g, "newContact.name.trim()");
html = html.replace(/newContact\.phoneNumber\.trim\(\)/g, "newContact.phoneNumber.trim()");

function optionReplace(html, from, to) {
    return html.split(from).join(to);
}

fs.writeFileSync('src/app/Pages/my-profile/my-profile.html', html, 'utf-8');
