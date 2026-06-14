<template>
  <section class="file-table-wrapper">
    <table class="file-table">
      <thead>
        <tr>
          <th class="col-checkbox">
            <!-- Checkbox per selezionare/deselezionare tutto -->
            <input type="checkbox" v-model="isAllSelected"  :disabled="rows.length == 0"
            />
          </th>
          <th>Week</th>
          <th>Year</th>
          <th>Name</th>
          <th>Start Imported At</th>
          <th>End Imported At</th>
          <th>N. records</th>
          <th>Error Message</th>
        </tr>
      </thead>

      <tbody v-if="rows.length > 0">
        <tr v-for="row in rows" :key="row.id" :class="{ 'row-selected': selectedRows.includes(row.id) }">
          <td>
            <!-- Il v-model lega il checkbox all'array selectedRows usando il valore row.id -->
            <input type="checkbox" :value="row.id" v-model="selectedRows" 
            />
          </td>
          <td>{{ row.week }}</td>
          <td>{{ row.year }}</td>
          <td>{{ row.name }}</td>
          <td>{{ formatDate(row.startImportedAt) }}</td>
          <td>{{ formatDate(row.endImportedAt) }}</td>
          <td>{{ row.numberOfRecords }}</td>
          <td>{{ row.errorMessage}}</td>
        </tr>
      </tbody>

      <tbody v-else>
        <tr>
          <td colspan="8" class="empty-row">{{ emptyMessage }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { FileResponseDto } from '../models/file-response-dto';

type Props = {
  items?: FileResponseDto[];
  emptyMessage?: string;
};
      
const selectedRows = ref<number[]>([]); // Array per tenere traccia delle righe selezionate
const props = withDefaults(defineProps<Props>(), {
  items: () => [],
  emptyMessage: 'No files found.',
});

const emit = defineEmits<{
  (e: 'update:selectedRows', value: number[]): void;
}>();

// Watcher su selectedRows per loggare le selezioni
watch(selectedRows, (newVal : number[], oldVal: number[]) => {
  console.log('Selected file IDs:', oldVal, '->', newVal);
  emit('update:selectedRows', newVal); // Emissione dell'evento per notificare al parent
});

watch(() => props.items, (newItems) => {
  // Se i file cambiano, resetta le selezioni
  selectedRows.value = [];
});

const rows = computed(() => props.items ?? []);
// Computed property per gestire il checkbox "Seleziona Tutti"
const isAllSelected = computed({
  // Restituisce true se tutte le righe correnti sono selezionate (e la tabella non è vuota)
  get() {
    return rows.value.length > 0 && selectedRows.value.length === rows.value.length
  },
  // Quando l'utente clicca su "Seleziona Tutti", popola o svuota l'intero array
  set(value) {
    if (value) {
      selectedRows.value = rows.value.map(row => row.id)
    } else {
      selectedRows.value = []
    }
  }
})

function formatDate(value: Date | string | null | undefined): string {
  if (!value) {
    return '-';
  }

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '-';
  }

  return date.toLocaleString();
}
</script>

<style scoped>
.file-table-wrapper {
  padding: 1rem;
  overflow-x: auto;
}

.file-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.95rem;
  text-align: left;
}

.file-table th,
.file-table td {
  border: 1px solid #d9d9d9;
  padding: 0.6rem 0.75rem;
  white-space: nowrap;
}

.file-table th {
  background-color: #f5f7fb;
  font-weight: 600;
}

.empty-row {
  text-align: center;
  color: #7a7a7a;
}

.col-checkbox {
  width: 40px;
}

.row-selected {
  background-color: #eff6ff;
}

</style>