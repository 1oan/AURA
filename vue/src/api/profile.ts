import { apiClient } from './client'

export interface LifestyleDto {
  sleepSchedule: string
  wakeUpTime: string
  cleanlinessLevel: number
  noiseTolerance: string
  studyLocation: string
  guestFrequency: string
  smokingHabit: string
  completedAt: string
}

export interface TipiDto {
  extraversion: number
  agreeableness: number
  conscientiousness: number
  emotionalStability: number
  openness: number
  completedAt: string
}

export interface InterestsDto {
  slugs: string[]
  completedAt: string | null
}

export interface SpotifyDto {
  connected: boolean
  connectedAt: string | null
  scopes: string[]
}

export interface ProfileDto {
  completenessPercent: number
  hasLifestyleData: boolean
  lifestyle: LifestyleDto | null
  tipi: TipiDto | null
  interests: InterestsDto
  spotify: SpotifyDto
}

export interface SubmitLifestyleRequest {
  sleepSchedule: 'Early' | 'Normal' | 'Late'
  wakeUpTime: 'Early' | 'Normal' | 'Late'
  cleanlinessLevel: number
  noiseTolerance: 'Silent' | 'Some' | 'Any'
  studyLocation: 'Room' | 'Campus' | 'Mixed'
  guestFrequency: 'Rarely' | 'Weekly' | 'Daily'
  smokingHabit: 'No' | 'OutdoorsOnly' | 'Yes'
}

export interface StartSpotifyOAuthResult {
  authorizationUrl: string
}

export function getMyProfile(): Promise<ProfileDto> {
  return apiClient<ProfileDto>('/profile')
}

export function submitLifestyle(req: SubmitLifestyleRequest): Promise<void> {
  return apiClient<void>('/profile/lifestyle', { method: 'PUT', body: req })
}

export function submitTipi(answers: number[]): Promise<void> {
  return apiClient<void>('/profile/tipi', { method: 'PUT', body: { answers } })
}

export function setInterests(slugs: string[]): Promise<void> {
  return apiClient<void>('/profile/interests', { method: 'PUT', body: { slugs } })
}

export function startSpotifyConnect(): Promise<StartSpotifyOAuthResult> {
  return apiClient<StartSpotifyOAuthResult>('/profile/spotify/start')
}

export function disconnectSpotify(): Promise<void> {
  return apiClient<void>('/profile/spotify/disconnect', { method: 'POST' })
}
