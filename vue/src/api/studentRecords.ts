import { apiClient, apiUpload } from './client'

export interface StudentRecordDto {
  id: string
  matriculationCode: string
  firstName: string
  lastName: string
  points: number
  facultyId: string
  facultyName: string
  facultyAbbreviation: string
  allocationPeriodId: string
  userId: string | null
}

export interface CsvRowError {
  row: number
  message: string
}

export interface UploadCsvResult {
  created: number
  errors: CsvRowError[]
}

export function uploadStudentRecordsCsv(allocationPeriodId: string, file: File): Promise<UploadCsvResult> {
  return apiUpload<UploadCsvResult>(`/student-records/upload/${allocationPeriodId}`, file)
}

export function getStudentRecords(allocationPeriodId: string, facultyId?: string): Promise<StudentRecordDto[]> {
  const params = new URLSearchParams({ allocationPeriodId })
  if (facultyId) params.set('facultyId', facultyId)
  return apiClient<StudentRecordDto[]>(`/student-records?${params}`)
}

export interface ParticipateRequest {
  matriculationCode?: string
}

export interface ParticipateResult {
  facultyName: string
  facultyAbbreviation: string
  points: number
}

export interface MyEligibilityResult {
  isEligible: boolean
  hasParticipated: boolean
  facultyName: string | null
  facultyAbbreviation: string | null
  points: number | null
  matriculationCode: string | null
}

export function participate(allocationPeriodId: string, data?: ParticipateRequest): Promise<ParticipateResult> {
  return apiClient<ParticipateResult>(`/student-records/participate/${allocationPeriodId}`, {
    method: 'POST',
    body: data ?? {},
  })
}

export function getMyEligibility(allocationPeriodId: string): Promise<MyEligibilityResult> {
  return apiClient<MyEligibilityResult>(`/student-records/my-eligibility/${allocationPeriodId}`)
}
