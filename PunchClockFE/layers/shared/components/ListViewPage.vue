<template>
  <div class="space-y-6">
    <header class="mb-6 border-b border-neutral-light dark:border-neutral-dark pb-3 flex flex-wrap items-center justify-between gap-4">
      <div>
        <h1 class="font-branding text-3xl">{{ title }}</h1>
        <p v-if="subtitle" class="text-sm opacity-70">{{ subtitle }}</p>
      </div>
      <div class="flex items-center gap-3">
        <AppButton @click="handleCreate" variant="primary" size="small">
          Create
        </AppButton>
        <Menu as="div" class="relative inline-block text-left">
          <div>
            <MenuButton
              :disabled="viewMode === 'card'"
              :class="[
                'inline-flex w-full justify-center gap-x-1.5 rounded-md px-3 py-2 text-sm font-semibold shadow-sm ring-1 ring-inset transition-colors',
                viewMode === 'card'
                  ? 'bg-neutral-light/50 dark:bg-neutral-dark/50 text-neutral/80 dark:text-neutral/80 ring-neutral-light dark:ring-neutral-dark cursor-not-allowed'
                  : 'bg-background-light dark:bg-background-dark text-neutral-dark dark:text-neutral-light ring-neutral-light dark:ring-neutral-dark hover:bg-neutral-light/30 dark:hover:bg-neutral-dark/30'
              ]"
            >
              Columns
              <ChevronDownIcon class="-mr-1 h-5 w-5 text-neutral" aria-hidden="true" />
            </MenuButton>
          </div>
          <transition enter-active-class="transition ease-out duration-100" enter-from-class="transform opacity-0 scale-95" enter-to-class="transform opacity-100 scale-100" leave-active-class="transition ease-in duration-75" leave-from-class="transform opacity-100 scale-100" leave-to-class="transform opacity-0 scale-95">
            <MenuItems v-if="viewMode === 'table'" class="absolute right-0 z-10 mt-2 w-56 origin-top-right rounded-md bg-background-light dark:bg-background-dark shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none dark:ring-neutral-dark">
              <div class="py-1">
                <div v-for="col in columns" :key="col.key" class="hover:bg-neutral-light/30 dark:hover:bg-neutral-dark/30">
                  <label class="flex items-center gap-3 px-4 py-2 text-sm text-neutral-dark dark:text-neutral-light cursor-pointer">
                    <input type="checkbox" :value="col.key" v-model="visibleColumnKeys" class="h-4 w-4 rounded border-neutral-light text-primary focus:ring-primary dark:bg-neutral-dark dark:border-neutral-dark">
                    {{ col.label }}
                  </label>
                </div>
              </div>
            </MenuItems>
          </transition>
        </Menu>

        <div class="flex items-center gap-3">
          <div class="hidden sm:flex items-center gap-1 rounded-md bg-neutral-light/50 dark:bg-neutral-dark/50 p-1">
            <button @click="viewMode = 'table'" :class="['rounded-md p-1.5 transition-colors', viewMode === 'table' ? 'bg-background-light dark:bg-neutral-dark shadow-sm' : 'text-neutral-dark/80 hover:text-neutral-dark dark:text-neutral-light/80 dark:hover:text-white']" aria-label="Table View">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M3 14h18M3 6h18M3 18h18" /></svg>
            </button>
            <button @click="viewMode = 'card'" :class="['rounded-md p-1.5 transition-colors', viewMode === 'card' ? 'bg-background-light dark:bg-neutral-dark shadow-sm' : 'text-neutral-dark/80 hover:text-neutral-dark dark:text-neutral-light/80 dark:hover:text-white']" aria-label="Card View">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" /></svg>
            </button>
          </div>

          <Menu as="div" class="relative inline-block text-left text-neutral-dark dark:text-neutral-light">
            <div>
              <MenuButton
                :disabled="isExporting"
                :class="[
                  'inline-flex w-full items-center justify-center gap-x-1.5 rounded-full px-3 py-2 text-sm font-semibold shadow-sm ring-1 ring-inset transition-colors',
                  isExporting
                    ? 'bg-neutral-light/60 dark:bg-neutral-dark/50 text-neutral/70 dark:text-neutral/50 ring-neutral-light dark:ring-neutral-dark cursor-wait'
                    : 'bg-background-light dark:bg-neutral-dark text-neutral-dark dark:text-neutral-light ring-neutral-light/70 dark:ring-neutral-dark/80 hover:bg-neutral-light/40 dark:hover:bg-neutral-dark/40'
                ]"
              >
                {{ isExporting ? 'Exporting...' : 'Export' }}
                <ChevronDownIcon class="-mr-1 h-5 w-5 text-neutral-dark/60 dark:text-neutral/50" aria-hidden="true" />
              </MenuButton>
            </div>
            <transition
              enter-active-class="transition ease-out duration-100"
              enter-from-class="transform opacity-0 scale-95"
              enter-to-class="transform opacity-100 scale-100"
              leave-active-class="transition ease-in duration-75"
              leave-from-class="transform opacity-100 scale-100"
              leave-to-class="transform opacity-0 scale-95"
            >
              <MenuItems
                class="absolute right-0 z-10 mt-2 w-44 origin-top-right rounded-xl border border-neutral-light/70 bg-background-light py-1.5 shadow-lg shadow-black/5 focus:outline-none dark:border-neutral-dark/70 dark:bg-background-dark dark:shadow-black/30"
              >
                <div class="py-1">
                  <MenuItem v-slot="{ active }">
                    <button
                      type="button"
                      @click="triggerExport('csv')"
                      :class="[
                        active ? 'bg-neutral-light/40 dark:bg-neutral-dark/40 text-neutral-dark dark:text-neutral-light' : 'text-neutral-dark/80 dark:text-neutral/70',
                        'flex w-full items-center px-4 py-2 text-sm'
                      ]"
                    >
                      Export to CSV
                    </button>
                  </MenuItem>
                  <MenuItem v-slot="{ active }">
                    <button
                      type="button"
                      @click="triggerExport('pdf')"
                      :class="[
                        active ? 'bg-neutral-light/40 dark:bg-neutral-dark/40 text-neutral-dark dark:text-neutral-light' : 'text-neutral-dark/80 dark:text-neutral/70',
                        'flex w-full items-center px-4 py-2 text-sm'
                      ]"
                    >
                      Export to PDF
                    </button>
                  </MenuItem>
                </div>
              </MenuItems>
            </transition>
          </Menu>
        </div>
      </div>
    </header>

    <form class="flex flex-wrap items-end gap-3" @submit.prevent>
      <div class="flex-grow sm:flex-grow-0 sm:w-64">
        <label for="q" class="block text-xs opacity-80 mb-1">Search</label>
        <input id="q" v-model="q" type="search" :placeholder="`Find by ${searchColumnLabel}...`" class="block w-full rounded-md border-0 py-1.5 px-3 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-primary dark:bg-white/5 dark:text-white dark:ring-white/10 dark:placeholder:text-gray-500" />
      </div>
      <div>
        <label for="search-column" class="block text-xs opacity-80 mb-1">In Column</label>
        <select id="search-column" v-model="selectedSearchColumn" class="block w-full rounded-md border-0 py-1.5 pl-3 pr-8 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 focus:ring-2 focus:ring-inset focus:ring-primary dark:bg-white/5 dark:text-white dark:ring-white/10">
          <option v-for="col in searchableColumns" :key="col.key" :value="col.key">{{ col.label }}</option>
        </select>
      </div>
    </form>

    <FormsFormTable
      v-if="viewMode === 'table'"
      ref="formTableRef"
      :endpoint="endpoint"
      :columns="columns"
      :search-query="debouncedQ"
      :search-column="selectedSearchColumn"
      :selected-fields="visibleColumnKeys"
    />
    <FormsCardGrid
      v-else-if="viewMode === 'card'"
      ref="formTableRef"
      :endpoint="endpoint"
      :columns="columns"
      :search-query="debouncedQ"
      :search-column="selectedSearchColumn"
    />
  </div>
</template>

<script setup lang="ts">
import { refDebounced } from '@vueuse/core'
import { Menu, MenuButton, MenuItems, MenuItem } from '@headlessui/vue'
import { ChevronDownIcon } from '@heroicons/vue/20/solid'
import type { Col } from '../types/Column'

interface Props {
  title: string
  subtitle?: string
  endpoint: string
  searchColumn: string
  columns: Array<Col>
  createRoute?: string
}

const props = defineProps<Props>()

const q = ref('')
const debouncedQ = refDebounced(q, 300)
const selectedSearchColumn = ref(props.searchColumn)
const visibleColumnKeys = ref<string[]>([])
const isExporting = ref(false)
const viewMode = ref<'table' | 'card'>('table')

const formTableRef = ref<any | null>(null)

const handleCreate = () => {
  if (props.createRoute) {
    navigateTo(props.createRoute)
  }
}

const triggerExport = async (format: 'csv' | 'pdf') => {
  if (!formTableRef.value) return
  try {
    isExporting.value = true
    await formTableRef.value.exportData(format, visibleColumnKeys.value)
  } catch (error) {
    console.error(`Failed to export ${format}:`, error)
  } finally {
    isExporting.value = false
  }
}

const searchableColumns = computed(() => {
  const nonSearchableTypes = new Set(['date', 'paragraph'])
  return props.columns
    .filter(c => !nonSearchableTypes.has(c.type || 'text') && c.key !== 'actions')
    .map(c => ({
      key: c.displayPath || c.key,
      label: c.label,
    }))
})

const searchColumnLabel = computed(() => {
  const col = searchableColumns.value.find(c => c.key === selectedSearchColumn.value)
  return col ? col.label : selectedSearchColumn.value
})

onMounted(() => {
  if (!searchableColumns.value.some(c => c.key === selectedSearchColumn.value)) {
    selectedSearchColumn.value = searchableColumns.value[0]?.key || ''
  }
  visibleColumnKeys.value = props.columns.map(c => c.key)
})
</script>
