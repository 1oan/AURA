import { apiClient } from './client'

export interface FacultyDto {
  id: string
  name: string
  abbreviation: string
}

export interface CreateFacultyRequest {
  name: string
  abbreviation: string
}

export interface UpdateFacultyRequest {
  name: string
  abbreviation: string
}

export function getFaculties(): Promise<FacultyDto[]> {
  return apiClient<FacultyDto[]>('/faculties')
}

export function getFacultyById(id: string): Promise<FacultyDto> {
  return apiClient<FacultyDto>(`/faculties/${id}`)
}

export function createFaculty(data: CreateFacultyRequest): Promise<FacultyDto> {
  return apiClient<FacultyDto>('/faculties', { method: 'POST', body: data })
}

export function updateFaculty(id: string, data: UpdateFacultyRequest): Promise<void> {
  return apiClient<void>(`/faculties/${id}`, { method: 'PUT', body: data })
}

export function deleteFaculty(id: string): Promise<void> {
  return apiClient<void>(`/faculties/${id}`, { method: 'DELETE' })
}
