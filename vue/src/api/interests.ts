import { apiClient } from './client'

export interface InterestItemDto {
  slug: string
  label: string
  displayOrder: number
}

export interface InterestCategoryDto {
  category: string
  items: InterestItemDto[]
}

export function getInterestCatalog(): Promise<InterestCategoryDto[]> {
  return apiClient<InterestCategoryDto[]>('/interests')
}
