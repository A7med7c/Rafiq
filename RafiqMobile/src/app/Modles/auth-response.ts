export interface AuthResponse {

    success: boolean;

    message: string;

    data: {

        accessToken: string;

        refreshToken: string;

        accessTokenExpiresAt: string;

        refreshTokenExpiresAt: string;

        hasEmergencyContacts: boolean;

        profileImageUrl: string | null;

    };

}