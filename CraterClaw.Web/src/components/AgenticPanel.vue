<script setup lang="ts">
import { ref } from 'vue'
import { useAgentic } from '../composables/useAgentic'

const props = defineProps<{
  providerName: string
  modelName: string
  profileId: string
}>()

const prompt = ref('')
const textareaRef = ref<HTMLTextAreaElement | null>(null)
const agentic = useAgentic()

async function submit() {
  const content = prompt.value.trim()
  if (!content || agentic.loading.value) return
  await agentic.run(props.providerName, {
    modelName: props.modelName,
    prompt: content,
    profileId: props.profileId,
  })
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
</script>

<template>
  <div class="agentic">
    <form @submit.prevent="submit">
      <div class="input-bar">
        <textarea
          ref="textareaRef"
          v-model="prompt"
          rows="1"
          placeholder="task prompt..."
          :disabled="agentic.loading.value"
          aria-label="Task prompt"
          @keydown="onKeydown"
          @input="onInput"
        />
        <button type="submit" :disabled="agentic.loading.value || !prompt.trim()">
          {{ agentic.loading.value ? 'running...' : 'run' }}
        </button>
      </div>
      <label class="thinking-toggle">
        <input type="checkbox" v-model="agentic.showThinking.value" />
        show thinking
      </label>
    </form>
    <p v-if="agentic.error.value" class="error">{{ agentic.error.value }}</p>
    <div v-if="agentic.content.value || agentic.loading.value" class="result">
      <p v-if="agentic.loading.value" class="running-indicator">running...</p>
      <details v-if="agentic.thinking.value" class="thinking-block">
        <summary>thinking</summary>
        <div class="thinking-content">{{ agentic.thinking.value }}</div>
      </details>
      <p v-if="agentic.finishReason.value" class="finish-reason">
        {{ agentic.finishReason.value }}
      </p>
      <p v-if="agentic.toolsInvoked.value.length > 0" class="tools-line">
        <span v-for="tool in agentic.toolsInvoked.value" :key="tool" class="tool-name">{{
          tool
        }}</span>
      </p>
      <div v-if="agentic.content.value" class="response">{{ agentic.content.value }}</div>
    </div>
  </div>
</template>

<style scoped>
.agentic {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.input-bar {
  display: flex;
  gap: 8px;
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
  transition:
    background var(--transition),
    transform var(--transition);
  align-self: flex-end;
  white-space: nowrap;
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

.thinking-toggle {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  color: var(--text-dim);
  cursor: pointer;
  margin-top: 6px;
}

.thinking-toggle input[type='checkbox'] {
  accent-color: var(--accent);
}

.error {
  color: var(--err);
  font-size: 12px;
}

.result {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.running-indicator {
  color: var(--text-dim);
  animation: blink 1.4s ease-in-out infinite;
}

@keyframes blink {
  0%,
  100% {
    opacity: 0.4;
  }
  50% {
    opacity: 1;
  }
}

.thinking-block {
  border: 1px solid var(--border);
  border-radius: var(--radius);
  background: var(--surface-raised);
}

.thinking-block summary {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--text-dim);
  padding: 6px 12px;
  cursor: pointer;
  user-select: none;
}

.thinking-block summary::-webkit-details-marker {
  display: none;
}

.thinking-content {
  padding: 8px 12px 12px;
  font-size: 11px;
  color: var(--text-dim);
  white-space: pre-wrap;
  line-height: 1.6;
  max-height: 240px;
  overflow-y: auto;
}

.finish-reason {
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--text-dim);
}

.tools-line {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.tool-name {
  font-size: 11px;
  color: var(--accent);
}

.response {
  background: var(--surface-raised);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 16px;
  white-space: pre-wrap;
  font-size: 13px;
  line-height: 1.7;
  max-height: 480px;
  overflow-y: auto;
}
</style>
