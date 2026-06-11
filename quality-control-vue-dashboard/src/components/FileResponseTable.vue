<template>
  <section class="file-table-wrapper">
    <table class="file-table">
      <thead>
        <tr>
          <th>ID</th>
          <th>Week</th>
          <th>Year</th>
          <th>Name</th>
          <th>Start Imported At</th>
          <th>End Imported At</th>
        </tr>
      </thead>

      <tbody v-if="rows.length > 0">
        <tr v-for="row in rows" :key="row.id">
          <td>{{ row.id }}</td>
          <td>{{ row.week }}</td>
          <td>{{ row.year }}</td>
          <td>{{ row.name }}</td>
          <td>{{ formatDate(row.startImportedAt) }}</td>
          <td>{{ formatDate(row.endImportedAt) }}</td>
        </tr>
      </tbody>

      <tbody v-else>
        <tr>
          <td colspan="6" class="empty-row">{{ emptyMessage }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FileResponseDto } from '../models/file-response-dto';

type Props = {
  items?: FileResponseDto[];
  emptyMessage?: string;
};

const props = withDefaults(defineProps<Props>(), {
  items: () => [],
  emptyMessage: 'No files found.',
});

const rows = computed(() => props.items ?? []);

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
</style>