import { apiClient } from './client'

export interface DormAllocationDto {
  id: string
  userId: string
  userFirstName: string | null
  userLastName: string | null
  userMatriculationCode: string | null
  dormitoryId: string
  dormitoryName: string
  campusName: string
  allocationPeriodId: string
  round: number
  status: string
  allocatedAt: string
  respondedAt: string | null
}

export function getMyAllocation(periodId: string): Promise<DormAllocationDto | null> {
  return apiClient<DormAllocationDto | null>(`/allocations/me/${periodId}`)
}

export function getAllocationsForPeriod(periodId: string): Promise<DormAllocationDto[]> {
  return apiClient<DormAllocationDto[]>(`/allocations/periods/${periodId}`)
}

export function advanceRound(periodId: string): Promise<void> {
  return apiClient<void>(`/allocations/periods/${periodId}/advance-round`, { method: 'POST' })
}
