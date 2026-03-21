import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useProfiles } from './useProfiles'
import * as client from '../api/client'
import type { BehaviorProfile } from '../api/types'

vi.mock('../api/client')

const mockGetProfiles = vi.mocked(client.getProfiles)

beforeEach(() => {
  vi.clearAllMocks()
})

const profiles: BehaviorProfile[] = [
  {
    id: 'no-tools',
    name: 'No Tools',
    description: 'No tools permitted',
    recommendedModelTags: [],
    allowedMcpServerNames: [],
  },
  {
    id: 'qbittorrent-manager',
    name: 'qBitTorrent Manager',
    description: 'qBitTorrent plugin permitted',
    recommendedModelTags: [],
    allowedMcpServerNames: ['qbittorrent'],
  },
]

describe('useProfiles', () => {
  it('fetchProfiles populates profiles', async () => {
    mockGetProfiles.mockResolvedValue(profiles)

    const { profiles: result, fetchProfiles } = useProfiles()
    await fetchProfiles()

    expect(result.value).toEqual(profiles)
  })

  it('fetchProfiles sets error on failure', async () => {
    mockGetProfiles.mockRejectedValue(new Error('network error'))

    const { error, fetchProfiles } = useProfiles()
    await fetchProfiles()

    expect(error.value).toContain('network error')
  })

  it('selectProfile sets selectedProfile', () => {
    const { selectedProfile, selectProfile } = useProfiles()
    selectProfile(profiles[0])
    expect(selectedProfile.value).toEqual(profiles[0])
  })
})
