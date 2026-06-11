<template>
  <div>
    <h1>Imported files</h1>

    <div v-if="loading">Loading...</div>

    <div v-else>
       <table-lite
        :is-loading="loadingTable"
        :columns="table.columns"
        :rows="table.rows"
        :total="table.totalRecordCount"
        :sortable="table.sortable"
        @do-search="loadFiles"
        @is-finished="table.isLoading = false"
    ></table-lite>

      <div class="buttons">
        <button
          @click="loadDataSetByWeekAndYear(16, 2026, 300, 1)"
          :disabled="loading">
          Load dataset
        </button>

        <button @click="loadImportedFiles()" :disabled="loading">
          Load files
        </button>

        <button @click="uploadDefaultFile()" :disabled="loading">
          Upload dataset
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useFilesTable } from "../composables/useFilesTable";
import { useUserForm } from "../composables/useUserForm";
import { FileRequestDto } from "../models/file-request-dto";

const {
  user,
  loading,
  saving,
  errors,
  loadDataSetByWeekAndYear,
  loadImportedFiles,
  uploadFile,
  clear,
} = useUserForm();

const {
    loadingTable,
    table,
    loadFiles,
} = useFilesTable();

function uploadDefaultFile() {
  const request = new FileRequestDto();
  request.week = 16;
  request.year = 2026;
  request.projects = ["BUS_ADAS", "TRUCK_L24", "TRUCK_MH24"];

  uploadFile(request);
}
</script>

<style scoped>
.page {
  max-width: 600px;
  margin: auto;
  padding: 20px;
}

.form-row {
  display: flex;
  flex-direction: column;
  margin-bottom: 12px;
}

.form-row label {
  font-weight: bold;
  margin-bottom: 4px;
}

.form-row input {
  padding: 8px;
}

.validation {
  margin-top: 16px;
  color: red;
}

.buttons {
  margin-top: 20px;
  display: flex;
  gap: 10px;
}

button {
  padding: 10px 16px;
}
</style>
