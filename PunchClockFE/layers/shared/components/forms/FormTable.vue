<template>
  <section class="space-y-4">
    <div class="overflow-x-auto rounded-lg border border-gray-200 dark:border-white/10 bg-white dark:bg-gray-800/20">
      <table class="min-w-full text-left align-middle text-sm">
        <thead class="uppercase text-xs opacity-80 bg-gray-50 dark:bg-white/5">
          <tr>
            <th v-for="col in tableColumns" :key="col.key" scope="col" class="px-4 py-3 font-semibold">
              <button v-if="col.key !== 'actions'" @click="setSort(col.key)" class="flex items-center gap-1 transition-colors" :class="[col.sortable === false ? 'cursor-default' : 'hover:text-gray-700 dark:hover:text-gray-300', sortBy === col.key ? 'text-primary dark:text-primary-light' : '']" :disabled="col.sortable === false">
                {{ col.label }}
                <template v-if="col.sortable !== false">
                  <ChevronUpDownIcon v-if="sortBy !== col.key" class="h-4 w-4 text-gray-400" />
                  <ChevronUpIcon v-else-if="sortOrder === 'asc'" class="h-4 w-4" />
                  <ChevronDownIcon v-else-if="sortOrder === 'desc'" class="h-4 w-4" />
                  <ChevronUpDownIcon v-else class="h-4 w-4 text-gray-400" />
                </template>
              </button>
              <span v-else class="flex justify-end pr-2 font-semibold">{{ col.label }}</span>
            </th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-white/10">
          <tr v-if="loading">
            <td :colspan="tableColumns.length" class="px-4 py-10 text-center opacity-70">
              <div class="flex items-center justify-center gap-2">
                <div class="spinner h-5 w-5"></div>
                <span>Loading…</span>
              </div>
            </td>
          </tr>
          <tr v-else-if="!rows.length">
            <td :colspan="tableColumns.length" class="px-4 py-10 text-center opacity-70">
              <span v-if="searchQuery">No records found matching your search.</span>
              <span v-else>No records yet.</span>
            </td>
          </tr>
          <tr v-else v-for="row in rows" :key="row.id" @click="openViewModal(row)" class="hover:bg-gray-50 dark:hover:bg-white/5 cursor-pointer">
            <td
              v-for="col in tableColumns.filter(c => c.key !== 'actions')"
              :key="col.key"
              class="px-4 py-3 whitespace-pre-wrap"
            >
              {{ formatCell(getNestedValue(row, col.displayPath || col.key), col.type) }}
            </td>
            <td class="px-4 py-3 text-right">
              <Menu as="div" class="relative inline-block text-left">
                <div>
                  <MenuButton @click.stop class="inline-flex w-full justify-center gap-x-1.5 rounded-full p-2 text-sm font-semibold text-neutral-dark/70 hover:bg-neutral-light/40 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary dark:text-neutral/60 dark:hover:bg-neutral-dark/40">
                    <span class="sr-only">Open options</span>
                    <EllipsisVerticalIcon class="h-5 w-5" aria-hidden="true" />
                  </MenuButton>
                </div>
                <transition enter-active-class="transition ease-out duration-100" enter-from-class="transform opacity-0 scale-95" enter-to-class="transform opacity-100 scale-100" leave-active-class="transition ease-in duration-75" leave-from-class="transform opacity-100 scale-100" leave-to-class="transform opacity-0 scale-95">
                  <MenuItems class="absolute right-0 z-10 mt-2 w-32 origin-top-right rounded-md bg-white dark:bg-gray-800 shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none dark:ring-white/10">
                    <div class="py-1">
                      <MenuItem v-slot="{ active }">
                        <button @click.stop="openEditModal(row)" :class="[active ? 'bg-neutral-light/40 dark:bg-neutral-dark/40 text-neutral-dark dark:text-neutral-light' : 'text-neutral-dark/80 dark:text-neutral/70', 'flex items-center w-full text-left px-4 py-2 text-sm']">
                          <PencilIcon class="mr-3 h-5 w-5 text-gray-400" aria-hidden="true" />
                          <span>Edit</span>
                        </button>
                      </MenuItem>
                      <MenuItem v-slot="{ active }">
                        <button @click.stop="openDeleteModal(row)" :class="[active ? 'bg-red-500/10 text-red-700 dark:text-red-400' : 'text-neutral-dark/80 dark:text-neutral/70', 'flex items-center w-full text-left px-4 py-2 text-sm']">
                          <TrashIcon class="mr-3 h-5 w-5 text-gray-400" aria-hidden="true" />
                          <span>Delete</span>
                        </button>
                      </MenuItem>
                    </div>
                  </MenuItems>
                </transition>
              </Menu>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <nav v-if="!loading && totalItems > 0" class="flex flex-wrap items-center justify-between gap-y-4 border-t border-gray-200 dark:border-white/10 pt-4" aria-label="Pagination">
      <div class="flex items-center gap-x-4">
        <p class="text-sm text-gray-700 dark:text-gray-400">
          {{ pageStatusText }}
        </p>
        <div class="flex items-center gap-x-2">
          <label for="items-per-page-table" class="sr-only">Items per page</label>
          <select
            id="items-per-page-table"
            v-model="itemsPerPage"
            class="block w-16 rounded-md border-0 py-1 pl-2 pr-7 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 focus:ring-2 focus:ring-inset focus:ring-primary dark:bg-white/5 dark:text-white dark:ring-white/10"
          >
            <option v-for="option in itemsPerPageOptions" :key="option" :value="option">{{ option }}</option>
          </select>
          <span class="text-sm text-gray-700 dark:text-gray-400">per page</span>
        </div>
      </div>

      <div class="flex items-center gap-x-2">
        <div class="flex items-center text-sm">
           <label for="page-input-table" class="sr-only">Go to page</label>
           <input
              id="page-input-table"
              type="number"
              v-model.lazy="pageInput"
              @change="goToPage(pageInput)"
              class="block w-16 rounded-md border-0 py-1 text-center text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 focus:ring-2 focus:ring-inset focus:ring-primary dark:bg-white/5 dark:text-white dark:ring-white/10"
              min="1"
              :max="totalPages"
           />
           <span class="ml-2 text-gray-700 dark:text-gray-400">of {{ totalPages }}</span>
        </div>

        <div class="isolate inline-flex -space-x-px rounded-md shadow-sm">
          <button @click="prevPage" :disabled="isFirstPage" type="button" class="relative inline-flex items-center rounded-l-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 dark:ring-white/10 hover:bg-gray-50 dark:hover:bg-white/5 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed">
            <span class="sr-only">Previous</span>
            <ChevronLeftIcon class="h-5 w-5" aria-hidden="true" />
          </button>
          <template v-for="(page, index) in paginationNumbers" :key="index">
            <span v-if="page === '...'" class="relative hidden items-center px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-400 ring-1 ring-inset ring-gray-300 dark:ring-white/10 md:inline-flex">...</span>
            <button
              v-else
              @click="goToPage(page as number)"
              type="button"
              :class="[
                page === currentPage
                  ? 'z-10 bg-primary text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary'
                  : 'text-gray-900 dark:text-white ring-1 ring-inset ring-gray-300 dark:ring-white/10 hover:bg-gray-50 dark:hover:bg-white/5',
                'relative hidden items-center px-4 py-2 text-sm font-semibold focus:z-20 focus:outline-offset-0 md:inline-flex'
              ]"
            >
              {{ page }}
            </button>
          </template>
          <button @click="nextPage" :disabled="isLastPage" type="button" class="relative inline-flex items-center rounded-r-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 dark:ring-white/10 hover:bg-gray-50 dark:hover:bg-white/5 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed">
            <span class="sr-only">Next</span>
            <ChevronRightIcon class="h-5 w-5" aria-hidden="true" />
          </button>
        </div>
      </div>
    </nav>

    <AppModal :isOpen="isViewModalOpen" @close="closeViewModal">
      <template #title>View Record</template>
      <template #content>
        <FormsRecordView
          v-if="recordToView"
          :endpoint="endpoint"
          :fields="columns"
          :record="recordToView"
          title="Record Details"
          @updated="handleViewUpdateSuccess"
          :breadcrumb-trail="[{ name: endpoint.replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase()) }]"
        />
      </template>
    </AppModal>

    <AppModal :isOpen="isEditModalOpen" @close="closeEditModal">
      <template #title>Edit Record</template>
      <template #content>
        <DynamicCreateForm
          v-if="recordToEdit"
          :endpoint="endpoint"
          :fields="columns"
          :initial-values="recordToEdit"
          :record-id="recordToEdit.id"
          mode="edit"
          title="Edit Record"
          @updated="handleUpdateSuccess"
        />
      </template>
    </AppModal>

    <AppConfirmDialog :isOpen="isDeleteModalOpen" @cancel="closeDeleteModal" @confirm="confirmDelete" title="Confirm Deletion" message="Are you sure you want to delete this record? This action cannot be undone." />
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Menu, MenuButton, MenuItem, MenuItems } from '@headlessui/vue';
import { ChevronUpIcon, ChevronDownIcon, ChevronUpDownIcon, ChevronLeftIcon, ChevronRightIcon, EllipsisVerticalIcon, PencilIcon, TrashIcon } from '@heroicons/vue/20/solid';
import DynamicCreateForm from './DynamicCreateForm.vue';
import { useListView } from '../../composables/useListView';
import { useExportData } from '../../composables/useExportData';

defineOptions({ name: 'FormTable' });

export type Col = { key: string; label: string; type?: 'text' | 'number' | 'date' | 'paragraph' | 'choice'; displayPath?: string; include?: string; sortable?: boolean };

const props = withDefaults(defineProps<{
  endpoint: string
  columns: Col[]
  searchQuery?: string
  searchColumn?: string
  selectedFields: string[]
}>(), {
  searchQuery: '',
  searchColumn: '',
  selectedFields: () => [],
});

const {
  rows, loading, error, sortBy, sortOrder, currentPage, itemsPerPage, totalItems,
  isEditModalOpen, recordToEdit, isDeleteModalOpen, recordToDelete, pageInput,
  itemsPerPageOptions, totalPages, paginationNumbers, isFirstPage, isLastPage,
  pageStatusText, getNestedValue, formatCell, setSort, openEditModal,
  closeEditModal, handleUpdateSuccess, openDeleteModal, closeDeleteModal,
  confirmDelete, prevPage, nextPage, goToPage, load,
  isViewModalOpen, recordToView, openViewModal, closeViewModal, handleViewUpdateSuccess,
  fetchAllRows,
} = useListView(props);

const tableColumns = computed<Col[]>(() => {
  const selectedSet = new Set(props.selectedFields);
  const baseColumns = selectedSet.size === 0
    ? props.columns
    : props.columns.filter(c => selectedSet.has(c.key));
  return [...baseColumns, { key: 'actions', label: 'Actions', sortable: false }];
});

const { exportData } = useExportData({
  endpoint: props.endpoint,
  getColumns: () => props.columns,
  fetchAllRows,
  getNestedValue,
  formatCell,
});

defineExpose({ load, exportData });
</script>

<style scoped>
.spinner { border-radius: 50%; border: 2px solid currentColor; border-top-color: transparent; animation: spin 0.6s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>
