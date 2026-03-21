export interface LoginResponse {
  token: string;
  email: string;
  name: string;
  role: string;
  expires: string;
}

export interface User {
  id?: number;
  name: string;
  email: string;
  phoneNumber?: string;
  role?: string;
}