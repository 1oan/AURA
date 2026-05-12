import { apiClient } from './client'

export interface UpgradeTargetDto {
  rank: number
  dormitoryId: string
  dormitoryName: string
  campusName: string
}

export interface UpgradeRequestDto {
  id: string
  allocationPeriodId: string
  createdAt: string
  targets: UpgradeTargetDto[]
}

export interface AvailableUpgradeTargetDto {
  dormitoryId: string
  dormitoryName: string
  campusName: string
}

export interface SubmitUpgradeRequestRequest {
  allocationPeriodId: string
  dormitoryIds: string[]
}

export function getMyUpgradeRequest(periodId: string): Promise<UpgradeRequestDto | null> {
  return apiClient<UpgradeRequestDto | null>(`/upgrade-requests/me/${periodId}`)
}

export function getAvailableUpgradeTargets(periodId: string): Promise<AvailableUpgradeTargetDto[]> {
  return apiClient<AvailableUpgradeTargetDto[]>(`/upgrade-requests/available-targets/${periodId}`)
}

export function submitUpgradeRequest(req: SubmitUpgradeRequestRequest): Promise<{ id: string }> {
  return apiClient<{ id: string }>('/upgrade-requests', { method: 'POST', body: req })
}

export function cancelUpgradeRequest(id: string): Promise<void> {
  return apiClient<void>(`/upgrade-requests/${id}`, { method: 'DELETE' })
}
