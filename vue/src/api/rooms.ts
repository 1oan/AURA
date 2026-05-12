import { apiClient } from './client'

export interface RoomDto {
  id: string
  number: string
  dormitoryId: string
  floor: number
  capacity: number
  gender: string
}

export interface CreateRoomRequest {
  number: string
  dormitoryId: string
  floor: number
  capacity: number
  gender: string
}

export interface UpdateRoomRequest {
  number: string
  floor: number
  capacity: number
  gender: string
}

export interface FloorConfiguration {
  floorNumber: number
  roomCount: number
  capacity: number
  gender: string
}

export interface BulkCreateRoomsRequest {
  dormitoryId: string
  floors: FloorConfiguration[]
}

export function getRooms(dormitoryId: string): Promise<RoomDto[]> {
  return apiClient<RoomDto[]>(`/rooms?dormitoryId=${dormitoryId}`)
}

export function getRoomById(id: string): Promise<RoomDto> {
  return apiClient<RoomDto>(`/rooms/${id}`)
}

export function createRoom(data: CreateRoomRequest): Promise<RoomDto> {
  return apiClient<RoomDto>('/rooms', { method: 'POST', body: data })
}

export function bulkCreateRooms(data: BulkCreateRoomsRequest): Promise<{ count: number }> {
  return apiClient<{ count: number }>('/rooms/bulk', { method: 'POST', body: data })
}

export function updateRoom(id: string, data: UpdateRoomRequest): Promise<void> {
  return apiClient<void>(`/rooms/${id}`, { method: 'PUT', body: data })
}

export function deleteRoom(id: string): Promise<void> {
  return apiClient<void>(`/rooms/${id}`, { method: 'DELETE' })
}

export interface RoommateDto {
  userId: string
  firstName: string
  lastName: string
}

export interface RoomAssignmentDto {
  roomAssignmentId: string
  roomId: string
  roomNumber: string
  dormitoryName: string
  floor: number
  capacity: number
  assignedAt: string
  roommates: RoommateDto[]
}

export interface PeriodCountdownDto {
  allocationPeriodId: string
  periodName: string
  closingAtUtc: string
  hoursRemaining: number
}

export function placeMeNow(): Promise<void> {
  return apiClient<void>('/rooms/place-me-now', { method: 'POST' })
}

export function getMyRoom(): Promise<RoomAssignmentDto | null> {
  return apiClient<RoomAssignmentDto | null>('/rooms/me')
}

export function getPeriodCountdown(): Promise<PeriodCountdownDto | null> {
  return apiClient<PeriodCountdownDto | null>('/rooms/period-countdown')
}
