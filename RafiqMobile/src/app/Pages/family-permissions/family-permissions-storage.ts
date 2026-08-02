export interface FamilyPermissionsPrefs {
  canManageMedications: boolean;
  receiveReminders: boolean;
  emergencyContactEnabled: boolean;
}

const DEFAULT_PREFS: FamilyPermissionsPrefs = {
  canManageMedications: true,
  receiveReminders: true,
  emergencyContactEnabled: false,
};

const STORAGE_PREFIX = 'rafiq.familyPermissions.';

/** Reads the locally-stored permission preferences for a family profile, falling back to sane defaults. */
export function getPermissions(profileId: string): FamilyPermissionsPrefs {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + profileId);
    if (!raw) return { ...DEFAULT_PREFS };
    const parsed = JSON.parse(raw);
    return { ...DEFAULT_PREFS, ...parsed };
  } catch {
    return { ...DEFAULT_PREFS };
  }
}

/** Persists the permission preferences for a family profile to local storage. */
export function setPermissions(profileId: string, prefs: FamilyPermissionsPrefs): void {
  try {
    localStorage.setItem(STORAGE_PREFIX + profileId, JSON.stringify(prefs));
  } catch {
    // localStorage can throw in some webviews (e.g. private mode) — fail silently.
  }
}
