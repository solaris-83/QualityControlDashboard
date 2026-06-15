<template>
  <div ref="root" class="dropdown-checkbox">
    <button
      type="button"
      class="trigger"
      :disabled="loading"
      @click="toggleOpen"
    >
      <span>{{ selectedLabel }}</span>
      <span class="caret" :class="{ open: isOpen }">▾</span>
    </button>

    <div v-if="isOpen" class="panel">
      <div class="panel-header">
        <label class="checkbox-row">
          <input
            type="checkbox"
            :checked="isAllSelected"
            :disabled="normalizedOptions.length === 0"
            @change="toggleAll"
          />
          <span>Select all</span>
        </label>
      </div>

      <div v-if="loading" class="status">{{ loadingText }}</div>
      <div v-else-if="normalizedOptions.length === 0" class="status">{{ emptyText }}</div>

      <ul v-else class="options-list">
        <li v-for="option in normalizedOptions" :key="option.id">
          <label class="checkbox-row">
            <input
              type="checkbox"
              :value="option.id"
              v-model="selectedValues"
            />
            <span>{{ option.label }}</span>
          </label>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';

type Option = {
  id: string;
  label: string;
};

type Props = {
  modelValue?: string[];
  options?: Option[];
  placeholder?: string;
  loadingText?: string;
  emptyText?: string;
  loadOptions?: () => Promise<Option[]>;
};

const props = withDefaults(defineProps<Props>(), {
  modelValue: () => [],
  options: () => [],
  placeholder: 'Select projects',
  loadingText: 'Loading projects...',
  emptyText: 'No projects available.',
  loadOptions: undefined,
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: string[]): void;
}>();

const root = ref<HTMLElement | null>(null);
const isOpen = ref(false);
const loading = ref(false);
const loadedOptions = ref<Option[]>([]);
const selectedValues = ref<string[]>([...props.modelValue]);

const normalizedOptions = computed<Option[]>(() => {
  return loadedOptions.value.length > 0 ? loadedOptions.value : props.options;
});

const isAllSelected = computed(() => {
  return (
    normalizedOptions.value.length > 0
    && selectedValues.value.length === normalizedOptions.value.length
  );
});

const selectedLabel = computed(() => {
  const selectedCount = selectedValues.value.length;
  if (selectedCount === 0) {
    return props.placeholder;
  }

  if (selectedCount === 1) {
    const selectedOption = normalizedOptions.value.find((item) => item.id === selectedValues.value[0]);
    return selectedOption?.label ?? props.placeholder;
  }

  return `${selectedCount} projects selected`;
});

watch(
  () => props.modelValue,
  (newValue) => {
    selectedValues.value = [...(newValue ?? [])];
  },
);

watch(
  selectedValues,
  (newValue) => {
    emit('update:modelValue', [...newValue]);
  },
  { deep: true },
);

watch(
  () => props.options,
  () => {
    // Keep only values that still exist when options are refreshed.
    const validIds = new Set(normalizedOptions.value.map((item) => item.id));
    selectedValues.value = selectedValues.value.filter((id) => validIds.has(id));
  },
  { deep: true },
);

onMounted(async () => {
  if (props.loadOptions) {
    loading.value = true;
    try {
      loadedOptions.value = await props.loadOptions();
    } finally {
      loading.value = false;
    }
  }

  document.addEventListener('click', onDocumentClick);
});

onUnmounted(() => {
  document.removeEventListener('click', onDocumentClick);
});

function toggleOpen() {
  isOpen.value = !isOpen.value;
}

function toggleAll(event: Event) {
  const target = event.target as HTMLInputElement;
  selectedValues.value = target.checked ? normalizedOptions.value.map((item) => item.id) : [];
}

function onDocumentClick(event: MouseEvent) {
  const target = event.target as Node | null;
  if (!target || !root.value) {
    return;
  }

  if (!root.value.contains(target)) {
    isOpen.value = false;
  }
}
</script>

<style scoped>
.dropdown-checkbox {
  position: relative;
  width: 100%;
}

.trigger {
  width: 100%;
  border: 1px solid #c9c9c9;
  border-radius: 8px;
  background: #fff;
  color: #1f2328;
  padding: 0.55rem 0.7rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  text-align: left;
}

.caret {
  transition: transform 0.2s ease;
}

.caret.open {
  transform: rotate(180deg);
}

.panel {
  position: absolute;
  z-index: 20;
  width: 100%;
  margin-top: 0.35rem;
  border: 1px solid #d5d5d5;
  border-radius: 8px;
  background: #fff;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.08);
  max-height: 260px;
  overflow: auto;
}

.panel-header {
  padding: 0.55rem 0.7rem;
  border-bottom: 1px solid #ededed;
}

.options-list {
  list-style: none;
  margin: 0;
  padding: 0.45rem 0;
}

.options-list li {
  padding: 0 0.7rem;
}

.checkbox-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-height: 2rem;
}

.status {
  padding: 0.7rem;
  color: #5f6368;
}
</style>