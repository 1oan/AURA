import { apiClient } from './client'

export interface AvailableDormitoryDto {
  dormitoryId: string
  dormitoryName: string
  campusName: string
  availableSpots: number
}

export interface DormPreferenceDto {
  dormitoryId: string
  dormitoryName: string
  campusName: string
  rank: number
}

export interface PreferenceStatsDto {
  totalParticipants: number
  studentsWithPreferences: number
}

export function getAvailableDormitories(allocationPeriodId: string): Promise<AvailableDormitoryDto[]> {
  return apiClient<AvailableDormitoryDto[]>(`/dorm-preferences/available/${allocationPeriodId}`)
}

export function submitPreferences(allocationPeriodId: string, dormitoryIds: string[]): Promise<void> {
  return apiClient<void>(`/dorm-preferences/${allocationPeriodId}`, {
    method: 'PUT',
    body: { dormitoryIds },
  })
}

export function getMyPreferences(allocationPeriodId: string): Promise<DormPreferenceDto[]> {
  return apiClient<DormPreferenceDto[]>(`/dorm-preferences/my/${allocationPeriodId}`)
}

export function getPreferenceStats(allocationPeriodId: string): Promise<PreferenceStatsDto> {
  return apiClient<PreferenceStatsDto>(`/dorm-preferences/stats/${allocationPeriodId}`)
}
