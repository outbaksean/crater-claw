<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useProviders } from './composables/useProviders'
import { useModels } from './composables/useModels'
import type { ProviderEndpoint, ModelItem } from './api/types'

const {
    providers,
    selectedProvider,
    status,
    loadingProviders,
    loadingStatus,
    error: providerError,
    fetchProviders,
    selectProvider,
} = useProviders()

const {
    models,
    selectedModel,
    loading: loadingModels,
    error: modelError,
    fetchModels,
    selectModel,
} = useModels()

onMounted(fetchProviders)

watch(
    () => status.value,
    (s) => {
        if (s?.isReachable && selectedProvider.value) {
            fetchModels(selectedProvider.value.name)
        }
    },
)

function onSelectProvider(provider: ProviderEndpoint) {
    selectProvider(provider)
}

function onSelectModel(model: ModelItem) {
    selectModel(model)
}
</script>

<template>
    <div class="app">
        <h1>CraterClaw</h1>

        <section>
            <h2>Providers</h2>
            <p v-if="loadingProviders">Loading...</p>
            <p v-else-if="providerError" class="error">{{ providerError }}</p>
            <ol v-else>
                <li
                    v-for="provider in providers"
                    :key="provider.name"
                    :class="{ selected: selectedProvider?.name === provider.name }"
                    @click="onSelectProvider(provider)"
                >
                    {{ provider.name }} <span class="url">({{ provider.baseUrl }})</span>
                </li>
            </ol>
        </section>

        <section v-if="selectedProvider">
            <h2>Status: {{ selectedProvider.name }}</h2>
            <p v-if="loadingStatus">Checking...</p>
            <p v-else-if="status">
                <span :class="status.isReachable ? 'reachable' : 'unreachable'">
                    {{ status.isReachable ? 'Reachable' : 'Unreachable' }}
                </span>
                <span v-if="status.errorMessage" class="error"> - {{ status.errorMessage }}</span>
            </p>
        </section>

        <section v-if="status?.isReachable">
            <h2>Models</h2>
            <p v-if="loadingModels">Loading...</p>
            <p v-else-if="modelError" class="error">{{ modelError }}</p>
            <ol v-else>
                <li
                    v-for="model in models"
                    :key="model.name"
                    :class="{ selected: selectedModel?.name === model.name }"
                    @click="onSelectModel(model)"
                >
                    {{ model.name }}
                </li>
            </ol>
            <p v-if="selectedModel">
                Selected: <strong>{{ selectedModel.name }}</strong>
            </p>
        </section>
    </div>
</template>

<style scoped>
.app {
    font-family: sans-serif;
    max-width: 800px;
    margin: 2rem auto;
    padding: 0 1rem;
}

section {
    margin-bottom: 2rem;
}

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

.url {
    color: #666;
    font-size: 0.9em;
}

.reachable {
    color: green;
}

.unreachable {
    color: red;
}

.error {
    color: red;
}
</style>
