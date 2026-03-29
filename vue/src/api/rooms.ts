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
