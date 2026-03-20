<script setup lang="ts">
import { ref } from 'vue'
import { postAgentic } from '../api/client'
import type { AgenticResponse } from '../api/types'

const props = defineProps<{
    providerName: string
    modelName: string
    profileId: string
}>()

const prompt = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
const result = ref<AgenticResponse | null>(null)

async function submit() {
    const content = prompt.value.trim()
    if (!content || loading.value) return
    loading.value = true
    error.value = null
    result.value = null
    try {
        result.value = await postAgentic(props.providerName, {
            modelName: props.modelName,
            prompt: content,
            profileId: props.profileId,
        })
    } catch (e) {
        error.value = e instanceof Error ? e.message : 'Agentic execution failed'
    } finally {
        loading.value = false
    }
}
</script>

<template>
    <div class="agentic">
        <form @submit.prevent="submit">
            <div class="input-row">
                <input
                    v-model="prompt"
                    type="text"
                    placeholder="Enter a task prompt..."
                    :disabled="loading"
                    aria-label="Task prompt"
                />
                <button type="submit" :disabled="loading || !prompt.trim()">
                    {{ loading ? 'Running...' : 'Run' }}
                </button>
            </div>
        </form>
        <p v-if="error" class="error">{{ error }}</p>
        <div v-if="result" class="result">
            <p class="finish-reason">Finish reason: {{ result.finishReason }}</p>
            <div v-if="result.toolsInvoked.length > 0" class="tools">
                <p class="tools-label">Tools invoked:</p>
                <ul>
                    <li v-for="tool in result.toolsInvoked" :key="tool">{{ tool }}</li>
                </ul>
            </div>
            <p v-else class="no-tools">No tools invoked.</p>
            <div class="response">
                <p class="response-label">Response:</p>
                <p class="response-content">{{ result.content }}</p>
            </div>
        </div>
    </div>
</template>

<style scoped>
.agentic {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.input-row {
    display: flex;
    gap: 0.5rem;
}

.input-row input {
    flex: 1;
    padding: 0.4rem 0.6rem;
}

.input-row button {
    padding: 0.4rem 1rem;
    cursor: pointer;
}

.input-row button:disabled {
    cursor: default;
    opacity: 0.6;
}

.error {
    color: red;
    margin: 0;
}

.result {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.finish-reason,
.tools-label,
.response-label,
.no-tools {
    margin: 0;
    font-weight: bold;
}

.no-tools {
    font-weight: normal;
    color: #666;
}

ul {
    margin: 0.25rem 0 0;
    padding-left: 1.5rem;
}

.response-content {
    margin: 0.25rem 0 0;
    white-space: pre-wrap;
}
</style>
