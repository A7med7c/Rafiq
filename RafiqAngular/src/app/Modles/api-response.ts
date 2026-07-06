export interface ApiResponseBase {
  success: boolean;
  message: string;
  errors: string[] | null;
}

export interface ApiResponse<T> extends ApiResponseBase {
  data: T;
}

export interface ApiErrorBody {
  success?: boolean;
  message?: string;
  errors?: string[] | null;
}
