<template>
  <div
    :class="[
      'inline-flex items-center justify-center rounded-full text-white font-medium select-none',
      sizeClasses[size]
    ]"
    :style="{ backgroundColor: backgroundColor }"
  >
    {{ initials }}
  </div>
</template>

<script setup lang="ts">
interface Props {
  name: string
  size?: 'small' | 'medium' | 'large'
}

const props = withDefaults(defineProps<Props>(), {
  size: 'medium'
})

const sizeClasses = {
  small: 'h-8 w-8 text-sm',
  medium: 'h-10 w-10 text-base',
  large: 'h-16 w-16 text-xl'
}

const initials = computed(() => {
  if (!props.name) return '??'
  
  const words = props.name.trim().split(/\s+/)
  if (words.length === 1) {
    return words[0].slice(0, 2).toUpperCase()
  }
  
  return (words[0][0] + words[words.length - 1][0]).toUpperCase()
})

// Generate a consistent color based on the name
const backgroundColor = computed(() => {
  if (!props.name) return '#6b7280' // gray-500 fallback
  
  // Simple hash function to generate consistent colors
  let hash = 0
  for (let i = 0; i < props.name.length; i++) {
    hash = props.name.charCodeAt(i) + ((hash << 5) - hash)
  }
  
  // Generate colors that work well with white text
  const colors = [
    '#ef4444', // red-500
    '#f97316', // orange-500
    '#eab308', // yellow-500
    '#22c55e', // green-500
    '#06b6d4', // cyan-500
    '#3b82f6', // blue-500
    '#8b5cf6', // violet-500
    '#ec4899', // pink-500
    '#64748b', // slate-500
    '#78716c', // stone-500
  ]
  
  return colors[Math.abs(hash) % colors.length]
})
</script>