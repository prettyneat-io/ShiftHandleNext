<template>
  <div 
    :class="[
      'relative flex flex-col bg-background-light dark:bg-background-dark border-r border-neutral-light dark:border-neutral-dark transition-all duration-300 ease-in-out',
      isExpanded ? 'w-80' : 'w-20'
    ]" 
    :style="{ height: 'calc(100vh - 4rem)' }"
  >
    <div class="flex-1 p-4 space-y-4 overflow-y-auto min-h-0">
      <nav class="space-y-4">
        <div v-for="(group, groupIndex) in navigation" :key="group.title" class="space-y-2">
          <button 
            @click="() => handleToggle(groupIndex)" 
            class="w-full flex items-center justify-between p-3 text-xs font-bold uppercase text-neutral-dark dark:text-neutral-light tracking-wider rounded-lg bg-neutral-light/30 dark:bg-neutral-dark/30 hover:bg-neutral-light dark:hover:bg-neutral-dark focus:outline-none focus:ring-2 focus:ring-primary border-l-3 border-primary"
          >
            <div class="flex items-center">
              <component :is="group.icon" class="h-5 w-5 mr-3 flex-shrink-0 text-primary" />
              <span v-if="isExpanded" class="transition-opacity duration-300" :class="isExpanded ? 'opacity-100' : 'opacity-0'">
                {{ group.title }}
              </span>
            </div>
            <component 
              :is="ChevronDownIcon" 
              v-if="isExpanded" 
              class="h-4 w-4 transform transition-transform duration-300 text-primary" 
              :class="isActive(groupIndex) && 'rotate-180'" 
            />
          </button>

          <Collapse :when="isActive(groupIndex)" class="v-collapse">
            <div class="mt-2 space-y-1" :class="isExpanded ? 'pl-2' : ''">
              <NuxtLink
                v-for="item in group.items"
                :key="item.name"
                :to="item.href"
                class="group flex items-center p-2 text-sm font-medium rounded-lg transition-colors"
                :class="[
                  isLinkActive(item.href)
                    ? 'bg-primary/10 dark:bg-primary/20 text-primary dark:text-primary-light border-l-2 border-primary'
                    : 'text-neutral-dark dark:text-neutral hover:bg-neutral-light/50 dark:hover:bg-neutral-dark/50 hover:text-neutral-dark dark:hover:text-white',
                  !isExpanded && 'justify-center'
                ]"
              >
                <component :is="item.icon" class="h-5 w-5" :class="isExpanded && 'mr-3'" aria-hidden="true" />
                <span 
                  v-if="isExpanded" 
                  class="truncate transition-opacity duration-300" 
                  :class="isExpanded ? 'opacity-100' : 'opacity-0'"
                >
                  {{ item.name }}
                </span>
                <span 
                  v-if="item.badge && isExpanded" 
                  class="ml-auto inline-flex items-center justify-center px-2 py-1 text-xs font-bold leading-none text-white bg-primary rounded-full"
                >
                  {{ item.badge }}
                </span>
                <span 
                  v-if="!isExpanded" 
                  class="absolute left-full ml-2 p-2 text-xs font-medium bg-neutral-dark text-white rounded-md opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap z-10"
                >
                  {{ item.name }}
                </span>
              </NuxtLink>
            </div>
          </Collapse>
        </div>
      </nav>
    </div>

    <div class="flex-shrink-0 p-4 border-t border-neutral-light dark:border-neutral-dark">
      <button 
        @click="isExpanded = !isExpanded" 
        class="w-full flex items-center p-3 text-sm font-medium rounded-lg text-neutral-dark dark:text-neutral-light hover:bg-neutral-light/50 dark:hover:bg-neutral-dark/50 focus:outline-none focus:ring-2 focus:ring-primary" 
        :class="!isExpanded && 'justify-center'"
      >
        <component 
          :is="isExpanded ? ChevronDoubleLeftIcon : ChevronDoubleRightIcon" 
          class="h-6 w-6 text-primary" 
          :class="isExpanded && 'mr-2'" 
        />
        <span 
          v-if="isExpanded" 
          class="transition-opacity duration-300 font-semibold" 
          :class="isExpanded ? 'opacity-100' : 'opacity-0'"
        >
          Collapse
        </span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Collapse } from 'vue-collapsed'
import {
  ChevronDownIcon,
  ChevronDoubleLeftIcon,
  ChevronDoubleRightIcon,
} from '@heroicons/vue/24/outline'
import { navigation, staffNavigation } from '~/config/navigation'

const route = useRoute()
const isExpanded = ref(true)

// Collapse state management
const activeIndex = ref<number | null>(0) // Start with first group expanded

function handleToggle(index: number) {
  if (activeIndex.value === index) {
    activeIndex.value = null
  } else {
    activeIndex.value = index
  }
}

const isActive = (index: number) => activeIndex.value === index

const isLinkActive = (href: string) => {
  if (href === '/') {
    return route.path === href
  }
  return route.path === href || route.path.startsWith(href + '/')
}
</script>

<style>
.v-collapse {
  transition: height 300ms cubic-bezier(0.3, 0, 0.6, 1);
}
</style>
