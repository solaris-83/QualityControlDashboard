<template>
  <section class="page">
    <header class="page-header">
      <h2>Data Set Viewer</h2>
      <button type="button" class="secondary" @click="$emit('go-home')">Back to Home</button>
    </header>

    <div class="filters">
      <label>
        Week
        <input v-model.number="week" type="number" min="1" max="53" />
      </label>

      <label>
        Year
        <input v-model.number="year" type="number" min="2000" max="2100" />
      </label>

      <label>
        Page Size
        <input v-model.number="pageSize" type="number" min="1" max="1000" />
      </label>

      <label>
        Page Number
        <input v-model.number="pageNumber" type="number" min="1" />
      </label>

      <button type="button" :disabled="loading" @click="loadData">Load Data Set</button>
    </div>

    <p v-if="loading">Loading data set stream...</p>
    <p v-if="dataSetInfo">{{ dataSetInfo }}</p>
    <p v-if="dataSetError" class="error">{{ dataSetError }}</p>

    <DataSetTable :items="dataSetRows" />
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import DataSetTable from '../components/DataSetTable.vue';
import { getISOWeek } from 'date-fns/getISOWeek';
import { useDataSet } from '../composables/useDataSet.ts';

defineEmits<{
  (e: 'go-home'): void;
}>();

const { loading, dataSetInfo, dataSetRows, dataSetError, loadDataSetByWeekAndYear } = useDataSet();

const now = new Date();
const week = ref(getISOWeek(now));
const year = ref(now.getFullYear());
const pageSize = ref(100);
const pageNumber = ref(1);

onMounted(() => {
  loadData();
});

async function loadData() {
  await loadDataSetByWeekAndYear(
    week.value,
    year.value,
    pageSize.value,
    pageNumber.value,
  );
}
</script>

<style scoped>
.page {
  padding: 1.2rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.filters {
  margin: 1rem 0;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.8rem;
  align-items: end;
}

label {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

input {
  padding: 0.5rem;
}

button {
  padding: 0.6rem 0.95rem;
  border-radius: 8px;
  border: 1px solid #1f6feb;
  background-color: #1f6feb;
  color: #fff;
  cursor: pointer;
  font-size: 17px;
}

button.secondary {
  border-color: #a2a2a2;
  background-color: #fff;
  font-size: 17px;
  color: #333;
}

button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.error {
  color: #b42318;
}
</style>