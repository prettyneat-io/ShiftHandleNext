<template>
  <form @submit.prevent="handleSubmit" class="flex flex-col gap-4">
    <slot />

    <slot name="submit-button" :loading="loading">
      <AppButton
        v-if="showSubmit"
        variant="primary"
        size="large"
        :disabled="loading || !isFormValid"
        :loading="loading"
        class="w-full"
      >
        {{ buttonText }}
      </AppButton>
    </slot>
  </form>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = withDefaults(defineProps<{
  buttonText?: string,
  showSubmit?: boolean,
  isFormValid?: boolean,
}>(), {
  buttonText: 'Submit',
  showSubmit: true,
  isFormValid: true,
});

const emit = defineEmits<{
  (e: 'submit'): void
}>();

const loading = ref(false);

async function handleSubmit() {
  if (!props.isFormValid) return;

  loading.value = true;
  try {
    await Promise.resolve(emit('submit'));
  } finally {
    loading.value = false;
  }
}
</script>
