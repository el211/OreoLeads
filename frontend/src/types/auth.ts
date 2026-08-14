export interface UserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  organizationId: string | null
  organizationName: string | null
  roles: string[]
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: UserDto
}

export interface RegisterDto {
  email: string
  password: string
  firstName: string
  lastName: string
  organizationName?: string
  inviteCode?: string
}

export interface LoginDto {
  email: string
  password: string
}

export interface ChangePasswordDto {
  currentPassword: string
  newPassword: string
}
