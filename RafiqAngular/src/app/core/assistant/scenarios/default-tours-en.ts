/**
 * @file default-tours-en.ts
 * @description English versions of all guided tour scenarios.
 * Each scenario ID mirrors the Arabic version with an "-en" suffix so the
 * tour engine can auto-select the right language at runtime.
 * speechLanguage is set to 'en-US' to force the English neural voice.
 */

import { TourScenario } from '../models/tour-scenario';

const ONBOARDING_FIXED_POSITION = { x: 450, y: 250 };

// ── Onboarding Tour (English) ──────────────────────────────────────────────

export const ONBOARDING_TOUR_EN: TourScenario = {
  id: 'onboarding-tour-en',
  title: 'Setting Up Your Health Profile',
  description: 'Guided tour during first-time health profile setup',
  category: 'onboarding',
  steps: [
    {
      id: 'onboarding-welcome-step-en',
      route: '/onboarding/welcome',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: "Welcome to Rafiq, {{userName}}! I'm your AI health companion. I'll guide you through setting up your health profile step by step — it will take less than two minutes.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      waitForUser: true,
    },
    {
      id: 'onboarding-step1-guide-en',
      route: '/onboarding/step1',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: "In this step, enter your basic information: date of birth, gender, height, weight, and blood type. This helps me track your health accurately.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      waitForUser: true,
    },
    {
      id: 'onboarding-emergency-guide-en',
      route: '/onboarding/emergency',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: "Add an emergency contact here — someone close to you I can reach in urgent situations. If you're late taking an important medication, I'll send them an instant WhatsApp alert.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      waitForUser: true,
    },
    {
      id: 'onboarding-step2-guide-en',
      route: '/onboarding/step2',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: "Do you have any allergies to food or medication? If so, record them here so I can always alert you before any new treatment is prescribed.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      waitForUser: true,
    },
    {
      id: 'onboarding-step3-guide-en',
      route: '/onboarding/step3',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: "If you have any chronic conditions, record them here so I can follow your case and give you tailored advice.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      waitForUser: true,
    },
    {
      id: 'onboarding-step4-guide-en',
      route: '/onboarding/step4',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: "Excellent! This is a complete review of all the information you entered. Verify everything looks right, and then we'll head to the dashboard together.",
      speechLanguage: 'en-US',
      avatarState: 'celebrate',
      waitForUser: true,
    },
  ],
};

// ── Welcome Tour (English) ─────────────────────────────────────────────────

export const WELCOME_TOUR_EN: TourScenario = {
  id: 'welcome-tour-en',
  title: 'Full Platform Walkthrough',
  description: 'A comprehensive tour of all Rafiq features and sections',
  category: 'onboarding',
  steps: [
    {
      id: 'welcome-greeting-en',
      route: '/dashboard',
      speech: "Welcome back, {{userName}}! I'm glad you joined Rafiq. Let me take you on a full tour so we can explore every page and feature of the platform together.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
    },
    {
      id: 'dashboard-nav-dashboard-en',
      anchor: 'dashboard-nav',
      route: '/dashboard',
      speech: "This is the sidebar — your gateway to everything in Rafiq. Starting with the Dashboard for an overview.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-records-en',
      anchor: 'medical-records-nav',
      speech: "Medical Records for uploading lab results and prescriptions.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-appointments-en',
      anchor: 'appointments-nav',
      speech: "Appointments to schedule your medical visits.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-medications-en',
      anchor: 'medications-nav',
      speech: "Medications and Reminders to track your doses.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-family-en',
      anchor: 'family-profiles-nav',
      speech: "Family Profiles to manage the health of all your family members.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-profile-en',
      anchor: 'profile-nav',
      speech: "And finally, My Profile where you can manage your personal and health information.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'ask-ai-button-intro-en',
      anchor: 'ask-ai-button',
      route: '/dashboard',
      speech: "This is the 'Ask Rafiq' button, available at the top of every page. Press it anytime you have a medical question and I'll answer immediately.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
    },
    {
      id: 'ai-health-summary-en',
      anchor: 'ai-health-summary-card',
      route: '/dashboard',
      speech: "Here you'll find your AI Health Summary. I continuously analyze your medical data and provide personalized guidance for your condition. Press 'Open AI Assistant' to discuss any detail with me.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
    },
    {
      id: 'system-status-en',
      anchor: 'system-status-card',
      route: '/dashboard',
      speech: "This card shows system status: data processing, last sync, and the latest backup. Below it you'll see your family's heart rate trend over the past week.",
      speechLanguage: 'en-US',
      avatarState: 'idle',
      highlight: true,
    },
    {
      id: 'upcoming-appointment-en',
      route: '/dashboard',
      variants: {
        populated: {
          anchor: 'upcoming-appointment-detail',
          speech: "This card shows your upcoming appointment details: the doctor's name, date, and time — so you're always informed.",
        },
        empty: {
          anchor: 'add-appointment-button',
          speech: "The Upcoming Appointment card will show your next doctor visit as soon as you add one. Press 'Add New Appointment' to get started.",
        },
      },
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'family-overview-en',
      route: '/dashboard',
      variants: {
        populated: {
          anchor: 'family-member-card',
          speech: "Here you see a card for each family member showing their gender and age. Press 'Health Summary' on any of them for an instant health overview.",
        },
        empty: {
          anchor: 'add-family-member-button',
          speech: "This is the Family Overview section — currently showing only your profile. Press 'Add Family Member' to start tracking your family's health too.",
        },
      },
      speechLanguage: 'en-US',
      avatarState: 'celebrate',
      highlight: true,
    },
    {
      id: 'medical-records-lab-en',
      anchor: 'records-card-lab',
      route: '/medical-records',
      speech: "On the Medical Records page you'll find five sections. First: Lab Results.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-prescription-en',
      anchor: 'records-card-prescription',
      speech: "Prescriptions.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-imaging-en',
      anchor: 'records-card-imaging',
      speech: "Imaging.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-medicine-en',
      anchor: 'records-card-medicine',
      speech: "Medication boxes.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-general-en',
      anchor: 'records-card-general',
      speech: "And other documents. Use 'Scan with AI' and I'll read the document and extract the information automatically, or 'Add Manually' to enter it yourself.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-add-en',
      anchor: 'add-record-button',
      speech: "At any time, press 'Add Record' to choose the document type and upload it directly from here.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'appointments-step-en',
      anchor: 'add-appointment',
      route: '/appointments',
      speech: "On the Appointments page, press 'Add Appointment' to open a dialog where you choose the type: doctor visit, lab test, imaging, vaccination, dental, therapy session, or other. Then set the date, time, and reminder.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'medications-step-en',
      route: '/medications',
      variants: {
        populated: {
          anchor: 'med-next-dose-card',
          speech: "Here you see 'Next Dose' showing the medication name, dose amount, and when the next dose is due. Press 'I Took It' as soon as you take it, and I'll send you a reminder if you're late.",
        },
        empty: {
          anchor: 'add-medication',
          speech: "On the Medications page, press 'Add Medication' to log your first one. Set the name and dose schedule, and I'll send you reminders exactly on time.",
        },
      },
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
    },
    {
      id: 'family-profiles-step-en',
      anchor: 'add-family-profile',
      route: '/family-profiles',
      speech: "On the Family Profiles page, select any member to find tabs for: Overview, Medical Records, Appointments, Medications, Reminders, and Health Summary. Everything about their health in one place.",
      speechLanguage: 'en-US',
      avatarState: 'celebrate',
      highlight: true,
    },
    {
      id: 'profile-personal-info-step-en',
      anchor: 'profile-personal-info',
      route: '/my-profile',
      speech: "In My Profile you can update your personal information and profile photo.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-health-info-step-en',
      anchor: 'profile-health-info',
      speech: "And your health information such as blood type, height, and weight.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-allergies-step-en',
      anchor: 'profile-allergies',
      speech: "Below that: your allergies,",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-chronic-step-en',
      anchor: 'profile-chronic-diseases',
      speech: "chronic conditions,",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-emergency-step-en',
      anchor: 'profile-emergency-contacts',
      speech: "and emergency contacts — to complete any missing information.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
    },
    {
      id: 'ask-ai-finish-en',
      anchor: 'ask-ai-button',
      route: '/dashboard',
      speech: "That wraps up the tour! Whenever you need help, press 'Ask Rafiq' on any page and I'll be right there. Wishing you great health!",
      speechLanguage: 'en-US',
      avatarState: 'celebrate',
      highlight: true,
    },
  ],
  onStart: () => console.log('[WelcomeTourEN] started'),
  onComplete: () => {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('rafiq_tour_completed', 'true');
    }
  },
  onCancel: () => {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('rafiq_tour_completed', 'true');
    }
  },
};

// ── Dashboard Tour (English) ───────────────────────────────────────────────

export const DASHBOARD_TOUR_EN: TourScenario = {
  id: 'dashboard-tour-en',
  title: 'Dashboard Tour',
  description: 'Explore your health dashboard',
  category: 'onboarding',
  steps: [
    {
      id: 'dash-welcome-en',
      anchor: 'sidebar-panel',
      route: '/dashboard',
      speech: "Welcome, {{userName}}! This is the sidebar — use it to navigate between all sections of the platform.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
    },
    {
      id: 'dash-ai-summary-en',
      anchor: 'ai-health-summary-card',
      speech: "Here's your daily AI health summary with personalized insights and guidance for your condition.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
    },
    {
      id: 'dash-upcoming-appt-en',
      variants: {
        populated: {
          anchor: 'upcoming-appointment-detail',
          speech: "And here are the details of your upcoming doctor appointment.",
        },
        empty: {
          anchor: 'add-appointment-button',
          speech: "Here you can add your next doctor appointment if you haven't scheduled one yet.",
        },
      },
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'dash-family-en',
      anchor: 'family-overview-section',
      speech: "And here's a quick look at the health status of your family members.",
      speechLanguage: 'en-US',
      avatarState: 'celebrate',
      highlight: true,
    },
    {
      id: 'dash-recent-records-en',
      anchor: 'recent-records-card',
      speech: "Here are the latest medical records that were added. You can view them or upload new ones.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
    },
    {
      id: 'dash-medications-en',
      anchor: 'medications-reminders-card',
      speech: "And here are your medications and reminders so you never miss a dose.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'dash-schedule-en',
      anchor: 'today-schedule-card',
      speech: "Here's your daily schedule — appointments and reminders arranged in chronological order.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
    },
    {
      id: 'dash-ask-ai-en',
      anchor: 'ask-ai-button',
      speech: "Press here anytime to chat with me. Wishing you great health!",
      speechLanguage: 'en-US',
      avatarState: 'celebrate',
      highlight: true,
    },
  ],
};

// ── Medical Records Tour (English) ────────────────────────────────────────

export const MEDICAL_RECORDS_TOUR_EN: TourScenario = {
  id: 'medical-records-tour-en',
  title: 'Medical Records Tour',
  description: 'Explore the Medical Records page',
  category: 'onboarding',
  steps: [
    {
      id: 'mr-lab-en',
      anchor: 'records-card-lab',
      route: '/medical-records',
      speech: "On the Medical Records page you'll find five sections. First: Lab Results.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-prescription-en',
      anchor: 'records-card-prescription',
      speech: "Prescriptions.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-imaging-en',
      anchor: 'records-card-imaging',
      speech: "Imaging.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-medicine-en',
      anchor: 'records-card-medicine',
      speech: "Medication boxes.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-general-en',
      anchor: 'records-card-general',
      speech: "And other documents. Use 'Scan with AI' and I'll read the document and extract information automatically, or 'Add Manually' to enter it yourself.",
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-add-en',
      anchor: 'add-record-button',
      speech: "At any time, press 'Add Record' to choose the document type and upload it directly.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
  ],
};

// ── Appointments Tour (English) ────────────────────────────────────────────

export const APPOINTMENTS_TOUR_EN: TourScenario = {
  id: 'appointments-tour-en',
  title: 'Appointments Tour',
  description: 'Explore the Appointments page',
  category: 'onboarding',
  steps: [
    {
      id: 'appt-add-en',
      anchor: 'add-appointment',
      route: '/appointments',
      speech: "On the Appointments page, press 'Add Appointment' to open a dialog where you choose the type: doctor visit, lab test, imaging, vaccination, dental, therapy session, or other. Then set the date, time, and reminder.",
      speechLanguage: 'en-US',
      avatarState: 'pointLeft',
      highlight: true,
    },
  ],
};

// ── Medications Tour (English) ─────────────────────────────────────────────

export const MEDICATIONS_TOUR_EN: TourScenario = {
  id: 'medications-tour-en',
  title: 'Medications Tour',
  description: 'Explore the Medications & Reminders page',
  category: 'onboarding',
  steps: [
    {
      id: 'med-main-en',
      route: '/medications',
      variants: {
        populated: {
          anchor: 'med-next-dose-card',
          speech: "Here you see 'Next Dose' showing the medication name, dose, and when the next dose is due. Press 'I Took It' as soon as you take it, and I'll send you a reminder if you're late.",
        },
        empty: {
          anchor: 'add-medication',
          speech: "On the Medications page, press 'Add Medication' to log your first one. Set the name and dose schedule, and I'll remind you exactly on time.",
        },
      },
      speechLanguage: 'en-US',
      avatarState: 'wave',
      highlight: true,
    },
  ],
};

// ── Family Profiles Tour (English) ────────────────────────────────────────

export const FAMILY_PROFILES_TOUR_EN: TourScenario = {
  id: 'family-profiles-tour-en',
  title: 'Family Profiles Tour',
  description: 'Explore the Family Profiles page',
  category: 'onboarding',
  steps: [
    {
      id: 'family-main-en',
      anchor: 'add-family-profile',
      route: '/family-profiles',
      speech: "On the Family Profiles page, select any member to find tabs for: Overview, Medical Records, Appointments, Medications, Reminders, and Health Summary. Everything about their health in one place.",
      speechLanguage: 'en-US',
      avatarState: 'celebrate',
      highlight: true,
    },
  ],
};

// ── My Profile Tour (English) ──────────────────────────────────────────────

export const MY_PROFILE_TOUR_EN: TourScenario = {
  id: 'my-profile-tour-en',
  title: 'My Profile Tour',
  description: 'Explore your personal profile page',
  category: 'onboarding',
  steps: [
    {
      id: 'profile-personal-en',
      anchor: 'profile-personal-info',
      route: '/my-profile',
      speech: "In My Profile you can update your personal information and profile photo.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-health-en',
      anchor: 'profile-health-info',
      speech: "And your health information such as blood type, height, and weight.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-allergies-en',
      anchor: 'profile-allergies',
      speech: "Below that: your allergies,",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-chronic-en',
      anchor: 'profile-chronic-diseases',
      speech: "chronic conditions,",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-emergency-en',
      anchor: 'profile-emergency-contacts',
      speech: "and emergency contacts — to complete any missing information.",
      speechLanguage: 'en-US',
      avatarState: 'thinking',
      highlight: true,
    },
  ],
};

export const DEFAULT_TOURS_EN: TourScenario[] = [
  ONBOARDING_TOUR_EN,
  WELCOME_TOUR_EN,
  DASHBOARD_TOUR_EN,
  MEDICAL_RECORDS_TOUR_EN,
  APPOINTMENTS_TOUR_EN,
  MEDICATIONS_TOUR_EN,
  FAMILY_PROFILES_TOUR_EN,
  MY_PROFILE_TOUR_EN,
];
