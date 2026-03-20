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
const textareaRef = ref<HTMLTextAreaElement | null>(null)

async function submit() {
    const content = input.value.trim()
    if (!content || loading.value) return
    input.value = ''
    resetHeight()
    await send(props.providerName, props.modelName, content)
}

function onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault()
        submit()
    }
}

function onInput() {
    const el = textareaRef.value
    if (!el) return
    el.style.height = 'auto'
    el.style.height = Math.min(el.scrollHeight, 72) + 'px'
}

function resetHeight() {
    const el = textareaRef.value
    if (el) el.style.height = 'auto'
}

function roleLabel(message: MessageItem): string {
    return message.role === 'User' ? 'you' : 'assistant'
}
</script>

<template>
    <div class="chat">
        <div class="history" role="log">
            <p v-if="messages.length === 0 && !loading" class="empty">No messages yet.</p>
            <div
                v-for="(message, index) in messages"
                :key="index"
                class="message"
            >
                <span class="role">{{ roleLabel(message) }}</span>
                <span class="content">{{ message.content }}</span>
            </div>
            <div v-if="loading" class="message">
                <span class="role">assistant</span>
                <span class="content">
                    <span class="dots">
                        <span class="dot">.</span><span class="dot">.</span><span class="dot">.</span>
                    </span>
                </span>
            </div>
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <form class="input-bar" @submit.prevent="submit">
            <textarea
                ref="textareaRef"
                v-model="input"
                rows="1"
                placeholder="message..."
                :disabled="loading"
                aria-label="Message input"
                @keydown="onKeydown"
                @input="onInput"
            />
            <button type="submit" :disabled="loading || !input.trim()">send</button>
        </form>
    </div>
</template>

<style scoped>
.chat {
    display: flex;
    flex-direction: column;
    gap: 0;
}

.history {
    display: flex;
    flex-direction: column;
    min-height: 80px;
    max-height: 480px;
    overflow-y: auto;
    margin-bottom: 12px;
}

.message {
    display: flex;
    gap: 16px;
    padding: 8px 0;
    border-bottom: 1px solid var(--border);
}

.message:last-child {
    border-bottom: none;
}

.role {
    width: 72px;
    flex-shrink: 0;
    color: var(--text-dim);
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    padding-top: 2px;
}

.content {
    color: var(--text);
    white-space: pre-wrap;
    flex: 1;
}

.empty {
    color: var(--text-dim);
    padding: 8px 0;
}

.dots {
    display: inline-flex;
    gap: 2px;
}

.dot {
    animation: pulse 1.2s ease-in-out infinite;
    color: var(--text-dim);
}

.dot:nth-child(2) { animation-delay: 0.2s; }
.dot:nth-child(3) { animation-delay: 0.4s; }

@keyframes pulse {
    0%, 60%, 100% { opacity: 0.2; }
    30% { opacity: 1; }
}

.input-bar {
    display: flex;
    gap: 8px;
    padding-top: 12px;
    border-top: 1px solid var(--border);
}

textarea {
    flex: 1;
    background: var(--surface-raised);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    color: var(--text);
    font-family: var(--font-ui);
    font-size: 13px;
    padding: 8px 12px;
    resize: none;
    line-height: 1.5;
    transition: border-color var(--transition);
}

textarea::placeholder {
    color: var(--text-placeholder);
}

textarea:focus {
    outline: none;
    border-color: var(--border-active);
}

textarea:disabled {
    opacity: 0.5;
}

button {
    background: var(--accent);
    color: #fff;
    border: none;
    border-radius: var(--radius);
    padding: 8px 16px;
    font-family: var(--font-ui);
    font-size: 12px;
    letter-spacing: 0.04em;
    cursor: pointer;
    transition: background var(--transition), transform var(--transition);
    align-self: flex-end;
}

button:hover:not(:disabled) {
    background: var(--accent-hover);
}

button:active:not(:disabled) {
    transform: scale(0.97);
}

button:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

.error {
    color: var(--err);
    font-size: 12px;
    margin-bottom: 8px;
}
</style>
