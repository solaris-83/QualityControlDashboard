import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
   build: {
    sourcemap: true
  },
  plugins: [vue({
        template: {
          compilerOptions: {
            isCustomElement: (tag) => ['table-lite'].includes(tag),
          }
        }
      })]
})
