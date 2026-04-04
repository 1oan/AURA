import { apiClient } from './client'

export interface AuthResult {
  token: string
  userId: string
  email: string
  firstName: string
  lastName: string
  role: string
  isEmailConfirmed: boolean
}

export interface UserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  role: string
  matriculationCode: string | null
  createdAt: string
  isEmailConfirmed: boolean
}

export interface RegisterRequest {
  email: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

export function register(data: RegisterRequest): Promise<AuthResult> {
  return apiClient<AuthResult>('/auth/register', {
    method: 'POST',
    body: data,
  })
}

export function login(data: LoginRequest): Promise<AuthResult> {
  return apiClient<AuthResult>('/auth/login', {
    method: 'POST',
    body: data,
  })
}

export function confirmEmail(code: string): Promise<void> {
  return apiClient<void>('/auth/confirm-email', {
    method: 'POST',
    body: { code },
  })
}

export function resendConfirmation(): Promise<void> {
  return apiClient<void>('/auth/resend-confirmation', {
    method: 'POST',
  })
}

export function getCurrentUser(): Promise<UserDto> {
  return apiClient<UserDto>('/users/me')
}

export function setMatriculationCode(matriculationCode: string): Promise<void> {
  return apiClient<void>('/users/me/matriculation-code', {
    method: 'PATCH',
    body: { matriculationCode },
  })
}