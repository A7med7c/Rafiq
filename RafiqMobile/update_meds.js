const fs = require('fs');

const path = 'src/app/Pages/medications/medications.ts';
let content = fs.readFileSync(path, 'utf8');

const replacements = [
    ["res.message || `${log.medicineName} has already been updated.`", "res.message || this.t().medications.alreadyUpdated.replace('{name}', log.medicineName)"],
    ["`${log.medicineName} dose skipped.`", "this.t().medications.doseSkipped.replace('{name}', log.medicineName)"],
    ["err?.error?.message ?? 'Could not skip dose.'", "err?.error?.message ?? this.t().medications.skipDoseFailed"],
    ["res.message || `${log.medicineName} could not be snoozed.`", "res.message || this.t().medications.snoozeFailed.replace('{name}', log.medicineName)"],
    ["`${log.medicineName} reminder snoozed for ${minutes} minutes.`", "this.t().medications.snoozedFor.replace('{name}', log.medicineName).replace('{minutes}', minutes.toString())"],
    ["err?.error?.message ?? 'Could not snooze reminder.'", "err?.error?.message ?? this.t().medications.snoozeReminderFailed"],
    ["`${log.medicineName} marked as taken. Great job! 💊`", "this.t().medications.markedTaken.replace('{name}', log.medicineName)"],
    ["err?.error?.message ?? 'Could not confirm medication.'", "err?.error?.message ?? this.t().medications.confirmMedFailed"],
    ["err?.error?.message ?? 'Could not save reminder.'", "err?.error?.message ?? this.t().medications.saveReminderFailed"],
    ["`Reminder updated for ${medName}.`", "this.t().medications.reminderUpdated.replace('{name}', medName)"],
    ["err?.error?.message ?? 'Could not update reminder.'", "err?.error?.message ?? this.t().medications.updateReminderFailed"],
    ["`Reminders ${isNowPaused ? 'paused' : 'resumed'} for ${medName}.`", "isNowPaused ? this.t().medications.remindersPaused.replace('{name}', medName) : this.t().medications.remindersResumed.replace('{name}', medName)"],
    ["err?.error?.message ?? 'Could not toggle reminders.'", "err?.error?.message ?? this.t().medications.toggleRemindersFailed"],
    ["`Reminders deleted for ${medName}.`", "this.t().medications.remindersDeleted.replace('{name}', medName)"],
    ["err?.error?.message ?? 'Could not delete reminders.'", "err?.error?.message ?? this.t().medications.deleteRemindersFailed"],
    ["`${payload.medicineName} added successfully.`", "this.t().medications.addedSuccess.replace('{name}', payload.medicineName)"],
    ["`${payload.medicineName} updated successfully.`", "this.t().medications.updatedSuccess.replace('{name}', payload.medicineName)"],
    ["`${medName} has been removed from your medication list.`", "this.t().medications.removedFromList.replace('{name}', medName)"],
    ["err?.error?.message ?? 'Could not delete medication.'", "err?.error?.message ?? this.t().medications.deleteMedFailed"]
];

replacements.forEach(([oldStr, newStr]) => {
    content = content.replace(oldStr, newStr);
});

fs.writeFileSync(path, content, 'utf8');
console.log("Done");
