import { apiClient } from './client'

export type RoomSizePreference = 2 | 3
export type GroupStatus = 'Forming' | 'Locked' | 'Disbanded' | 'Expired'

export interface GroupMemberDto {
  userId: string
  firstName: string
  lastName: string
  isLeader: boolean
}

export interface GroupDto {
  id: string
  allocationPeriodId: string
  dormitoryId: string
  dormitoryName: string
  leaderUserId: string
  roomSizePreference: number
  status: GroupStatus
  createdAt: string
  expiresAt: string
  lockedAt: string | null
  members: GroupMemberDto[]
}

export interface InvitationDto {
  id: string
  groupId: string
  dormitoryName: string
  roomSizePreference: number
  inviterUserId: string
  inviterFirstName: string
  inviterLastName: string
  createdAt: string
}

export interface EligibleStudentDto {
  userId: string
  firstName: string
  lastName: string
  matriculationCode: string
}

export interface CompatibilityCandidateDto {
  userId: string
  firstName: string
  lastName: string
  score: number
  signalsUsed: string[]
}

export function createGroup(roomSizePreference: RoomSizePreference): Promise<{ id: string }> {
  return apiClient<{ id: string }>('/groups', { method: 'POST', body: { roomSizePreference } })
}

export function getMyGroup(): Promise<GroupDto | null> {
  return apiClient<GroupDto | null>('/groups/me')
}

export function inviteToGroup(groupId: string, inviteeUserId: string): Promise<void> {
  return apiClient<void>(`/groups/${groupId}/invite`, { method: 'POST', body: { inviteeUserId } })
}

export function leaveGroup(groupId: string): Promise<void> {
  return apiClient<void>(`/groups/${groupId}/leave`, { method: 'POST' })
}

export function disbandGroup(groupId: string): Promise<void> {
  return apiClient<void>(`/groups/${groupId}/disband`, { method: 'POST' })
}

export function changeRoomSizePreference(groupId: string, newPreference: RoomSizePreference): Promise<void> {
  return apiClient<void>(`/groups/${groupId}/preference`, { method: 'PATCH', body: { newPreference } })
}

export function lockGroup(groupId: string): Promise<void> {
  return apiClient<void>(`/groups/${groupId}/lock`, { method: 'POST' })
}

export function searchEligibleStudents(query: string, periodId: string): Promise<EligibleStudentDto[]> {
  const q = encodeURIComponent(query)
  return apiClient<EligibleStudentDto[]>(`/groups/search?q=${q}&periodId=${periodId}`)
}

export function getCompatibleSuggestions(groupId: string): Promise<CompatibilityCandidateDto[]> {
  return apiClient<CompatibilityCandidateDto[]>(`/groups/${groupId}/suggestions`)
}

export function getMyInvitations(): Promise<InvitationDto[]> {
  return apiClient<InvitationDto[]>('/group-invitations/me')
}

export function acceptInvitation(id: string): Promise<void> {
  return apiClient<void>(`/group-invitations/${id}/accept`, { method: 'POST' })
}

export function declineInvitation(id: string): Promise<void> {
  return apiClient<void>(`/group-invitations/${id}/decline`, { method: 'POST' })
}
