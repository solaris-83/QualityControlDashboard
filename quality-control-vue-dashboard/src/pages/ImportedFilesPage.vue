<template>
  <section class="page">
    <header class="page-header">
      <h2>Imported CSV Files</h2>
      <button type="button" class="secondary" @click="$emit('go-home')">Back to Home</button>
    </header>

    <div class="upload-panel">
      <h3>Import New CSV Batch</h3>
      <div class="form-grid">
        <label>
          Week
          <input v-model.number="week" type="number" min="1" max="53" />
        </label>

        <label>
          Year
          <input v-model.number="year" type="number" min="2000" max="2100" />
        </label>

        <label class="projects">
          Projects (comma separated)
          <input v-model="projectsText" type="text" />
        </label>
      </div>

      <div class="actions">
        <button type="button" :disabled="loading" @click="loadFiles">Load</button>
        <button type="button" :disabled="uploading" @click="importCsv">Import New CSV</button>
        <button type="button" :disabled="selectedFileIds.length === 0" @click="deleteSelected">Delete</button>
      </div>
    </div>

    <p v-if="uploading">Uploading in progress ...</p>
    <p v-if="loading">Loading imported files ...</p>
    <p v-if="dataSetInfo">{{ dataSetInfo }}</p>
    <p v-if="dataSetError" class="error">{{ dataSetError }}</p>

    <FileResponseTable :items="dataSetRows" @update:selected-rows="handleSelectedRowsUpdate" />
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import FileResponseTable from '../components/FileResponseTable.vue';
import { useImportFiles } from '../composables/useImportFiles.ts';
import { FileRequestDto } from '../models/file-request-dto';
import { getISOWeek } from 'date-fns';

defineEmits<{
  (e: 'go-home'): void;
}>();

const { uploadFile, loading, uploading, dataSetInfo, dataSetRows, dataSetError, loadImportedFiles, deleteImportedFiles } = useImportFiles();

const now = new Date();
const week = ref(getISOWeek(now));
const year = ref(now.getFullYear());
const projectsText = ref('BUS_ADAS,TRUCK_L24,TRUCK_MH24');
//const isDeleteEnabled = ref(false);
const selectedFileIds = ref<number[]>([]);

const handleSelectedRowsUpdate = (selectedIds: number[]) => {
  //isDeleteEnabled.value = selectedIds.length > 0;
  selectedFileIds.value = selectedIds;
  console.log('Selected file IDs from table:', selectedIds);
};

onMounted(async() => {
  await loadImportedFiles(week.value, year.value, projectsText.value.split(',').map((project) => project.trim()).filter(Boolean));
});

async function loadFiles() {
  await loadImportedFiles(week.value, year.value, projectsText.value.split(',').map((project) => project.trim()).filter(Boolean));
}

async function importCsv() {
  const request = new FileRequestDto();
  request.week = week.value;
  request.year = year.value;
  request.projects = projectsText.value
    .split(',')
    .map((project) => project.trim())
    .filter(Boolean);

  await uploadFile(request);
  await loadImportedFiles(week.value, year.value, projectsText.value.split(',').map((project) => project.trim()).filter(Boolean));
}

async function deleteSelected() {
  await deleteImportedFiles(selectedFileIds.value);
  loadFiles();
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

.upload-panel {
  border: 1px solid #d9d9d9;
  border-radius: 10px;
  padding: 1rem;
  margin: 1rem 0;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 0.8rem;
  margin: 0.75rem 0;
}

label {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

input {
  padding: 0.5rem;
}

.projects {
  grid-column: 1 / -1;
}

.actions {
  display: flex;
  gap: 0.7rem;
  flex-wrap: wrap;
}

button {
  padding: 0.6rem 0.95rem;
  border-radius: 8px;
  font-size: 17px;
  border: 1px solid #1f6feb;
  background-color: #1f6feb;
  color: #fff;
  cursor: pointer;
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
