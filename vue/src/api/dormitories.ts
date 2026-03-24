import { apiClient } from './client'
import type { RoomDto } from './rooms'

export interface DormitoryDto {
  id: string
  name: string
  campusId: string
}

export interface DormitoryDetailDto extends DormitoryDto {
  rooms: RoomDto[]
}

export interface CreateDormitoryRequest {
  name: string
  campusId: string
}

export interface UpdateDormitoryRequest {
  name: string
}

export function getDormitories(campusId: string): Promise<DormitoryDto[]> {
  return apiClient<DormitoryDto[]>(`/dormitories?campusId=${campusId}`)
}

export function getDormitoryById(id: string): Promise<DormitoryDetailDto> {
  return apiClient<DormitoryDetailDto>(`/dormitories/${id}`)
}

export function createDormitory(data: CreateDormitoryRequest): Promise<DormitoryDto> {
  return apiClient<DormitoryDto>('/dormitories', { method: 'POST', body: data })
}

export function updateDormitory(id: string, data: UpdateDormitoryRequest): Promise<void> {
  return apiClient<void>(`/dormitories/${id}`, { method: 'PUT', body: data })
}

export function deleteDormitory(id: string): Promise<void> {
  return apiClient<void>(`/dormitories/${id}`, { method: 'DELETE' })
}
