<script setup lang="ts">
import type { BehaviorProfile } from '../api/types'

defineProps<{
    profiles: BehaviorProfile[]
    selectedProfile: BehaviorProfile | null
}>()

const emit = defineEmits<{
    select: [profile: BehaviorProfile]
}>()
</script>

<template>
    <ol>
        <li
            v-for="profile in profiles"
            :key="profile.id"
            :class="{ selected: selectedProfile?.id === profile.id }"
            @click="emit('select', profile)"
        >
            <span class="name">{{ profile.name }}</span>
            <span class="description"> - {{ profile.description }}</span>
        </li>
    </ol>
</template>

<style scoped>
ol {
    padding-left: 1.5rem;
}

li {
    cursor: pointer;
    padding: 0.25rem 0;
}

li.selected {
    font-weight: bold;
}

.description {
    color: #555;
    font-size: 0.9em;
}
</style>
