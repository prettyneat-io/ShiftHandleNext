<template>
  <section class="space-y-4">
    <div v-if="!loading && rows.length" class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
      <div v-for="row in rows" :key="row.id" @click="openViewModal(row)" class="col-span-1 flex flex-col rounded-lg bg-background-light dark:bg-neutral-dark/30 shadow-md border border-neutral-light dark:border-neutral-dark cursor-pointer transition-transform duration-150 hover:scale-[1.02]">
        <div class="flex items-start justify-between p-4">
          <div class="flex-1 pr-4">
            <h3 class="font-medium text-neutral-dark dark:text-neutral-light">{{ getNestedValue(row, primaryField.displayPath || primaryField.key) }}</h3>
            <p v-if="secondaryField" class="mt-1 text-sm text-neutral dark:text-neutral">{{ formattedSecondaryField(row) }}</p>
          </div>
          <Menu as="div" class="relative ml-auto shrink-0">
            <MenuButton @click.stop class="inline-flex justify-center rounded-full p-2 text-sm font-semibold text-neutral dark:text-neutral hover:bg-neutral-light/50 dark:hover:bg-neutral-dark/50">
              <span class="sr-only">Open options</span>
              <EllipsisVerticalIcon class="h-5 w-5" aria-hidden="true" />
            </MenuButton>
            <transition enter-active-class="transition ease-out duration-100" enter-from-class="transform opacity-0 scale-95" enter-to-class="transform opacity-100 scale-100" leave-active-class="transition ease-in duration-75" leave-from-class="transform opacity-100 scale-100" leave-to-class="transform opacity-0 scale-95">
              <MenuItems class="absolute right-0 z-10 mt-2 w-32 origin-top-right rounded-md bg-background-light dark:bg-background-dark shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none dark:ring-neutral-dark">
                <div class="py-1">
                  <MenuItem v-slot="{ active }">
                    <button @click.stop="openEditModal(row)" :class="[active ? 'bg-neutral-light/30 dark:bg-neutral-dark/30' : '', 'flex items-center w-full text-left px-4 py-2 text-sm text-neutral-dark dark:text-neutral-light']">
                      <PencilIcon class="mr-3 h-5 w-5 text-neutral" aria-hidden="true" />
                      <span>Edit</span>
                    </button>
                  </MenuItem>
                  <MenuItem v-slot="{ active }">
                    <button @click.stop="openDeleteModal(row)" :class="[active ? 'bg-danger/10' : '', 'flex items-center w-full text-left px-4 py-2 text-sm text-danger dark:text-danger-light']">
                      <TrashIcon class="mr-3 h-5 w-5 text-danger-light/80" aria-hidden="true" />
                      <span>Delete</span>
                    </button>
                  </MenuItem>
                </div>
              </MenuItems>
            </transition>
          </Menu>
        </div>

        <div class="px-4 pb-4 space-y-2 border-t border-neutral-light dark:border-neutral-dark pt-4">
          <CardField
            v-for="field in remainingFields"
            :key="field.key"
            :field="field"
            :value="getNestedValue(row, field.displayPath || field.key)"
          />
        </div>

        <div class="px-4 py-2 bg-neutral-light/30 dark:bg-neutral-dark/10 text-xs text-neutral dark:text-neutral/70 border-t border-neutral-light dark:border-neutral-dark flex justify-between">
          <span>Created: {{ new Date(row.createdDate).toLocaleDateString() }}</span>
          <span>Modified: {{ new Date(row.modifiedDate).toLocaleDateString() }}</span>
        </div>
      </div>
    </div>

    <div v-if="loading" class="px-4 py-10 text-center opacity-70">
      <div class="flex items-center justify-center gap-2">
        <div class="spinner h-5 w-5"></div>
        <span>Loading…</span>
      </div>
    </div>
    <div v-else-if="!rows.length" class="px-4 py-10 text-center opacity-70">
      <span v-if="searchQuery">No records found matching your search.</span>
      <span v-else>No records yet.</span>
    </div>

    <nav v-if="!loading && totalItems > 0" class="flex flex-wrap items-center justify-between gap-y-4 border-t border-neutral-light dark:border-neutral-dark pt-4" aria-label="Pagination">
      <div class="flex items-center gap-x-4">
        <p class="text-sm text-neutral-dark dark:text-neutral">
          {{ pageStatusText }}
        </p>
        <div class="flex items-center gap-x-2">
          <label for="items-per-page-grid" class="sr-only">Items per page</label>
          <select id="items-per-page-grid" v-model="itemsPerPage" class="block w-20 rounded-md border-0 py-1 pl-2 pr-7 text-neutral-dark shadow-sm ring-1 ring-inset ring-neutral-light focus:ring-2 focus:ring-inset focus:ring-primary dark:bg-neutral-dark/30 dark:text-neutral-light dark:ring-neutral-dark">
            <option v-for="option in itemsPerPageOptions" :key="option" :value="option">{{ option }}</option>
          </select>
          <span class="text-sm text-neutral-dark dark:text-neutral">per page</span>
        </div>
      </div>
      <div class="flex items-center gap-x-2">
        <div class="flex items-center text-sm">
           <label for="page-input-grid" class="sr-only">Go to page</label>
           <input id="page-input-grid" type="number" v-model.lazy="pageInput" @change="goToPage(pageInput)" class="block w-24 rounded-md border-0 py-1 text-center text-neutral-dark shadow-sm ring-1 ring-inset ring-neutral-light focus:ring-2 focus:ring-inset focus:ring-primary dark:bg-neutral-dark/30 dark:text-neutral-light dark:ring-neutral-dark" min="1" :max="totalPages"/>
           <span class="ml-2 text-neutral-dark dark:text-neutral">of {{ totalPages }}</span>
        </div>
        <div class="isolate inline-flex -space-x-px rounded-md shadow-sm">
          <button @click="prevPage" :disabled="isFirstPage" type="button" class="relative inline-flex items-center rounded-l-md px-2 py-2 text-neutral ring-1 ring-inset ring-neutral-light dark:ring-neutral-dark hover:bg-neutral-light/50 dark:hover:bg-neutral-dark/50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed">
            <span class="sr-only">Previous</span>
            <ChevronLeftIcon class="h-5 w-5" aria-hidden="true" />
          </button>
          <template v-for="(page, index) in paginationNumbers" :key="index">
            <span v-if="page === '...'" class="relative hidden items-center px-4 py-2 text-sm font-semibold text-neutral-dark dark:text-neutral ring-1 ring-inset ring-neutral-light dark:ring-neutral-dark md:inline-flex">...</span>
            <button v-else @click="goToPage(page as number)" type="button" :class="[page === currentPage ? 'z-10 bg-primary text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary' : 'text-neutral-dark dark:text-neutral-light ring-1 ring-inset ring-neutral-light dark:ring-neutral-dark hover:bg-neutral-light/50 dark:hover:bg-neutral-dark/50', 'relative hidden items-center px-4 py-2 text-sm font-semibold focus:z-20 focus:outline-offset-0 md:inline-flex']">
              {{ page }}
            </button>
          </template>
          <button @click="nextPage" :disabled="isLastPage" type="button" class="relative inline-flex items-center rounded-r-md px-2 py-2 text-neutral ring-1 ring-inset ring-neutral-light dark:ring-neutral-dark hover:bg-neutral-light/50 dark:hover:bg-neutral-dark/50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed">
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
          :title="`Details for ${getNestedValue(recordToView, primaryField.displayPath || primaryField.key)}`"
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
          :title="`Edit ${primaryField.label}`"
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
import { ChevronLeftIcon, ChevronRightIcon, EllipsisVerticalIcon, PencilIcon, TrashIcon } from '@heroicons/vue/20/solid';
import DynamicCreateForm from './DynamicCreateForm.vue';
import CardField from './CardField.vue';
import { useListView } from '../../composables/useListView';
import { useExportData } from '../../composables/useExportData';
import type { Col } from './FormTable.vue';

defineOptions({ name: 'CardGrid' });

const props = withDefaults(defineProps<{
  endpoint: string
  columns: Col[]
  searchQuery?: string
  searchColumn?: string
}>(), {
  searchQuery: '',
  searchColumn: '',
});

const {
  rows, loading, error, currentPage, itemsPerPage, totalItems,
  isEditModalOpen, recordToEdit, isDeleteModalOpen, recordToDelete, pageInput,
  itemsPerPageOptions, totalPages, paginationNumbers, isFirstPage, isLastPage,
  pageStatusText, getNestedValue, formatCell, openEditModal, closeEditModal,
  handleUpdateSuccess, openDeleteModal, closeDeleteModal, confirmDelete,
  prevPage, nextPage, goToPage, load,
  isViewModalOpen, recordToView, openViewModal, closeViewModal, handleViewUpdateSuccess,
  fetchAllRows,
} = useListView(props);

// ✨ FIX: Swapped primary and secondary field logic
const primaryField = computed(() => props.columns.length > 1 ? props.columns[1] : props.columns[0] || { key: 'id', label: 'ID' });
const secondaryField = computed(() => props.columns[0]);
const remainingFields = computed(() => props.columns.slice(2, 4));

const formattedSecondaryField = (row: any) => {
  if (!secondaryField.value) return '';
  const val = getNestedValue(row, secondaryField.value.displayPath || secondaryField.value.key);
  if (secondaryField.value.type === 'date') {
    const d = new Date(val);
    return isNaN(d.getTime()) ? val : d.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
  return formatCell(val, secondaryField.value.type);
};

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
