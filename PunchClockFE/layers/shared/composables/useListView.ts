import { ref, computed, watch, onMounted } from 'vue';
import { useNotificationStore } from '../stores/notification';
import type { Col } from '../components/forms/FormTable.vue';

export function useListView(props: {
  endpoint: string;
  columns: Col[];
  searchQuery?: string;
  searchColumn?: string;
}) {
  // --- State ---
  const rows = ref<any[]>([]);
  const loading = ref(true);
  const error = ref('');
  const notificationStore = useNotificationStore();
  const sortBy = ref<string | null>(null);
  const sortOrder = ref<'asc' | 'desc' | null>(null);
  const currentPage = ref(1);
  const itemsPerPage = ref(10);
  const totalItems = ref(0);
  const isEditModalOpen = ref(false);
  const recordToEdit = ref<any | null>(null);
  const isDeleteModalOpen = ref(false);
  const recordToDelete = ref<any | null>(null);
  const isViewModalOpen = ref(false);
  const recordToView = ref<any | null>(null);
  const pageInput = ref(currentPage.value);
  const itemsPerPageOptions = [10, 25, 50, 100];

  // --- Computed Properties ---
  const totalPages = computed(() => {
    if (totalItems.value === 0) return 1;
    return Math.ceil(totalItems.value / itemsPerPage.value);
  });

  const paginationNumbers = computed(() => {
    const current = currentPage.value;
    const last = totalPages.value;
    const delta = 1;
    const left = current - delta;
    const right = current + delta;
    const range = [];
    const rangeWithDots: (number | string)[] = [];

    for (let i = 1; i <= last; i++) {
      if (i === 1 || i === last || (i >= left && i <= right)) {
        range.push(i);
      }
    }

    let l: number | null = null;
    for (const i of range) {
      if (l !== null) {
        if (i - l === 2) {
          rangeWithDots.push(l + 1);
        } else if (i - l > 2) {
          rangeWithDots.push('...');
        }
      }
      rangeWithDots.push(i);
      l = i;
    }
    return rangeWithDots;
  });

  const isFirstPage = computed(() => currentPage.value === 1);
  const isLastPage = computed(() => currentPage.value >= totalPages.value);

  const pageStatusText = computed(() => {
    if (totalItems.value === 0) return 'No results';
    const firstItem = (currentPage.value - 1) * itemsPerPage.value + 1;
    const lastItem = Math.min(currentPage.value * itemsPerPage.value, totalItems.value);
    return `Showing ${firstItem} to ${lastItem} of ${totalItems.value} results`;
  });

  // --- Helper Functions ---
  function getNestedValue(obj: any, path: string): any {
    if (!path) return null;
    return path.split('.').reduce((o, key) => (o && o[key] != null ? o[key] : null), obj);
  }

  function formatCell(val: any, type?: Col['type']) {
    if (val == null || val === '') return '—';
    if (type === 'date') {
      const d = new Date(val);
      return isNaN(d.getTime()) ? String(val) : d.toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    }
    return String(val);
  }

  function formatDateTimeForInput(date: string | Date | null): string | null {
    if (!date) return null;
    try {
      const d = new Date(date);
      if (isNaN(d.getTime())) return null;
      const pad = (num: number) => num.toString().padStart(2, '0');
      const year = d.getFullYear();
      const month = pad(d.getMonth() + 1);
      const day = pad(d.getDate());
      const hours = pad(d.getHours());
      const minutes = pad(d.getMinutes());
      return `${year}-${month}-${day}T${hours}:${minutes}`;
    } catch {
      return null;
    }
  }

  // ✨ NEW: Sort Handler
  function setSort(key: string) {
    if (sortBy.value === key) {
      if (sortOrder.value === 'asc') {
        sortOrder.value = 'desc';
      } else if (sortOrder.value === 'desc') {
        sortOrder.value = null;
      } else {
        sortOrder.value = 'asc';
      }
    } else {
      sortBy.value = key;
      sortOrder.value = 'asc';
    }
  }

  // --- Modal Actions ---
  const openViewModal = (record: any) => {
    recordToView.value = record;
    isViewModalOpen.value = true;
  };
  const closeViewModal = () => {
    isViewModalOpen.value = false;
    recordToView.value = null;
  };
  const handleViewUpdateSuccess = () => {
    closeViewModal();
    load(); // Reload data after an update from the view modal
  };

  const openEditModal = (record: any) => {
    const recordCopy = JSON.parse(JSON.stringify(record));
    props.columns.forEach(col => {
      if (col.type === 'date' && recordCopy[col.key]) {
        recordCopy[col.key] = formatDateTimeForInput(recordCopy[col.key]);
      }
    });
    recordToEdit.value = recordCopy;
    isEditModalOpen.value = true;
  };
  const closeEditModal = () => { isEditModalOpen.value = false; recordToEdit.value = null; };
  const handleUpdateSuccess = () => { closeEditModal(); load(); };

  const openDeleteModal = (record: any) => { recordToDelete.value = record; isDeleteModalOpen.value = true; };
  const closeDeleteModal = () => { isDeleteModalOpen.value = false; recordToDelete.value = null; };
  const confirmDelete = async () => {
    if (!recordToDelete.value) return;
    try {
      await $fetch(`/api/${props.endpoint}/${recordToDelete.value.id}`, { method: 'DELETE' });
      notificationStore.addNotification({ message: 'Record deleted successfully.', type: 'success' });
      closeDeleteModal();
      const remainingItemsOnPage = rows.value.length - 1;
      if (remainingItemsOnPage === 0 && currentPage.value > 1) {
        currentPage.value--;
      } else {
        await load();
      }
    } catch (e: any) {
      notificationStore.addNotification({ message: e.data?.error || 'Failed to delete record.', type: 'error' });
      closeDeleteModal();
    }
  };

  // --- Pagination Actions ---
  const prevPage = () => { if (!isFirstPage.value) currentPage.value--; };
  const nextPage = () => { if (!isLastPage.value) currentPage.value++; };
  const goToPage = (page: number | string) => {
    const pageNum = Number(page);
    if (!isNaN(pageNum) && pageNum >= 1 && pageNum <= totalPages.value) {
      currentPage.value = pageNum;
    }
  };

  // --- Data Fetching ---
  function buildQueryParams(overrides: { page?: number; limit?: number } = {}) {
    const queryParams: Record<string, string | number> = {};
    const page = overrides.page ?? currentPage.value;
    const limit = overrides.limit ?? itemsPerPage.value;
    queryParams.page = page;
    queryParams.limit = limit;
    if (props.searchQuery && props.searchColumn) {
      queryParams[props.searchColumn] = props.searchQuery;
    }
    if (sortBy.value && sortOrder.value) {
      queryParams.sort = sortBy.value;
      queryParams.order = sortOrder.value;
    }
    const includes = props.columns.map(c => c.include).filter((p): p is string => !!p);
    const includeParam = [...new Set(includes)].join(',');
    if (includeParam) {
      queryParams.include = includeParam;
    }
    return queryParams;
  }

  async function load() {
    try {
      loading.value = true;
      error.value = '';
      const res = await $fetch<{ data: any[], total: number, meta?: { total: number } }>(
        `/api/${props.endpoint}`,
        { params: buildQueryParams() },
      );
      rows.value = res.data;
      // Support both response formats: res.total (backend) or res.meta.total (alternative)
      totalItems.value = res.total ?? res.meta?.total ?? 0;
    } catch (e: any) {
      const errorMessage = e.data?.error || 'Failed to load data';
      error.value = errorMessage;
      rows.value = [];
      totalItems.value = 0;
    } finally {
      loading.value = false;
    }
  }

  async function fetchAllRows(limitOverride?: number) {
    const total = totalItems.value;
    const limit = limitOverride ?? (total > 0 ? total : itemsPerPage.value);
    const params = buildQueryParams({ page: 1, limit });
    const res = await $fetch<{ data: any[] }>(`/api/${props.endpoint}`, { params });
    return res.data;
  }

  // --- Reactivity ---
  watch([() => props.searchQuery, () => props.searchColumn], () => {
    if (currentPage.value === 1) {
      load();
    } else {
      currentPage.value = 1;
    }
  });

  watch([sortBy, sortOrder], load);

  watch(currentPage, (newPage) => {
    pageInput.value = newPage;
    load();
  });

  // ✨ FIX: Updated this watcher to prevent duplicate requests
  watch(itemsPerPage, () => {
    if (currentPage.value === 1) {
      load();
    } else {
      currentPage.value = 1;
    }
  });

  onMounted(load);

  // Return everything the components need
  return {
    rows, loading, error, sortBy, sortOrder, currentPage, itemsPerPage, totalItems,
    isEditModalOpen, recordToEdit, isDeleteModalOpen, recordToDelete, pageInput,
    itemsPerPageOptions, totalPages, paginationNumbers, isFirstPage, isLastPage,
    pageStatusText, getNestedValue, formatCell, setSort, openEditModal,
    closeEditModal, handleUpdateSuccess, openDeleteModal, closeDeleteModal,
    confirmDelete, prevPage, nextPage, goToPage, load,
    isViewModalOpen, recordToView, openViewModal, closeViewModal, handleViewUpdateSuccess,
    fetchAllRows,
  };
}
