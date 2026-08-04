/**
 * @file default-tours.ts
 * @description Pre-defined default guided tour scenarios for the Rafiq Assistant.
 * Scripts are clean Egyptian-colloquial Arabic with تشكيل for correct TTS pronunciation — the
 * exact strings supplied by product/content, reproduced as-is (do not paraphrase or "clean up").
 */

import { TourScenario } from '../models/tour-scenario';

// Onboarding pins the mascot+bubble to this constant viewport point on every step (no anchor,
// no spotlight) rather than tracking a page element — keeps the group in the same visual spot
// throughout the whole onboarding flow, as requested.
const ONBOARDING_FIXED_POSITION = { x: 450, y: 250 };

// ── Onboarding Tour — Guided profile creation ─────────────────────────────

export const ONBOARDING_TOUR: TourScenario = {
  id: 'onboarding-tour',
  title: 'إعداد الملف الصحي',
  description: 'جولة تعريفيّة أثناء إعداد الملف الصحي الأول',
  category: 'onboarding',
  steps: [
    {
      id: 'onboarding-welcome-step',
      route: '/onboarding/welcome',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: 'أَهْلًا بِكَ يا {{userName}} فِي رَفِيق. أَنَا مُساعِدُكَ الصِّحِّي الذَّكِي، وسَأُساعِدُكَ الآن فِي إِعْداد مَلَفِّكَ الصِّحِّي خُطْوَة بِخُطْوَة، وسَيَسْتَغْرِق ذَلِكَ أَقَلَّ مِن دَقِيقَتَين.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      // Waits for the user to press "Get Started" (which calls nextStep() itself) rather than
      // auto-advancing — the mascot should not move on until the user actually acts.
      waitForUser: true,
    },
    {
      id: 'onboarding-step1-guide',
      route: '/onboarding/step1',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: 'فِي هَذِهِ الخُطْوَة، اُكْتُب بَياناتِكَ الأَساسِيَّة: تارِيخ المِيلاد، النَّوْع، الطُّول، الوَزْن، وفَصِيلَة الدَّم. هَذِهِ المَعْلُومات تُساعِدُنِي عَلَى مُتابَعَة حالَتِكَ الصِّحِّيَّة بِدِقَّة.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      // Waits for "Continue" (the form's own submit handler calls nextStep()) — the user must
      // actually fill in and submit their basic info before the tour moves on.
      waitForUser: true,
    },
    {
      id: 'onboarding-emergency-guide',
      route: '/onboarding/emergency',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: 'أَضِف هُنا بَيانات شَخْص قَرِيب مِنْكَ لِلتَّواصُل مَعَه فِي حالات الطَّوارِئ. لَو تَأَخَّرْتَ فِي أَخْذ دَواءٍ مُهِمّ، سَأُرْسِل لَه رِسالَة تَنْبِيه عَلَى الوَاتْساب فَوْرًا.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      waitForUser: true,
    },
    {
      id: 'onboarding-step2-guide',
      route: '/onboarding/step2',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: 'هَل لَدَيْكَ حَساسِيَّة مِن أَي طَعام أَو دَواء؟ إِن كانَ الأَمْر كَذَلِك، سَجِّلْها هُنا حَتَّى أُنَبِّهَكَ دائِمًا قَبْل وَصْف أَي عِلاج جَدِيد.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      waitForUser: true,
    },
    {
      id: 'onboarding-step3-guide',
      route: '/onboarding/step3',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: 'إِن كُنْتَ تُعانِي مِن أَي مَرَض مُزْمِن، سَجِّلْه هُنا لِأَتَمَكَّن مِن مُتابَعَة حالَتِكَ وتَقْدِيم النَّصائِح المُناسِبَة لَك.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      waitForUser: true,
    },
    {
      id: 'onboarding-step4-guide',
      route: '/onboarding/step4',
      fixedPosition: ONBOARDING_FIXED_POSITION,
      speech: 'مُمْتاز! هَذِهِ مُراجَعَة شامِلَة لِكُل البَيانات الَّتِي أَدْخَلْتَها. تَأَكَّد مِنْها، وبَعْدَها سَنَنْتَقِل مَعًا إِلَى لَوْحَة التَّحَكُّم.',
      speechLanguage: 'ar-EG',
      avatarState: 'celebrate',
      waitForUser: true,
    },
  ],
};

// ── Welcome Tour — Full Site Comprehensive Walkthrough ─────────────────────

export const WELCOME_TOUR: TourScenario = {
  id: 'welcome-tour',
  title: 'الجولة التعريفية الشاملة',
  description: 'جولة شاملة لشرح كافة مميزات وأقسام منصة رفيق',
  category: 'onboarding',
  steps: [

    // 1 ── Welcome (unanchored — centered mascot intro)
    {
      id: 'welcome-greeting',
      route: '/dashboard',
      speech: 'مَرْحَبًا بِكَ مُجَدَّدًا يا {{userName}}. يَسُرُّنِي اِنْضِمامُكَ إِلَى رَفِيق. سَآخُذُكَ الآن فِي جَوْلَة شامِلَة نَتَعَرَّف فِيها مَعًا عَلَى كُل صَفْحَة وكُل زِرّ فِي المِنَصَّة.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
    },

    // 2 ── Dashboard sidebar nav — split into one sub-step per nav item so the
    // highlight always matches the exact clause being spoken. Auto-chains through
    // 2a-2d; the last sub-step (2e) waits for the user, matching the original pacing.
    {
      id: 'dashboard-nav-dashboard',
      anchor: 'dashboard-nav',
      route: '/dashboard',
      speech: 'هَذِهِ هِيَ القائِمَة الجانِبِيَّة، بَوَّابَتُكَ لِكُل شَيء فِي رَفِيق: لَوْحَة التَّحَكُّم لِلنَّظْرَة العامَّة.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-records',
      anchor: 'medical-records-nav',
      speech: 'السِّجِلَّات الطِّبِّيَّة لِرَفْع التَّحالِيل والرُّوشِتَّات.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-appointments',
      anchor: 'appointments-nav',
      speech: 'المَواعِيد لِحَجْز زِياراتك الطبية.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-medications',
      anchor: 'medications-nav',
      speech: 'الأَدْوِيَة والتَّذْكِيرات لِمُتابَعَة الجُرَعات.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-family',
      anchor: 'family-profiles-nav',
      speech: 'مَلَفَّات العائِلَة لِإِدارَة صِحَّة كُل أَفْراد أُسْرَتِكَ.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'dashboard-nav-profile',
      anchor: 'profile-nav',
      speech: 'وأَخِيرًا مَلَفُّكَ الشَّخْصِي، حَيْثُ يُمْكِنُكَ إِدارَة بَياناتِكَ الصِّحِّيَّة والشَّخْصِيَّة الخاصَّة بِك.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },

    // 3 ── Ask AI button intro
    {
      id: 'ask-ai-button-intro',
      anchor: 'ask-ai-button',
      route: '/dashboard',
      speech: 'هَذا زِرّ \'اِسْأَل رَفِيق\'، وهُوَ مَوْجُود فِي أَعْلَى كُل صَفْحَة. اِضْغَط عَلَيْه فِي أَي وَقْت لَو عِنْدَكَ سُؤال طِبِّي، وسَأُجِيبُكَ فَوْرًا.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
    },

    // 4 ── AI Health Summary
    {
      id: 'ai-health-summary',
      anchor: 'ai-health-summary-card',
      route: '/dashboard',
      speech: 'هُنا تَجِد \'المُلَخَّص الصِّحِّي الذَّكِي\'. أَقُوم بِتَحْلِيل بَياناتِكَ الطِّبِّيَّة بِاسْتِمْرار وأُزَوِّدُكَ بِإِرْشادات مُخَصَّصَة لِحالَتِكَ. يُمْكِنُكَ الضَّغْط عَلَى \'افتح المساعد الذكي\' لِلتَّحَدُّث مَعِي عَن أَي تَفْصِيلَة فِيه.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
    },

    // 5 ── System status
    {
      id: 'system-status',
      anchor: 'system-status-card',
      route: '/dashboard',
      speech: 'هَذِهِ البِطاقَة تَعْرِض حالَة النِّظام: مُعالَجَة البَيانات، آخِر مُزامَنَة، وآخِر نُسْخَة اِحْتِياطِيَّة. وأَسْفَلَها مُعَدَّل نَبَضات القَلْب لِعائِلَتِكَ خِلال آخِر أُسْبُوع.',
      speechLanguage: 'ar-EG',
      avatarState: 'idle',
      highlight: true,
    },

    // 6 ── Upcoming appointment card (state-aware)
    {
      id: 'upcoming-appointment',
      route: '/dashboard',
      variants: {
        populated: {
          anchor: 'upcoming-appointment-detail',
          speech: 'فِي هَذِهِ البِطاقَة تَجِد تَفاصِيل مَوْعِدِكَ القادِم: اِسْم الطَّبِيب، التَّارِيخ، والوَقْت، حَتَّى تَبْقَى دائِمًا عَلَى اِطِّلاع.',
        },
        empty: {
          anchor: 'add-appointment-button',
          speech: 'فِي بِطاقَة \'المَوْعِد الجاي\' سَتَجِد مَوْعِدَكَ القادِم مَع الطَّبِيب فَوْر إِضافَتِه. لَيْسَ لَدَيْكَ مَوْعِد حَتَّى الآن، فَابْدَأ بِالضَّغْط عَلَى \'أَضِف مَوْعِد جَدِيد\' لِإِضافَة أَوَّل مَوْعِد.',
        },
      },
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },

    // 7 ── Family Overview (state-aware)
    {
      id: 'family-overview',
      route: '/dashboard',
      variants: {
        populated: {
          anchor: 'family-member-card',
          speech: 'هُنا تَجِد بِطاقَة لِكُل فَرْد فِي عائِلَتِكَ، تَتَضَمَّن النَّوْع والعُمْر. اِضْغَط عَلَى \'مُلَخَّص الحالَة الصِّحِّيَّة\' لِأَي مِنْهُم لِلحُصُول عَلَى مُلَخَّص صِحِّي فَوْرِيّ.',
        },
        empty: {
          anchor: 'add-family-member-button',
          speech: 'هُنا قِسْم \'نَظْرَة عائِلِيَّة\'، وحالِيًّا يَضُمّ مَلَفَّكَ فَقَط. اِضْغَط عَلَى \'أَضِف فَرْد لِلعِيلَة\' لِإِضافَة أَحَد أَفْراد أُسْرَتِكَ ومُتابَعَة حالَتِهِ الصِّحِّيَّة أَيْضًا.',
        },
      },
      speechLanguage: 'ar-EG',
      avatarState: 'celebrate',
      highlight: true,
    },

    // 8 ── Medical Records — split one sub-step per record-type card so the highlight
    // tracks the exact card named, then a closing sub-step on the Add Record button.
    {
      id: 'medical-records-lab',
      anchor: 'records-card-lab',
      route: '/medical-records',
      speech: 'فِي صَفْحَة \'السِّجِلَّات الطِّبِّيَّة\'، تَجِد خَمْسَة أَقْسام: تَحالِيل.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-prescription',
      anchor: 'records-card-prescription',
      speech: 'رُوشِتَّات.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-imaging',
      anchor: 'records-card-imaging',
      speech: 'أَشِعَّة.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-medicine',
      anchor: 'records-card-medicine',
      speech: 'عُلَب أَدْوِيَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-general',
      anchor: 'records-card-general',
      // "Scan with AI"/"Add Manually" describes the buttons that sit on every upload card
      // (including this last one), so it reads naturally as this card's closing sentence —
      // highlighting the top "Add Record" button for it was a mismatch (different feature).
      speech: 'ومُسْتَنَدات أُخْرَى. اِسْتَخْدِم \'مَسْح ضَوْئِي بِالذَّكاء الاِصْطِناعِي\' لِأَقُوم بِقِراءَة المُسْتَنَد واسْتِخْراج المَعْلُومات آلِيًّا، أَو \'اِكْتُب بِنَفْسِك\' لِإِدْخالِها بِنَفْسِك.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'medical-records-add',
      anchor: 'add-record-button',
      speech: 'وفِي أَي وَقْت، اِضْغَط عَلَى \'أَضِف سِجِلًّا\' لِتَخْتار نَوْع المُسْتَنَد وتَرْفَعَه مُباشَرَةً مِن هُنا.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },

    // 9 ── Appointments
    {
      id: 'appointments-step',
      anchor: 'add-appointment',
      route: '/appointments',
      speech: 'فِي صَفْحَة \'المَواعِيد\'، اِضْغَط عَلَى \'أَضِف مَوْعِد\' لِفَتْح نافِذَة تَخْتار مِنْها نَوْع المَوْعِد: زِيارَة طَبِيب، تَحْلِيل، أَشِعَّة، تَطْعِيم، كَشْف أَسْنان، جَلْسَة عِلاج، أَو مَوْعِد آخَر. بَعْد الاِخْتِيار، سَتُحَدِّد التَّارِيخ والوَقْت والتَّذْكِير.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },

    // 10 ── Medications (state-aware)
    {
      id: 'medications-step',
      route: '/medications',
      variants: {
        populated: {
          anchor: 'med-next-dose-card',
          speech: 'هُنا تَجِد \'الجُرْعَة الجايَة\'، ويُوَضِّح اِسْم الدَّواء، الجُرْعَة، ومَوْعِد الجُرْعَة القادِمَة. اِضْغَط عَلَى \'أَخَدْتُه\' فَوْر أَخْذ الدَّواء لِأُسَجِّل ذَلِكَ، وسَأُرْسِل لَكَ تَنْبِيهًا إِن تَأَخَّرْتَ.',
        },
        empty: {
          anchor: 'add-medication',
          speech: 'فِي صَفْحَة \'الأَدْوِيَة والتَّذْكِيرات\'، اِضْغَط عَلَى \'أَضِف دَوا\' لِتَسْجِيل أَوَّل دَواء. حَدِّد اِسْمَه ومَواعِيد الجُرَعات، وسَأَتَوَلَّى تَذْكِيرَكَ بِها فِي مَوْعِدِها بِالضَّبْط.',
        },
      },
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
    },

    // 11 ── Family Profiles
    {
      id: 'family-profiles-step',
      anchor: 'add-family-profile',
      route: '/family-profiles',
      speech: 'فِي صَفْحَة \'أَفْراد العِيلَة\'، اِخْتَر أَي فَرْد لِتَجِد تَبْوِيبات: نَظْرَة عامَّة، سِجِلَّات طِبِّيَّة، مَواعِيد، أَدْوِيَة، تَذْكِيرات، ومُلَخَّص صِحِّي. كُل مَعْلُومَة عَن صِحَّتِه فِي مَكان واحِد.',
      speechLanguage: 'ar-EG',
      avatarState: 'celebrate',
      highlight: true,
    },

    // 12 ── My Profile — split so the Health Information card gets its own highlight
    // instead of staying dark while the script describes blood type/height/weight.
    {
      id: 'profile-personal-info-step',
      anchor: 'profile-personal-info',
      route: '/my-profile',
      speech: 'فِي \'مَلَفِّي الشَّخْصِي\' يُمْكِنُكَ تَعْدِيل بَياناتِكَ الشَّخْصِيَّة وصُورَتِكَ.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-health-info-step',
      anchor: 'profile-health-info',
      speech: 'والمَعْلُومات الصِّحِّيَّة كَفَصِيلَة الدَّم والطُّول والوَزْن.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-allergies-step',
      anchor: 'profile-allergies',
      speech: 'وأَسْفَلَها أَقْسام الحَساسِيَّة،',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-chronic-step',
      anchor: 'profile-chronic-diseases',
      speech: 'الأَمْراض المُزْمِنَة،',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-emergency-step',
      anchor: 'profile-emergency-contacts',
      speech: 'وجِهات الطَّوارِئ لِإِكْمال أَي بَيانات ناقِصَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
    },

    // 13 ── Ask AI finish
    {
      id: 'ask-ai-finish',
      anchor: 'ask-ai-button',
      route: '/dashboard',
      speech: 'بِهَذا نَكُون قَد اِنْتَهَيْنا مِن الجَوْلَة. وفِي أَي وَقْت تَحْتاج فِيهِ مُساعَدَة، اِضْغَط عَلَى \'اِسْأَل رَفِيق\' فِي أَي صَفْحَة، وسَأَكُون مَعَك. أَتَمَنَّى لَكَ دَوام الصِّحَّة والعافِيَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'celebrate',
      highlight: true,
    },
  ],
  onStart: () => console.log('[WelcomeTour] started'),
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

// ── Dashboard Tour ──────────────────────────────────────────────────────────

export const DASHBOARD_TOUR: TourScenario = {
  id: 'dashboard-tour',
  title: 'جولة لوحة التحكم',
  description: 'تعرف على لوحة التحكم الصحية',
  category: 'onboarding',
  steps: [
    {
      id: 'dash-welcome',
      anchor: 'sidebar-panel',
      route: '/dashboard',
      speech: 'أَهْلًا بِكَ يا {{userName}}. هَذِهِ القائِمَة الجانِبِيَّة لِلتَّنَقُّل بَيْن أَقْسام المِنَصَّة.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
    },
    {
      id: 'dash-ai-summary',
      anchor: 'ai-health-summary-card',
      speech: 'هُنا مُلَخَّصُكَ الصِّحِّي الذَّكِي اليَوْمِي والإِرْشادات المُخَصَّصَة لِحالَتِكَ.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
    },
    {
      id: 'dash-upcoming-appt',
      variants: {
        populated: {
          anchor: 'upcoming-appointment-detail',
          speech: 'وهُنا تَفاصِيل مَوْعِدِكَ القادِم مَع الطَّبِيب.',
        },
        empty: {
          anchor: 'add-appointment-button',
          speech: 'وهُنا يُمْكِنُكَ إِضافَة مَوْعِدِكَ القادِم مَع الطَّبِيب لَو لَم يَكُن لَدَيْكَ مَوْعِد بَعْد.',
        },
      },
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'dash-family',
      anchor: 'family-overview-section',
      speech: 'وهُنا نَظْرَة سَرِيعَة عَلَى الحالَة الصِّحِّيَّة لِأَفْراد عائِلَتِكَ.',
      speechLanguage: 'ar-EG',
      avatarState: 'celebrate',
      highlight: true,
    },
    {
      id: 'dash-recent-records',
      anchor: 'recent-records-card',
      speech: 'هُنا آخِر السِّجِلَّات الطِّبِّيَّة المُضافَة، يُمْكِنُكَ الاطِّلَاع عَلَيْها أَو رَفْع سِجِلَّات جَدِيدَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
    },
    {
      id: 'dash-medications',
      anchor: 'medications-reminders-card',
      speech: 'وهُنا أَدوِيَتُكَ وتَذْكِيراتُها حَتَّى لا تَفُوتَكَ جُرْعَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },
    {
      id: 'dash-schedule',
      anchor: 'today-schedule-card',
      speech: 'وهُنا جَدْوَلُ يَوْمِكَ: المَواعِيد والتَّذْكِيرات مُرَتَّبَة بِالتَّسَلْسُل الزَّمَنِي.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
    },
    {
      id: 'dash-ask-ai',
      anchor: 'ask-ai-button',
      speech: 'اِضْغَط هُنا فِي أَي وَقْت لِلتَّحَدُّث مَعِي. أَتَمَنَّى لَكَ دَوام الصِّحَّة والعافِيَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'celebrate',
      highlight: true,
    },
  ],
};

// ── Medical Records Tour ───────────────────────────────────────────────────

export const MEDICAL_RECORDS_TOUR: TourScenario = {
  id: 'medical-records-tour',
  title: 'جولة السجلات الطبية',
  description: 'تعرف على صفحة السجلات الطبية',
  category: 'onboarding',
  steps: [
    {
      id: 'mr-lab',
      anchor: 'records-card-lab',
      route: '/medical-records',
      speech: 'فِي صَفْحَة السِّجِلَّات الطِّبِّيَّة تَجِد خَمْسَة أَقْسام: أَوَّلًا التَّحالِيل.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-prescription',
      anchor: 'records-card-prescription',
      speech: 'الرُّوشِتَّات.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-imaging',
      anchor: 'records-card-imaging',
      speech: 'الأَشِعَّة.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-medicine',
      anchor: 'records-card-medicine',
      speech: 'عُلَب الأَدْوِيَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-general',
      anchor: 'records-card-general',
      speech: 'ومُسْتَنَدات أُخْرَى. اسْتَخْدِم \'Scan with AI\' لِأَقُوم بِقِراءَة المُسْتَنَد واسْتِخْراج المَعْلُومات آلِيًّا، أَو \'Add Manually\' لِإِدْخالِها بِنَفْسِك.',
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'mr-add',
      anchor: 'add-record-button',
      speech: 'وفِي أَي وَقْت، اضْغَط عَلَى \'Add Record\' لِتَخْتار نَوْع المُسْتَنَد وتَرْفَعَه مُباشَرَةً.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },
  ],
};

// ── Appointments Tour ──────────────────────────────────────────────────────

export const APPOINTMENTS_TOUR: TourScenario = {
  id: 'appointments-tour',
  title: 'جولة المواعيد',
  description: 'تعرف على صفحة المواعيد',
  category: 'onboarding',
  steps: [
    {
      id: 'appt-add',
      anchor: 'add-appointment',
      route: '/appointments',
      speech: 'فِي صَفْحَة المَواعِيد، اضْغَط عَلَى \'Add Appointment\' لِفَتْح نافِذَة تَخْتار مِنْها نَوْع المَوْعِد: زِيارَة طَبِيب، تَحْلِيل، أَشِعَّة، تَطْعِيم، كَشْف أَسْنان، جَلْسَة عِلاج، أَو مَوْعِد آخَر. بَعْد الاِخْتِيار، سَتُحَدِّد التَّارِيخ والوَقْت والتَّذْكِير.',
      speechLanguage: 'ar-EG',
      avatarState: 'pointLeft',
      highlight: true,
    },
  ],
};

// ── Medications Tour ───────────────────────────────────────────────────────

export const MEDICATIONS_TOUR: TourScenario = {
  id: 'medications-tour',
  title: 'جولة الأدوية',
  description: 'تعرف على صفحة الأدوية والتذكيرات',
  category: 'onboarding',
  steps: [
    {
      id: 'med-main',
      route: '/medications',
      variants: {
        populated: {
          anchor: 'med-next-dose-card',
          speech: 'هُنا تَجِد \'Next Dose\'، ويُوَضِّح اِسْم الدَّواء، الجُرْعَة، ومَوْعِد الجُرْعَة القادِمَة. اضْغَط عَلَى \'I Took It\' فَوْر أَخْذ الدَّواء لِأُسَجِّل ذَلِكَ، وسَأُرْسِل لَكَ تَنْبِيهًا إِن تَأَخَّرْتَ.',
        },
        empty: {
          anchor: 'add-medication',
          speech: 'فِي صَفْحَة الأَدْوِيَة والتَّذْكِيرات، اضْغَط عَلَى \'Add Medication\' لِتَسْجِيل أَوَّل دَواء. حَدِّد اِسْمَه ومَواعِيد الجُرَعات، وسَأَتَوَلَّى تَذْكِيرَكَ بِها فِي مَوْعِدِها بِالضَّبْط.',
        },
      },
      speechLanguage: 'ar-EG',
      avatarState: 'wave',
      highlight: true,
    },
  ],
};

// ── Family Profiles Tour ───────────────────────────────────────────────────

export const FAMILY_PROFILES_TOUR: TourScenario = {
  id: 'family-profiles-tour',
  title: 'جولة ملفات العائلة',
  description: 'تعرف على صفحة ملفات العائلة',
  category: 'onboarding',
  steps: [
    {
      id: 'family-main',
      anchor: 'add-family-profile',
      route: '/family-profiles',
      speech: 'فِي صَفْحَة \'Family Profiles\'، اخْتَر أَي فَرْد لِتَجِد تَبْوِيبات: نَظْرَة عامَّة، سِجِلَّات طِبِّيَّة، مَواعِيد، أَدْوِيَة، تَذْكِيرات، ومُلَخَّص صِحِّي. كُل مَعْلُومَة عَن صِحَّتِه فِي مَكان واحِد.',
      speechLanguage: 'ar-EG',
      avatarState: 'celebrate',
      highlight: true,
    },
  ],
};

// ── My Profile Tour ────────────────────────────────────────────────────────

export const MY_PROFILE_TOUR: TourScenario = {
  id: 'my-profile-tour',
  title: 'جولة ملفي الشخصي',
  description: 'تعرف على صفحة ملفك الشخصي',
  category: 'onboarding',
  steps: [
    {
      id: 'profile-personal',
      anchor: 'profile-personal-info',
      route: '/my-profile',
      speech: 'فِي \'My Profile\' يُمْكِنُكَ تَعْدِيل بَياناتِكَ الشَّخْصِيَّة وصُورَتِكَ.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-health',
      anchor: 'profile-health-info',
      speech: 'والمَعْلُومات الصِّحِّيَّة كَفَصِيلَة الدَّم والطُّول والوَزْن.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-allergies',
      anchor: 'profile-allergies',
      speech: 'وأَسْفَلَها أَقْسام الحَساسِيَّة،',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-chronic',
      anchor: 'profile-chronic-diseases',
      speech: 'الأَمْراض المُزْمِنَة،',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
      delayAfterMs: 300,
    },
    {
      id: 'profile-emergency',
      anchor: 'profile-emergency-contacts',
      speech: 'وجِهات الطَّوارِئ لِإِكْمال أَي بَيانات ناقِصَة.',
      speechLanguage: 'ar-EG',
      avatarState: 'thinking',
      highlight: true,
    },
  ],
};

export const DEFAULT_TOURS: TourScenario[] = [
  ONBOARDING_TOUR,
  WELCOME_TOUR,
  DASHBOARD_TOUR,
  MEDICAL_RECORDS_TOUR,
  APPOINTMENTS_TOUR,
  MEDICATIONS_TOUR,
  FAMILY_PROFILES_TOUR,
  MY_PROFILE_TOUR,
];
