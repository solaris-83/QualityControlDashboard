<template>
  <section class="dataset-table-wrapper">
    <table class="dataset-table">
      <thead>
        <tr>
          <th>Week</th>
          <th>Year</th>
          <th>License</th>
          <th>VIN</th>
          <th>Model</th>
          <th>App</th>
          <th>Result</th>
          <th>Error</th>
          <th>Elapsed</th>
          <th>Affected Controllers</th>
        </tr>
      </thead>

      <tbody v-if="rows.length > 0">
        <tr v-for="row in rows" :key="row.id">
          <td>{{ row.week }}</td>
          <td>{{ row.year }}</td>
          <td>{{ row.license }}</td>
          <td>{{ row.vin }}</td>
          <td>{{ row.model }}</td>
          <td>{{ row.appName }}</td>
          <td>{{ row.resultType }}</td>
          <td>{{ row.errorCode }}</td>
          <td>{{ row.elapsedTime }}</td>
          <td>{{ row.affectedControllers }}</td>
        </tr>
      </tbody>

      <tbody v-else>
        <tr>
          <td colspan="11" class="empty-row">{{ emptyMessage }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { DataSetResponseDto } from '../models/data-set-response-dto';

type Props = {
  items?: DataSetResponseDto[];
  emptyMessage?: string;
};

const props = withDefaults(defineProps<Props>(), {
  items: () => [],
  emptyMessage: 'No dataset records found.',
});

const rows = computed(() => props.items ?? []);
</script>

<style scoped>
.dataset-table-wrapper {
  padding: 1rem;
  overflow-x: auto;
}

.dataset-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
  text-align: left;
}

.dataset-table th,
.dataset-table td {
  border: 1px solid #d9d9d9;
  padding: 0.55rem 0.7rem;
  white-space: nowrap;
}

.dataset-table th {
  background-color: #f5f7fb;
  font-weight: 600;
}

.empty-row {
  text-align: center;
  color: #7a7a7a;
}
</style>