import { UserRole } from './register-request';

export interface RegisterResponse {
  userId: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
  requiresPhoneVerification: boolean;
}
