<template>
  <button
    :type="type"
    :class="buttonClass"
    :disabled="disabled || loading"
    @click="handleClick"
  >
    <span v-if="loading" class="spinner"></span>
    <slot />
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue';

type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'outline';
type ButtonSize = 'small' | 'medium' | 'large';

const props = withDefaults(defineProps<{
  variant?: ButtonVariant,
  type?: 'submit' | 'button' | 'reset',
  size?: ButtonSize,
  disabled?: boolean,
  loading?: boolean,
}>(), {
  variant: 'primary',
  type: 'submit',
  size: 'medium',
  disabled: false,
  loading: false,
});

const emit = defineEmits<{
  (e: 'click', event: MouseEvent): void
}>();

const sizeClass = computed(() => ({
  small: 'text-sm px-3 py-1.5',
  medium: 'text-base px-4 py-2',
  large: 'text-lg px-5 py-3'
}[props.size]));

const variantClass = computed(() => ({
  primary: 'bg-gradient-to-r from-primary to-primary-light text-white hover:shadow-lg hover:scale-[1.03]',
  secondary: 'bg-gradient-to-r from-secondary to-secondary-light text-white hover:shadow-lg hover:scale-[1.03]',
  danger: 'bg-gradient-to-r from-danger to-danger-light text-white hover:shadow-lg hover:scale-[1.03]',
  outline: 'border border-primary text-primary hover:bg-primary hover:text-white hover:shadow-lg hover:scale-[1.03]'
}[props.variant]));

const buttonClass = computed(() => [
  'inline-flex items-center justify-center font-semibold rounded-lg shadow-md transition-all duration-300 transform',
  'focus:outline-none focus:ring-2 focus:ring-offset-2',
  sizeClass.value,
  variantClass.value,
  { 'opacity-50 cursor-not-allowed': props.disabled || props.loading }
]);

function handleClick(event: MouseEvent) {
  if (!props.disabled && !props.loading) {
    emit('click', event);
  }
}
</script>

<style scoped>
.spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid currentColor;
  border-top: 2px solid transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-right: 8px;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.shadow-soft {
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
}
</style>
