import { ref } from 'vue'
import { getProfiles } from '../api/client'
import type { BehaviorProfile } from '../api/types'

export function useProfiles() {
  const profiles = ref<BehaviorProfile[]>([])
  const selectedProfile = ref<BehaviorProfile | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchProfiles() {
    loading.value = true
    error.value = null
    try {
      profiles.value = await getProfiles()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load profiles'
    } finally {
      loading.value = false
    }
  }

  function selectProfile(profile: BehaviorProfile) {
    selectedProfile.value = profile
  }

  return { profiles, selectedProfile, loading, error, fetchProfiles, selectProfile }
}
