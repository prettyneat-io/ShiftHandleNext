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

type ButtonVariant = 'primary' | 'secondary' | 'tertiary' | 'destructive';
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
  primary: 'bg-gradient-to-r from-primary to-primary-alt text-white shadow-md shadow-primary/15 hover:from-primary-alt hover:to-primary-alt/80 hover:shadow-lg hover:shadow-primary/20 focus:ring-2 focus:ring-primary-alt/40 focus:ring-offset-2',
  secondary: 'bg-gradient-to-r from-white to-background-alt text-primary border border-primary/20 hover:border-primary hover:text-primary-alt focus:ring-2 focus:ring-primary/30',
  tertiary: 'bg-transparent text-primary underline-offset-4 hover:text-primary-alt hover:underline focus:ring-2 focus:ring-primary/30',
  destructive: 'bg-transparent text-danger border border-danger/40 hover:bg-danger/10 focus:ring-2 focus:ring-danger/40'
}[props.variant]));

const buttonClass = computed(() => [
  'inline-flex items-center justify-center font-semibold rounded-md transition-all duration-180 ease-out',
  'focus:outline-none',
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
</style>
