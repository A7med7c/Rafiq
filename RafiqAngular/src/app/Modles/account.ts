import { UserRole } from './register-request';

export interface Account {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  phoneNumberConfirmed: boolean;
  role: UserRole;
  profileImageUrl?: string | null;
}
