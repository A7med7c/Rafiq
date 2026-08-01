import { UserRole } from './register-request';

export interface Account {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  phoneNumberConfirmed: boolean;
  emailConfirmed: boolean;
  role: UserRole;
  profileImageUrl?: string | null;
}
