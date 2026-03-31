import { apiClient } from './client'

export interface AllocationPeriodDto {
  id: string
  name: string
  startDate: string
  endDate: string
  status: string
}

export interface CreateAllocationPeriodRequest {
  name: string
  startDate: string
  endDate: string
}

export interface UpdateAllocationPeriodRequest {
  name: string
  startDate: string
  endDate: string
}

export function getAllocationPeriods(): Promise<AllocationPeriodDto[]> {
  return apiClient<AllocationPeriodDto[]>('/allocation-periods')
}

export function getAllocationPeriodById(id: string): Promise<AllocationPeriodDto> {
  return apiClient<AllocationPeriodDto>(`/allocation-periods/${id}`)
}

export function createAllocationPeriod(data: CreateAllocationPeriodRequest): Promise<AllocationPeriodDto> {
  return apiClient<AllocationPeriodDto>('/allocation-periods', { method: 'POST', body: data })
}

export function updateAllocationPeriod(id: string, data: UpdateAllocationPeriodRequest): Promise<void> {
  return apiClient<void>(`/allocation-periods/${id}`, { method: 'PUT', body: data })
}

export function activateAllocationPeriod(id: string): Promise<void> {
  return apiClient<void>(`/allocation-periods/${id}/activate`, { method: 'POST' })
}

export function startAllocating(id: string): Promise<void> {
  return apiClient<void>(`/allocation-periods/${id}/start-allocating`, { method: 'POST' })
}

export function closeAllocationPeriod(id: string): Promise<void> {
  return apiClient<void>(`/allocation-periods/${id}/close`, { method: 'POST' })
}

export function deleteAllocationPeriod(id: string): Promise<void> {
  return apiClient<void>(`/allocation-periods/${id}`, { method: 'DELETE' })
}
