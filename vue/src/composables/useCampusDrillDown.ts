import { ref, computed } from 'vue'
import { getCampusById } from '@/api/campuses'
import { getDormitoryById } from '@/api/dormitories'
import type { CampusDetailDto } from '@/api/campuses'
import type { DormitoryDetailDto } from '@/api/dormitories'

export type DrillLevel = 'campuses' | 'dormitories' | 'rooms'

export function useCampusDrillDown() {
  const level = ref<DrillLevel>('campuses')
  const selectedCampus = ref<CampusDetailDto | null>(null)
  const selectedDormitory = ref<DormitoryDetailDto | null>(null)
  const drilling = ref(false)

  const breadcrumbs = computed(() => {
    const crumbs: { label: string; action?: () => void }[] = [
      { label: 'Campuses', action: level.value !== 'campuses' ? () => drillBack('campuses') : undefined },
    ]
    if (selectedCampus.value && (level.value === 'dormitories' || level.value === 'rooms')) {
      crumbs.push({
        label: selectedCampus.value.name,
        action: level.value === 'rooms' ? () => drillBack('dormitories') : undefined,
      })
    }
    if (selectedDormitory.value && level.value === 'rooms') {
      crumbs.push({ label: selectedDormitory.value.name })
    }
    return crumbs
  })

  async function drillToCampus(campusId: string) {
    drilling.value = true
    try {
      selectedCampus.value = await getCampusById(campusId)
      level.value = 'dormitories'
    } finally {
      drilling.value = false
    }
  }

  async function drillToDormitory(dormitoryId: string) {
    drilling.value = true
    try {
      selectedDormitory.value = await getDormitoryById(dormitoryId)
      level.value = 'rooms'
    } finally {
      drilling.value = false
    }
  }

  function drillBack(to: DrillLevel = 'campuses') {
    if (to === 'campuses') {
      level.value = 'campuses'
      selectedCampus.value = null
      selectedDormitory.value = null
    } else if (to === 'dormitories') {
      level.value = 'dormitories'
      selectedDormitory.value = null
    }
  }

  // Refresh the current level's data after CRUD operations
  async function refreshCampus() {
    if (selectedCampus.value) {
      selectedCampus.value = await getCampusById(selectedCampus.value.id)
    }
  }

  async function refreshDormitory() {
    if (selectedDormitory.value) {
      selectedDormitory.value = await getDormitoryById(selectedDormitory.value.id)
    }
  }

  return {
    level,
    selectedCampus,
    selectedDormitory,
    drilling,
    breadcrumbs,
    drillToCampus,
    drillToDormitory,
    drillBack,
    refreshCampus,
    refreshDormitory,
  }
}
