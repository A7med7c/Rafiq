export interface AdminTrendPoint {
  label: string;
  value: number;
}

export interface AdminDistributionItem {
  label: string;
  value: number;
}

export interface AdminRecentUser {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  profileImageUrl?: string | null;
}

export interface AdminRecentAppointment {
  id: string;
  title: string;
  provider: string;
  patientName: string;
  appointmentDateTime: string;
  status: string;
}

export interface AdminDashboard {
  totalUsers: number;
  activeUsers: number;
  totalProfiles: number;
  managedProfiles: number;
  appointmentsToday: number;
  appointmentsThisMonth: number;
  pendingAppointments: number;
  completedAppointments: number;
  medicationRemindersToday: number;
  medicalDocuments: number;
  aiConversations: number;
  newRegistrationsThisMonth: number;
  monthlyGrowthPercent: number;
  userGrowth: AdminTrendPoint[];
  appointmentTrend: AdminTrendPoint[];
  genderDistribution: AdminDistributionItem[];
  recentUsers: AdminRecentUser[];
  recentAppointments: AdminRecentAppointment[];
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  role: string;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumberConfirmed: boolean;
  createdAt: string;
  profileImageUrl?: string | null;
  hasHealthProfile: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminUserQuery {
  search?: string;
  status?: 'active' | 'inactive' | '';
  role?: 'Admin' | 'User' | '';
  sortBy?: 'createdAt' | 'name' | 'email' | 'status';
  sortDirection?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

// ── AI Operations ──────────────────────────────────────────────────────────

/** Per-day entry for the 7-day quality trend chart. */
export interface AiQualityDay {
  date: string;
  requests: number;
  failed: number;
  thumbsUp: number;
  thumbsDown: number;
}

/** Composite quality overview — health score + quality KPIs. */
export interface AiOverview {
  aiHealthScore: number;           // 0-100 composite metric
  positiveRatePercent: number;     // thumbs-up / total reactions
  unreviewedNegative: number;      // unreviewed negative feedback count
  failedLast7Days: number;
  avgResponseTimeMs: number;
  requestsToday: number;
  totalChatConversations: number;
  totalVoiceSessions: number;
  qualityTrend: AiQualityDay[];    // last 7 days
}

// Conversations

export interface AiRequestItem {
  id: string;
  userId?: string | null;
  userName?: string | null;
  userEmail?: string | null;
  feature: string;
  modelName: string;
  success: boolean;
  errorType?: string | null;
  durationMs: number;
  createdAt: string;
}

export interface AiRequestQuery {
  search?: string;
  feature?: string;
  status?: 'success' | 'failed' | '';
  from?: string;
  to?: string;
  sortBy?: 'createdAt' | 'duration' | 'feature';
  sortDirection?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

/** Detail payload shown in the conversations drawer. */
export interface AiRequestDetail {
  id: string;
  feature: string;
  modelName: string;
  success: boolean;
  errorType?: string | null;
  durationMs: number;
  createdAt: string;
  userId?: string | null;
  userName?: string | null;
  userEmail?: string | null;
  conversationId?: string | null;
  userPrompt?: string | null;
  aiResponse?: string | null;
}

// Feedback

export interface AiFeedbackItem {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  reaction: string;           // 'ThumbsUp' | 'ThumbsDown'
  userComment?: string | null;
  category?: string | null;
  triageStatus: string;       // 'New' | 'Investigating' | 'Resolved' | 'Ignored'
  adminNotes?: string | null;
  messageExcerpt: string;     // ≤80 chars — shown in compact row
  messageContent: string;     // full AI message — shown when expanded
  userPrompt?: string | null; // user question that preceded this AI response
  conversationId: string;
  createdAt: string;
}

export interface AiInsights {
  mostUsedFeature?: string | null;
  mostUsedFeatureCount: number;
  highestErrorFeature?: string | null;
  highestErrorCount: number;
  slowestFeature?: string | null;
  slowestFeatureAvgMs: number;
  fastestFeature?: string | null;
  fastestFeatureAvgMs: number;
  mostReportedCategory?: string | null;
  mostReportedCategoryCount: number;
}

export interface AiFeedbackQuery {
  search?: string;
  reaction?: 'ThumbsUp' | 'ThumbsDown' | '';
  status?: string;
  category?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

export interface UpdateAiFeedbackRequest {
  triageStatus: string;
  category?: string | null;
  adminNotes?: string | null;
}

// Performance

export interface AiDailyStat {
  date: string;
  requests: number;
  failed: number;
  avgDurationMs: number;
}

export interface AiPerformance {
  daily: AiDailyStat[];
  requestsByFeature: AdminDistributionItem[];
  slowRequests: number;
  p95LatencyMs: number;
  avgLatencyMs: number;
  errorRatePercent: number;
}


// ── Reviews ────────────────────────────────────────────────────────────────

export type ReviewStatus = 'Pending' | 'Reviewed' | 'Resolved' | 'Archived';
export type ReviewCategory = 'General' | 'BugReport' | 'FeatureRequest' | 'Performance' | 'UiUx' | 'ContentQuality';

export interface AdminReview {
  id: string;
  displayName: string;
  stars: number;
  comment: string | null;
  isVisible: boolean;
  status: ReviewStatus;
  category: ReviewCategory;
  adminNotes: string | null;
  adminReply: string | null;
  repliedAt: string | null;
  reviewedAt: string | null;
  deviceInfo: string | null;
  appVersion: string | null;
  appLanguage: string | null;
  createdAt: string;
}

export interface AdminReviewsPage {
  items: AdminReview[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ReviewStats {
  total: number;
  visible: number;
  hidden: number;
  averageStars: number;
  fiveStars: number;
  fourStars: number;
  threeStars: number;
  twoStars: number;
  oneStar: number;
}

export interface ReviewOverview {
  total: number;
  pending: number;
  thisWeek: number;
  averageStars: number;
  healthScore: number;
  positiveRate: number;
  withReply: number;
  fiveStars: number;
  fourStars: number;
  threeStars: number;
  twoStars: number;
  oneStar: number;
  visible: number;
  hidden: number;
  byCategory: Record<string, number>;
  byStatus: Record<string, number>;
}

export interface ReviewTrendPoint {
  month: string;
  averageStars: number;
  count: number;
}

export interface AdminReviewQuery {
  page?: number;
  pageSize?: number;
  status?: ReviewStatus | '';
  category?: ReviewCategory | '';
  minStars?: number | null;
  maxStars?: number | null;
  sortBy?: string;
}

// ── Audit Logs ─────────────────────────────────────────────────────────────

export type AuditSeverity = 'Info' | 'Success' | 'Warning' | 'Critical';
export type AuditModule   = 'Users' | 'Reviews' | 'AI Operations' | 'Settings' | 'System';

export interface AuditChange {
  field: string;
  before: string | null;
  after:  string | null;
}

export interface AuditLogEntry {
  id:          string;
  timestamp:   string;
  actorName:   string;
  actorEmail:  string;
  module:      AuditModule;
  action:      string;
  target:      string;
  severity:    AuditSeverity;
  description: string;
  changes:     AuditChange[];
}

export interface AuditLogSummary {
  total:         number;
  today:         number;
  critical:      number;
  adminActions:  number;
  infoCount:     number;
  successCount:  number;
  warningCount:  number;
  criticalCount: number;
}

export interface AuditLogQuery {
  search?:   string;
  module?:   string;
  severity?: string;
  dateFrom?: string;
  dateTo?:   string;
  page?:     number;
  pageSize?: number;
}
