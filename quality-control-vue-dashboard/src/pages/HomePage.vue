<template>
  <section class="home-page">
    <h1>Quality Control Dashboard</h1>
    <p>Select a page to continue.</p>

    <div class="actions">
      <button type="button" @click="$emit('open-files')">Imported CSV Files</button>
      <button type="button" @click="$emit('open-dataset')">Data Set Viewer</button>
    </div>
  </section>
</template>

<script setup lang="ts">
defineEmits<{
  (e: 'open-files'): void;
  (e: 'open-dataset'): void;
}>();
</script>

<style scoped>
.home-page {
  max-width: 720px;
  margin: 4rem auto;
  padding: 2rem;
  border: 1px solid #d9d9d9;
  border-radius: 12px;
  text-align: center;
}

.actions {
  margin-top: 1.5rem;
  display: flex;
  justify-content: center;
  gap: 1rem;
  flex-wrap: wrap;
}

button {
  padding: 0.7rem 1rem;
  border: 1px solid #1f6feb;
  background-color: #1f6feb;
  color: #fff;
  border-radius: 8px;
  cursor: pointer;
}

button:hover {
  background-color: #1357bd;
}
</style>