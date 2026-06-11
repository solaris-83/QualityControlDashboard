<script setup lang="ts">
import { ref } from 'vue';
import HomePage from './pages/HomePage.vue';
import ImportedFilesPage from './pages/ImportedFilesPage.vue';
import DataSetPage from './pages/DataSetPage.vue';

type PageKey = 'home' | 'files' | 'dataset';

const activePage = ref<PageKey>('home');

function openHome() {
  activePage.value = 'home';
}

function openFiles() {
  activePage.value = 'files';
}

function openDataSet() {
  activePage.value = 'dataset';
}
</script>

<template>
  <main>
    <HomePage
      v-if="activePage === 'home'"
      @open-files="openFiles"
      @open-dataset="openDataSet"
    />

    <ImportedFilesPage
      v-else-if="activePage === 'files'"
      @go-home="openHome"
    />

    <DataSetPage
      v-else
      @go-home="openHome"
    />
  </main>
</template>
