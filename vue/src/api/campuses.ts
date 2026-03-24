import { apiClient } from './client'
import type { DormitoryDto } from './dormitories'

export interface CampusDto {
  id: string
  name: string
  address: string | null
}

export interface CampusDetailDto extends CampusDto {
  dormitories: DormitoryDto[]
}

export interface CreateCampusRequest {
  name: string
  address?: string
}

export interface UpdateCampusRequest {
  name: string
  address?: string
}

export function getCampuses(): Promise<CampusDto[]> {
  return apiClient<CampusDto[]>('/campuses')
}

export function getCampusById(id: string): Promise<CampusDetailDto> {
  return apiClient<CampusDetailDto>(`/campuses/${id}`)
}

export function createCampus(data: CreateCampusRequest): Promise<CampusDto> {
  return apiClient<CampusDto>('/campuses', { method: 'POST', body: data })
}

export function updateCampus(id: string, data: UpdateCampusRequest): Promise<void> {
  return apiClient<void>(`/campuses/${id}`, { method: 'PUT', body: data })
}

export function deleteCampus(id: string): Promise<void> {
  return apiClient<void>(`/campuses/${id}`, { method: 'DELETE' })
}
