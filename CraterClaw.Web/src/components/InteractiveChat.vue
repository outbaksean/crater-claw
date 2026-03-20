<script setup lang="ts">
import { ref } from 'vue'
import { useExecution } from '../composables/useExecution'
import type { MessageItem } from '../api/types'

const props = defineProps<{
    providerName: string
    modelName: string
}>()

const { messages, loading, error, send } = useExecution()

const input = ref('')

async function submit() {
    const content = input.value.trim()
    if (!content || loading.value) return
    input.value = ''
    await send(props.providerName, props.modelName, content)
}

function roleLabel(message: MessageItem): string {
    return message.role === 'User' ? 'You' : 'Assistant'
}
</script>

<template>
    <div class="chat">
        <div class="history" role="log">
            <div
                v-for="(message, index) in messages"
                :key="index"
                :class="['message', message.role.toLowerCase()]"
            >
                <span class="role">{{ roleLabel(message) }}:</span>
                <span class="content">{{ message.content }}</span>
            </div>
            <p v-if="messages.length === 0" class="empty">No messages yet.</p>
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <form class="input-row" @submit.prevent="submit">
            <input
                v-model="input"
                type="text"
                placeholder="Enter a message..."
                :disabled="loading"
                aria-label="Message input"
            />
            <button type="submit" :disabled="loading || !input.trim()">
                {{ loading ? 'Sending...' : 'Send' }}
            </button>
        </form>
    </div>
</template>

<style scoped>
.chat {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.history {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    min-height: 4rem;
}

.message {
    display: flex;
    gap: 0.5rem;
}

.role {
    font-weight: bold;
    min-width: 5rem;
    flex-shrink: 0;
}

.empty {
    color: #888;
    font-style: italic;
    margin: 0;
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
</style>
