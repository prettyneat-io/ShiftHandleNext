<template>
  <form @submit="onSubmit" novalidate>
    <AppNestedBreadcrumbs v-if="breadcrumbTrail" :trail="breadcrumbTrail" @navigate="handleNavBack" />
    <div class="space-y-12 sm:space-y-16">
      <div>
        <h1 v-if="title" class="font-branding text-3xl text-neutral-dark dark:text-neutral-light border-neutral-light dark:border-neutral-dark flex flex-wrap items-center justify-between gap-4">{{ title }}</h1>
        <p v-if="subtitle" class="text-md text-neutral-dark dark:text-neutral-light opacity-70 mb-6">{{ subtitle }}</p>

        <fieldset :disabled="submitting" class="transition-opacity duration-300" :class="{ 'opacity-75': submitting }">
          <div class="border-b border-gray-900/10 pb-12 sm:divide-y sm:divide-gray-900/10 sm:border-t sm:pb-0 dark:border-white/10 dark:sm:divide-white/10">
            <div v-for="f in fields" :key="f.key" class="py-6">
              <AppInput
                :id="f.key"
                :name="f.key"
                :label="f.label"
                :type="f.type"
                :related="f.related"
                @create-new-related="handleCreateNewRelated"
                v-model="formFields[f.key].field.value"
                v-bind="formFields[f.key].attrs"
                :error="errors[f.key]"
              />
            </div>
          </div>
        </fieldset>

        <p v-if="serverError" class="mt-4 text-sm text-red-600 dark:text-red-400">{{ serverError }}</p>
      </div>
    </div>

    <div class="mt-6 flex items-center justify-end gap-x-6">
      <NuxtLink v-if="backTo" :to="backTo" class="text-sm/6 font-semibold text-neutral-dark dark:text-neutral-light">Cancel</NuxtLink>
      <AppButton
        variant="primary"
        size="small"
        :disabled="!meta.valid || submitting"
        :loading="submitting"
      >
        {{ submitTextComputed }}
      </AppButton>
    </div>

    <AppModal
      :isOpen="isCreateRelatedOpen"
      @close="closeCreateRelatedModal"
      :prevent-close-on-backdrop="true"
    >
      <template #title>New {{ relatedCreationConfig?.label }}</template>
      <template #content>
        <DynamicCreateForm
          v-if="relatedCreationConfig"
          :endpoint="relatedCreationConfig.endpoint"
          :fields="relatedCreationConfig.fields"
          title=""
          :subtitle="`Create a new ${relatedCreationConfig.label.toLowerCase()} to select it here.`"
          mode="create"
          @created="onRelatedCreated"
          :breadcrumb-trail="trailForChild"
          @navBack="handleChildNavBack"
        />
      </template>
    </AppModal>
  </form>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/yup';
import { useNotificationStore } from '../../stores/notification';
import AppModal from '../AppModal.vue';
import AppNestedBreadcrumbs from '../AppNestedBreadcrumbs.vue';
import { fetchColumnsForEndpoint } from '../../data/form-columns-dynamic';
import { useRefreshStore } from '../../stores/refresh';
import { generateYupSchema } from '../../utils/yup-schema-generator';

export type Field = {
  key: string;
  label: string;
  type?: 'text' | 'number' | 'date' | 'paragraph' | 'choice' | 'related';
  related?: { endpoint: string; label: string; }
  required?: boolean;
};

const props = defineProps<{
  endpoint: string
  fields: Field[]
  title: string
  subtitle?: string
  backTo?: string
  transformBeforeSubmit?: (payload: any) => any
  cacheInvalidateKey?: string
  mode?: 'create'|'edit'
  submitPath?: string
  recordId?: string | number
  initialValues?: Record<string, any>
  submitText?: string
  breadcrumbTrail?: { name: string }[]
}>()

const emit = defineEmits<{
  (e: 'created', record: any): void
  (e: 'updated', record: any): void
  (e: 'navBack', toIndex: number): void
}>()

const notificationStore = useNotificationStore();
const refreshStore = useRefreshStore();
const submitting = ref(false);
const serverError = ref('');

const formInitialValues = computed(() => {
  // If in edit mode with existing values, use them.
  if (props.mode === 'edit' && props.initialValues) {
    return props.initialValues;
  }
  // For create mode, generate an object with `null` for each field key.
  // This ensures every AppInput receives a valid prop type on mount.
  const defaults: Record<string, null> = {};
  props.fields.forEach(field => {
    defaults[field.key] = null;
  });
  return defaults;
});

// --- Initialize the Form with `useForm` and our Yup Generator ---
const { errors, handleSubmit, defineField, meta, setValues } = useForm({
  get validationSchema() {
    return toTypedSchema(generateYupSchema(props.fields, props.endpoint));
  },
  initialValues: formInitialValues.value,
  validateOnMount: true,
});

// Update form values if initialValues prop changes
watch(() => props.initialValues, (newValues) => {
  if (newValues) {
    setValues(newValues);
  }
}, { immediate: true, deep: true });

// --- Bind Inputs with `defineField` ---
const formFields = computed(() => {
    const fields: Record<string, any> = {};
    props.fields.forEach(f => {
      const [field, attrs] = defineField(f.key);
      fields[f.key] = { field, attrs };
    });
    return fields;
});

// --- Define the Submission Handler with `handleSubmit` ---
const onSubmit = handleSubmit(async (validatedValues) => {
  serverError.value = '';
  submitting.value = true;
  try {
    const cleanedValues = Object.entries(validatedValues)
      .reduce((acc, [key, value]) => {
        if (value !== null) {
          acc[key] = value;
        }
        return acc;
      }, {} as Record<string, any>);

    let payload = props.transformBeforeSubmit
      ? props.transformBeforeSubmit(cleanedValues)
      : cleanedValues;

    const url = props.submitPath || (
      props.mode === 'edit' && props.recordId
        ? `/api/${props.endpoint}/${props.recordId}`
        : `/api/${props.endpoint}`
    );
    const method = props.mode === 'edit' ? 'PUT' : 'POST';
    const res = await $fetch(url, { method, body: payload });

    notificationStore.addNotification({
      message: `Record ${props.mode === 'edit' ? 'updated' : 'created'} successfully!`,
      type: 'success',
    });

    if (props.cacheInvalidateKey) {
      const { useFormCache } = await import('../../stores/formCache');
      useFormCache().invalidate(props.cacheInvalidateKey);
    }
    emit(props.mode === 'edit' ? 'updated' : 'created', res);
  } catch (e: any) {
    const errorMessage = e.data?.error || e.message || 'An unexpected error occurred.';
    notificationStore.addNotification({ message: errorMessage, type: 'error' });
    serverError.value = errorMessage;
  } finally {
    submitting.value = false;
  }
});

// --- Logic for Nested Modals (unchanged) ---
const isCreateRelatedOpen = ref(false);
const relatedCreationConfig = ref<any | null>(null);
const activeRelatedFieldKey = ref<string | null>(null);

const handleCreateNewRelated = async (
  fieldKey: string, relatedConfig: Field['related']
) => {
  if (!relatedConfig) return;
  activeRelatedFieldKey.value = fieldKey;
  const relatedFields = await fetchColumnsForEndpoint(relatedConfig.endpoint);
  relatedCreationConfig.value = { ...relatedConfig, fields: relatedFields };
  isCreateRelatedOpen.value = true;
};

const onRelatedCreated = (newRecord: any) => {
  if (activeRelatedFieldKey.value && relatedCreationConfig.value) {
    const fieldKey = activeRelatedFieldKey.value;
    refreshStore.triggerRefresh(relatedCreationConfig.value.endpoint);
    const fieldRef = formFields.value[fieldKey]?.field;
    if(fieldRef) {
        fieldRef.value = newRecord.data.id;
    }
  }
  closeCreateRelatedModal();
};

const closeCreateRelatedModal = () => {
  isCreateRelatedOpen.value = false;
  relatedCreationConfig.value = null;
  activeRelatedFieldKey.value = null;
};

const currentFormName = computed(() => props.title.replace(/^New\s*/, ''));

const trailForChild = computed(() => {
  if (!relatedCreationConfig.value) return [];
  const myTrail = props.breadcrumbTrail || [{ name: currentFormName.value }];
  const child = { name: relatedCreationConfig.value.label };
  return [...myTrail, child];
});

const handleNavBack = (toIndex: number) => {
  const myLevel = (props.breadcrumbTrail?.length ?? 1) - 1;
  if (toIndex < myLevel) {
    emit('navBack', toIndex);
  }
};

const handleChildNavBack = (toIndex: number) => {
  closeCreateRelatedModal();
  handleNavBack(toIndex);
};

const submitTextComputed = computed(() => {
  return props.submitText || (props.mode === 'edit' ? 'Update' : 'Create')
});
</script>
