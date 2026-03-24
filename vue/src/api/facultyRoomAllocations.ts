import { apiClient } from './client'

export interface FacultyRoomAllocationDto {
  id: string
  facultyId: string
  roomId: string
  allocationPeriodId: string
}

export interface AssignRoomsRequest {
  facultyId: string
  allocationPeriodId: string
  roomIds: string[]
}

export interface RemoveRoomAssignmentsRequest {
  facultyId: string
  allocationPeriodId: string
  roomIds: string[]
}

export function getFacultyRoomAllocations(
  periodId: string,
  facultyId?: string,
): Promise<FacultyRoomAllocationDto[]> {
  const params = new URLSearchParams({ periodId })
  if (facultyId) params.set('facultyId', facultyId)
  return apiClient<FacultyRoomAllocationDto[]>(`/faculty-room-allocations?${params}`)
}

export function assignRooms(data: AssignRoomsRequest): Promise<{ count: number }> {
  return apiClient<{ count: number }>('/faculty-room-allocations', { method: 'POST', body: data })
}

export function removeRoomAssignments(data: RemoveRoomAssignmentsRequest): Promise<void> {
  return apiClient<void>('/faculty-room-allocations/remove', { method: 'POST', body: data })
}
