<template>
  <div 
    :class="['relative flex flex-col bg-white dark:bg-panel-dark border-r border-neutral-light/60 dark:border-border-dark transition-all duration-300 ease-in-out', isExpanded ? 'w-64' : 'w-20']" 
    :style="{ height: 'calc(100vh - 4rem)' }"
  >
    <div class="flex-1 p-4 space-y-4 overflow-y-auto min-h-0">
      <nav class="space-y-4">
        <div v-for="(group, groupIndex) in navigation" :key="group.title" class="space-y-2">
          <button @click="() => handleToggle(groupIndex)" class="w-full flex items-center justify-between p-3 text-xs font-bold uppercase tracking-wider rounded-lg transition-all duration-180 bg-background-alt/60 dark:bg-border-dark/50 hover:bg-background-alt dark:hover:bg-border-dark text-secondary dark:text-text-dark-muted focus:outline-none focus:ring-2 focus:ring-primary/30 border-l-3 border-primary">
            <div class="flex items-center">
              <component :is="group.icon" class="h-5 w-5 mr-3 flex-shrink-0 text-primary dark:text-primary-alt" />
              <span v-if="isExpanded" class="transition-opacity duration-300" :class="isExpanded ? 'opacity-100' : 'opacity-0'">{{ group.title }}</span>
            </div>
            <ChevronDownIcon v-if="isExpanded" class="h-4 w-4 transform transition-transform duration-300 text-primary" :class="isActive(groupIndex) && 'rotate-180'" />
          </button>

          <Collapse :when="isActive(groupIndex)" class="v-collapse">
            <div class="mt-2 space-y-1" :class="isExpanded ? 'pl-2' : ''">
              <NuxtLink
                v-for="item in group.items"
                :key="item.name"
                :to="item.href"
                class="group flex items-center p-2 text-sm font-medium rounded-lg transition-all duration-180"
                 :class="[
                  isLinkActive(item.href)
                    ? 'bg-primary/10 dark:bg-primary/20 text-primary dark:text-primary-alt border-l-2 border-primary'
                    : 'text-neutral-mid dark:text-text-dark-muted hover:bg-background-alt/60 dark:hover:bg-primary/10 hover:text-primary dark:hover:text-primary-alt',
                    !isExpanded && 'justify-center'
                ]"
              >
                <component :is="item.icon" class="h-5 w-5" :class="isExpanded && 'mr-3'" aria-hidden="true" />
                <span v-if="isExpanded" class="truncate transition-opacity duration-300" :class="isExpanded ? 'opacity-100' : 'opacity-0'">{{ item.name }}</span>
                 <span v-if="!isExpanded" class="absolute left-full ml-2 p-2 text-xs font-medium bg-neutral-dark text-white rounded-md opacity-0 group-hover:opacity-100 transition-opacity duration-180 whitespace-nowrap z-10">
                  {{ item.name }}
                </span>
              </NuxtLink>
            </div>
          </Collapse>
        </div>
      </nav>
    </div>

    <div class="flex-shrink-0 p-4 border-t border-neutral-light/60 dark:border-border-dark">
       <button @click="isExpanded = !isExpanded" class="w-full flex items-center p-3 text-sm font-medium rounded-lg transition-all duration-180 text-neutral-mid dark:text-text-dark-muted hover:bg-background-alt/60 dark:hover:bg-primary/10 hover:text-primary dark:hover:text-primary-alt focus:outline-none focus:ring-2 focus:ring-primary/30" :class="!isExpanded && 'justify-center'">
        <component :is="isExpanded ? ChevronDoubleLeftIcon : ChevronDoubleRightIcon" class="h-6 w-6 text-primary dark:text-primary-alt" :class="isExpanded && 'mr-2'" />
        <span v-if="isExpanded" class="transition-opacity duration-300 font-semibold" :class="isExpanded ? 'opacity-100' : 'opacity-0'">Collapse</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Collapse } from 'vue-collapsed';
import {
  ChevronDownIcon,
  ChevronDoubleLeftIcon,
  ChevronDoubleRightIcon,
  HomeIcon,
  BeakerIcon,
  BuildingStorefrontIcon,
  ArchiveBoxIcon,
  BugAntIcon,
  TruckIcon,
  WrenchScrewdriverIcon,
  DocumentChartBarIcon,
  MapIcon,
  SparklesIcon,
  UserGroupIcon
} from '@heroicons/vue/24/outline';

const route = useRoute();
const isExpanded = ref(true);

const isLinkActive = (href: string) => {
  return route.path === href || (href !== '/forms' && route.path.startsWith(href));
};

const navigation = [
  {
    title: 'Plants',
    icon: SparklesIcon,
    items: [
      { name: 'Overview', href: '/overview/plants', icon: HomeIcon },
      { name: 'Plants', href: '/forms/plants', icon: UserGroupIcon },
      { name: 'Strains', href: '/forms/strains', icon: BeakerIcon },
      { name: 'Plant Stages', href: '/forms/plant-stages', icon: DocumentChartBarIcon },
      { name: 'Batches', href: '/forms/batches', icon: ArchiveBoxIcon },
    ],
  },
  {
    title: 'Plant Actions',
    icon: WrenchScrewdriverIcon,
    items: [
      { name: 'Overview', href: '/overview/plant-actions', icon: HomeIcon },
      { name: 'Movement', href: '/forms/plant-movements', icon: MapIcon },
      { name: 'Watering', href: '/forms/watering-visual-inspections', icon: SparklesIcon }, // Placeholder icon
      { name: 'Pest Control', href: '/forms/pest-control-logs', icon: BugAntIcon },
      { name: 'Interventions', href: '/forms/plant-interventions', icon: WrenchScrewdriverIcon },
    ],
  },
  {
    title: 'Inventory',
    icon: ArchiveBoxIcon,
    items: [
      { name: 'Overview', href: '/overview/inventory', icon: HomeIcon },
      { name: 'Seed Lots', href: '/forms/seeds', icon: ArchiveBoxIcon },
      { name: 'Products', href: '/forms/products', icon: BuildingStorefrontIcon },
      { name: 'Packages', href: '/forms/batch-packages', icon: ArchiveBoxIcon },
    ],
  },
  {
    title: 'Premises',
    icon: HomeIcon,
    items: [
      { name: 'Overview', href: '/overview/premises', icon: HomeIcon },
      { name: 'Cleaning', href: '/forms/cleaning-logs', icon: SparklesIcon },
      { name: 'Inspections', href: '/forms/premises-inspections', icon: DocumentChartBarIcon },
      { name: 'Access Control', href: '/forms/secure-location-access-logs', icon: UserGroupIcon },
      { name: 'Video Inspections', href: '/forms/video-storage-inspections', icon: DocumentChartBarIcon },
      { name: 'pH Inspections', href: '/forms/runoff-ph-inspections', icon: BeakerIcon },
    ],
  },
];

// Collapse state management
const activeIndex = ref<number | null>(null);

function handleToggle(index: number) {
  if (activeIndex.value === index) {
    activeIndex.value = null;
  } else {
    activeIndex.value = index;
  }
}

const isActive = (index: number) => activeIndex.value === index;

</script>

<style>
.v-collapse {
  transition: height 300ms cubic-bezier(0.3, 0, 0.6, 1);
}
</style>
