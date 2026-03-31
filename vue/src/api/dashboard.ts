import { apiClient } from './client'

export interface ActivePeriodDto {
  id: string
  name: string
  status: string
  startDate: string
  endDate: string
}

export interface FacultyAllocationDto {
  facultyId: string
  facultyName: string
  abbreviation: string
  roomCount: number
}

export interface DashboardStatsDto {
  campusCount: number
  dormitoryCount: number
  totalRooms: number
  totalCapacity: number
  facultyCount: number
  activePeriod: ActivePeriodDto | null
  allocationsByFaculty: FacultyAllocationDto[]
}

export function getDashboardStats(): Promise<DashboardStatsDto> {
  return apiClient<DashboardStatsDto>('/dashboard/stats')
}