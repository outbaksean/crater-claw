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
    <ul class="profile-list">
        <li
            v-for="(profile, i) in profiles"
            :key="profile.id"
            :class="['item', { 'item--selected': selectedProfile?.id === profile.id }]"
            @click="emit('select', profile)"
        >
            <span class="item-index">{{ i + 1 }}</span>
            <span class="item-name">{{ profile.name }}</span>
            <span class="item-desc"> &mdash; {{ profile.description }}</span>
        </li>
    </ul>
</template>

<style scoped>
.profile-list {
    list-style: none;
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.item {
    display: flex;
    align-items: baseline;
    gap: 10px;
    padding: 8px 12px;
    border-radius: var(--radius);
    cursor: pointer;
    border-left: 3px solid transparent;
    transition: background var(--transition), border-color var(--transition);
}

.item:hover {
    background: var(--surface-raised);
}

.item--selected {
    border-left-color: var(--accent);
    background: var(--surface-raised);
}

.item-index {
    color: var(--text-dim);
    min-width: 16px;
    flex-shrink: 0;
    font-size: 11px;
}

.item-name {
    color: var(--text);
}

.item-desc {
    color: var(--text-dim);
    font-size: 11px;
}
</style>
