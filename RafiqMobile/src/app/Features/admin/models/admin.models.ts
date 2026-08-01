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
