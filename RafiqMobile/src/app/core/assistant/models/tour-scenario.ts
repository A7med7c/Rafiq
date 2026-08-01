/**
 * @file tour-scenario.ts
 * @description Data models and interfaces for the Rafiq Assistant Tour Engine.
 * Responsibility: Defines TourScenario, TourStepScenario, and TourState structures.
 */

import { AvatarState } from './assistant-state';
import { ElementPlacement } from '../services/avatar-position.service';

/** A concrete speech/anchor pairing used for one branch of a state-aware step. */
export interface TourStepVariant {
  /** Message text or localization key to speak via SpeechService */
  speech: string;

  /** Anchor name registered in AssistantAnchorRegistryService or DOM CSS selector */
  anchor?: string;

  /** Dynamic key-value parameters for localized speech string interpolation */
  speechParams?: Record<string, any>;
}

export interface TourStepScenario {
  /** Optional step identifier */
  id?: string;

  /** Anchor name registered in AssistantAnchorRegistryService or DOM CSS selector */
  anchor?: string;

  /** Route path to navigate to before executing this step (e.g. '/dashboard', '/appointments') */
  route?: string;

  /** Message text or localization key to speak via SpeechService */
  speech?: string;

  /** Dynamic key-value parameters for localized speech string interpolation */
  speechParams?: Record<string, any>;

  /**
   * State-aware content: when set, the engine checks whether `populated.anchor` currently
   * exists in the DOM. If it does, `populated` is used; otherwise `empty` is used instead.
   * Both variants go through the exact same anchor-resolution/highlight/position path —
   * only WHICH anchor id gets requested changes.
   */
  variants?: {
    populated: TourStepVariant;
    empty: TourStepVariant;
  };

  /** Language code for speech synthesis (defaults to 'ar-EG') */
  speechLanguage?: string;

  /** Avatar animation gesture during this step (idle, wave, listening, thinking, speaking, pointLeft, pointRight, celebrate, error) */
  avatarState?: AvatarState;

  /** Placement relative to anchor element ('left' | 'right' | 'top' | 'bottom') */
  placement?: ElementPlacement;

  /**
   * Pins the bubble+mascot to a constant viewport point instead of chasing a page anchor.
   * When set, the step is treated as unanchored: no element is resolved, no spotlight is
   * ever drawn (regardless of `highlight`), and the group renders at the same spot on every
   * step that sets it — used by scenarios (e.g. onboarding) that want a fixed presence.
   */
  fixedPosition?: { x: number; y: number };

  /**
   * Highlighting configuration using Driver.js spotlight.
   * If boolean true, highlights anchor element.
   * Can also pass custom Driver.js options.
   */
  highlight?: boolean | {
    popoverTitle?: string;
    popoverDescription?: string;
    stagePadding?: number;
    stageRadius?: number;
  };

  /**
   * User interaction requirement before proceeding.
   * If true or configured, engine waits for user action or next button click.
   */
  waitForUser?: boolean | {
    event?: 'click' | 'nextButton' | 'action';
    targetAnchor?: string;
    timeoutMs?: number;
  };

  /** Delay in milliseconds before executing step actions */
  delayBeforeMs?: number;

  /** Delay in milliseconds after speech/action before advancing automatically */
  delayAfterMs?: number;

  /** Optional custom execution callback function */
  action?: () => void | Promise<void>;
}

export interface TourScenario {
  /** Unique scenario ID (e.g. 'dashboard-tour', 'appointments-tour') */
  id: string;

  /** Human-readable title or translation key */
  title: string;

  /** Optional description of what this tour covers */
  description?: string;

  /** Scenario category ('onboarding' | 'feature' | 'guided') */
  category?: 'onboarding' | 'feature' | 'guided';

  /** Ordered steps included in this tour scenario */
  steps: TourStepScenario[];

  /** Callback fired when tour begins */
  onStart?: () => void;

  /** Callback fired when tour completes successfully */
  onComplete?: () => void;

  /** Callback fired when tour is cancelled or stopped early */
  onCancel?: () => void;
}

export interface TourState {
  scenario: TourScenario | null;
  currentStepIndex: number;
  currentStep: TourStepScenario | null;
  totalSteps: number;
  isPlaying: boolean;
  isPaused: boolean;
  isCompleted: boolean;
  isWaitingForUser: boolean;
}
