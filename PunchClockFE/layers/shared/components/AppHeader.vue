<template>
  <Disclosure as="nav" class="relative bg-white/95 border-b border-neutral-light/60 backdrop-blur dark:bg-background-dark dark:border-border-dark" v-slot="{ open }">
    <div class="mx-auto px-4">
      <div class="flex h-16 items-center justify-between gap-4">
        <div class="flex items-center gap-6 min-w-0">
          <div class="flex shrink-0 items-center">
            <NuxtLink to="/" class="flex items-center space-x-3" aria-label="Home">
              <div class="p-2 rounded-lg border-2 border-primary bg-primary/10 dark:bg-primary/20">
                <svg class="h-6 w-6 text-primary" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M11.5,6C10.97,6.5 10.5,7.08 10.15,7.72C9.8,8.36 9.58,9.05 9.5,9.76C9.42,10.47 9.47,11.19 9.65,11.88C9.83,12.57 10.13,13.22 10.53,13.8C10.93,14.38 11.43,14.88 12,15.28C12.57,14.88 13.07,14.38 13.47,13.8C13.87,13.22 14.17,12.57 14.35,11.88C14.53,11.19 14.58,10.47 14.5,9.76C14.42,9.05 14.2,8.36 13.85,7.72C13.5,7.08 13.03,6.5 12.5,6H11.5M12,11C11.45,11 11,10.55 11,10C11,9.45 11.45,9 12,9C12.55,9 13,9.45 13,10C13,10.55 12.55,11 12,11Z"/>
                </svg>
              </div>

            </NuxtLink>
          </div>
          <div class="hidden lg:flex lg:space-x-8">
            <NuxtLink
              v-for="item in navigation"
              :key="item.name"
              :to="item.href"
              :class="[
                isActive(item.href)
                  ? 'border-primary text-primary border-b-2'
                  : 'border-transparent text-secondary hover:text-primary-alt dark:text-text-dark-muted dark:hover:text-primary-alt',
                  'inline-flex items-center border-b-2 px-1 pt-1 text-sm font-medium transition-colors duration-180'
              ]"
            >{{ item.name }}</NuxtLink>
          </div>
        </div>
        <div class="flex items-center gap-2 ml-auto">
          <div class="hidden lg:grid w-full max-w-lg grid-cols-1 lg:max-w-xs">
            <form @submit.prevent="handleSearch" class="relative">
              <input
                v-model="searchQuery"
                type="search"
                name="search"
                class="block w-full rounded-md border py-1.5 pr-3 pl-10 shadow-sm transition-all duration-180 border-neutral-light/70 bg-white text-neutral-dark placeholder:text-neutral-mid/60 focus:border-primary focus:ring-2 focus:ring-primary/30 sm:text-sm sm:leading-6 dark:bg-white/5 dark:text-text-dark dark:border-border-dark dark:placeholder:text-text-dark-muted/60 dark:focus:border-primary dark:focus:ring-primary/40"
                placeholder="Search"
                :disabled="isSearching"
              />
              <button
                type="submit"
                class="absolute inset-y-0 left-0 flex items-center pl-3 text-secondary/80 hover:text-primary dark:text-text-dark-muted dark:hover:text-primary"
                :disabled="isSearching || !searchQuery.trim()"
                aria-label="Search"
              >
                <MagnifyingGlassIcon v-if="!isSearching" class="size-5" aria-hidden="true" />
                <svg v-else class="animate-spin size-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="m15.84 7.16a1 1 0 0 1 1.42 1.42l-8.49 8.49a1 1 0 0 1-1.42-1.42l8.49-8.49z"></path>
                </svg>
              </button>
            </form>
          </div>
          <div class="hidden lg:flex lg:items-center lg:gap-2">
            <Menu as="div" class="relative text-neutral-dark dark:text-neutral-light">
              <MenuButton
                class="inline-flex items-center justify-center rounded-full p-2 text-secondary hover:bg-primary/10 focus:outline-2 focus:outline-offset-2 focus:ring-2 focus:ring-primary/30 dark:text-text-dark-muted dark:hover:bg-primary/20"
                aria-label="Select theme"
              >
                <SwatchIcon class="size-5" aria-hidden="true" />
                <span class="sr-only">Theme selector</span>
              </MenuButton>
              <MenuItems class="absolute right-0 z-20 mt-2 w-56 origin-top-right rounded-lg border border-neutral-light/60 bg-white py-2 shadow-2xl shadow-neutral-dark/20 dark:border-border-dark dark:bg-panel-dark">
                <div class="px-4 pb-2 text-xs font-semibold uppercase tracking-wide text-neutral-mid/80 dark:text-text-dark-muted/80">
                  Choose Theme
                  <div class="mt-1 text-sm font-medium text-neutral-dark dark:text-text-dark">{{ themeLabel }}</div>
                </div>
                <MenuItem
                  v-for="themeOption in availableThemes"
                  :key="themeOption.key"
                  v-slot="{ active }"
                >
                  <button
                    type="button"
                    class="flex w-full items-center gap-3 px-4 py-2 text-left text-sm"
                    :class="[
                      themeOption.key === currentThemeKey
                        ? 'text-primary dark:text-primary-alt'
                        : active
                          ? 'bg-primary/5 text-neutral-dark dark:bg-primary/10 dark:text-text-dark'
                          : 'text-neutral-mid dark:text-text-dark-muted'
                    ]"
                    @click="selectTheme(themeOption.key)"
                  >
                    <span class="flex h-4 w-4 items-center justify-center">
                      <CheckIcon
                        v-if="themeOption.key === currentThemeKey"
                        class="size-4"
                        aria-hidden="true"
                      />
                    </span>
                    <span>{{ themeOption.name }}</span>
                  </button>
                </MenuItem>
              </MenuItems>
            </Menu>
            <button
              @click="toggleTheme"
              type="button"
              class="relative shrink-0 rounded-full p-1 text-secondary hover:text-primary focus:outline-2 focus:outline-offset-2 focus:ring-2 focus:ring-primary/30 dark:text-text-dark-muted dark:hover:text-primary-alt"
              :aria-label="isDarkMode ? 'Toggle Light Mode' : 'Toggle Dark Mode'"
            >
              <span class="absolute -inset-1.5" />
              <span class="sr-only">Toggle theme</span>
              <component :is="isDarkMode ? SunIcon : MoonIcon" class="size-6" aria-hidden="true" />
            </button>

          <template v-if="isAuthenticated">
            <button type="button" class="relative ml-2 shrink-0 rounded-full p-1 text-secondary hover:text-primary focus:outline-2 focus:outline-offset-2 focus:ring-2 focus:ring-primary/30 dark:text-text-dark-muted dark:hover:text-primary-alt">
              <span class="absolute -inset-1.5" />
              <span class="sr-only">View notifications</span>
              <BellIcon class="size-6" aria-hidden="true" />
            </button>

            <Menu as="div" class="relative ml-4 shrink-0">
              <MenuButton class="relative flex rounded-full focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:ring-2 focus-visible:ring-primary/50">
                <span class="absolute -inset-1.5" />
                <span class="sr-only">Open user menu</span>
                <AppAvatar :name="displayName" size="small" />
              </MenuButton>

              <transition enter-active-class="transition ease-out duration-100" enter-from-class="transform opacity-0 scale-95" enter-to-class="transform opacity-100 scale-100" leave-active-class="transition ease-in duration-75" leave-from-class="transform opacity-100 scale-100" leave-to-class="transform opacity-0 scale-95">
                <MenuItems class="absolute right-0 z-10 mt-2 w-48 origin-top-right rounded-lg bg-white py-1 shadow-lg border border-neutral-light/60 dark:bg-panel-dark dark:border-border-dark dark:shadow-2xl dark:shadow-black/60">
                  <MenuItem v-for="item in userNavigation" :key="item.name" v-slot="{ active }">
                    <a :href="item.href" :class="[active ? 'bg-primary/5 dark:bg-primary/10' : '', 'block px-4 py-2 text-sm text-neutral-dark dark:text-text-dark transition-colors duration-180']">{{ item.name }}</a>
                  </MenuItem>
                  <MenuItem v-slot="{ active }">
                    <button @click="logout" :class="[active ? 'bg-primary/5 dark:bg-primary/10' : '', 'block w-full text-left px-4 py-2 text-sm text-neutral-dark dark:text-text-dark transition-colors duration-180']">Sign out</button>
                  </MenuItem>
                </MenuItems>
              </transition>
            </Menu>
          </template>
          <AppButton v-else @click="navigateTo('/login')" variant="primary" size="small" class="ml-2">
            Login
          </AppButton>
          </div>
          <div class="flex items-center lg:hidden">
            <DisclosureButton class="relative inline-flex items-center justify-center rounded-md p-2 text-secondary hover:bg-primary/10 hover:text-primary focus:outline-2 focus:ring-2 focus:ring-primary/30 dark:text-text-dark-muted dark:hover:bg-primary/20 dark:hover:text-primary-alt">
              <span class="absolute -inset-0.5" />
              <span class="sr-only">Open main menu</span>
              <Bars3Icon v-if="!open" class="block size-6" aria-hidden="true" />
              <XMarkIcon v-else class="block size-6" aria-hidden="true" />
            </DisclosureButton>
          </div>
        </div>
      </div>
    </div>

    <DisclosurePanel class="lg:hidden">
      <div class="space-y-1 pt-2 pb-3">
        <DisclosureButton
          v-for="item in navigation"
          :key="item.name"
          as="a"
          :href="item.href"
          :class="[
            isActive(item.href)
              ? 'border-primary bg-primary/10 text-primary dark:bg-primary/20 dark:text-primary-alt'
              : 'border-transparent text-secondary hover:border-neutral-light hover:bg-background-alt/60 hover:text-primary dark:text-text-dark-muted dark:hover:border-border-dark dark:hover:bg-primary/10 dark:hover:text-primary-alt',
              'block border-l-4 py-2 pl-3 pr-4 text-base font-medium transition-colors duration-180'
          ]"
        >{{ item.name }}</DisclosureButton>

        <!-- Mobile Search -->
        <div class="px-3 py-2">
          <form @submit.prevent="handleSearch">
            <div class="relative">
              <input
                v-model="searchQuery"
                type="search"
                placeholder="Search..."
                class="block w-full rounded-md border py-2 pl-10 pr-3 shadow-sm transition-all duration-180 border-neutral-light/70 bg-white text-neutral-dark placeholder:text-neutral-mid/60 focus:border-primary focus:ring-2 focus:ring-primary/30 dark:bg-white/5 dark:text-text-dark dark:border-border-dark dark:placeholder:text-text-dark-muted/60"
                :disabled="isSearching"
              />
              <div class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                <MagnifyingGlassIcon v-if="!isSearching" class="h-5 w-5 text-gray-400" aria-hidden="true" />
                <svg v-else class="animate-spin h-5 w-5 text-gray-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="m15.84 7.16a1 1 0 0 1 1.42 1.42l-8.49 8.49a1 1 0 0 1-1.42-1.42l8.49-8.49z"></path>
                </svg>
              </div>
            </div>
          </form>
        </div>
      </div>
      <div class="border-t border-neutral-light/60 pt-4 pb-3 dark:border-border-dark">
          <template v-if="isAuthenticated && user">
            <div class="flex items-center px-4">
              <div class="shrink-0">
                <AppAvatar :name="displayName" size="medium" />
              </div>
              <div class="ml-3">
                <div class="text-base font-medium text-neutral-dark dark:text-text-dark">{{ displayName }}</div>
                <div class="text-sm font-medium text-neutral-mid dark:text-text-dark-muted">{{ user.email }}</div>
              </div>
              <button type="button" class="relative ml-auto shrink-0 rounded-full p-1 text-secondary hover:text-primary focus:outline-2 focus:ring-2 focus:ring-primary/30 dark:text-text-dark-muted dark:hover:text-primary-alt">
                <span class="absolute -inset-1.5" />
                <span class="sr-only">View notifications</span>
                <BellIcon class="size-6" aria-hidden="true" />
              </button>
            </div>
            <div class="mt-3 space-y-1">
                  <DisclosureButton v-for="item in userNavigation" :key="item.name" as="a" :href="item.href" class="block px-4 py-2 text-base font-medium text-secondary hover:bg-background-alt/60 hover:text-primary transition-colors duration-180 dark:text-text-dark-muted dark:hover:bg-primary/10 dark:hover:text-primary-alt">{{ item.name }}</DisclosureButton>
                  <DisclosureButton @click="logout" as="button" class="block w-full text-left px-4 py-2 text-base font-medium text-secondary hover:bg-background-alt/60 hover:text-primary transition-colors duration-180 dark:text-text-dark-muted dark:hover:bg-primary/10 dark:hover:text-primary-alt">Sign out</DisclosureButton>
            </div>
          </template>
          <div v-else class="mt-3 space-y-1">
            <DisclosureButton as="a" href="/login" class="block px-4 py-2 text-base font-medium text-secondary hover:bg-background-alt/60 hover:text-primary transition-colors duration-180 dark:text-text-dark-muted dark:hover:bg-primary/10 dark:hover:text-primary-alt">Login</DisclosureButton>
          </div>
      </div>
    </DisclosurePanel>
  </Disclosure>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { Disclosure, DisclosureButton, DisclosurePanel, Menu, MenuButton, MenuItem, MenuItems } from '@headlessui/vue'
import { MagnifyingGlassIcon } from '@heroicons/vue/20/solid'
import { Bars3Icon, BellIcon, XMarkIcon, SunIcon, MoonIcon, SwatchIcon, CheckIcon } from '@heroicons/vue/24/outline'
import { useColorMode } from '@vueuse/core'
import { useAuthStore } from '../stores/auth'
import { useThemeManager } from '../composables/useThemeManager'
import { type ThemeKey } from '../theme.config'

const authStore = useAuthStore()
const isAuthenticated = computed(() => authStore.isAuthenticated)
const user = computed(() => authStore.user)

// Compute display name from firstName and lastName, fallback to username or generic name
const displayName = computed(() => {
  if (!user.value) {
    return 'User'
  }

  const first = (user.value as any).firstName?.trim() ?? ''
  const last = (user.value as any).lastName?.trim() ?? ''
  const fullName = [first, last].filter(Boolean).join(' ')
  return fullName || (user.value as any).username || user.value.name || 'User'
})

const { availableThemes, currentThemeKey, currentTheme: activeTheme, setTheme, initialize: ensureTheme } = useThemeManager()
ensureTheme()

const themeLabel = computed(() => activeTheme.value?.name ?? 'Theme')

const selectTheme = (key: ThemeKey) => {
  setTheme(key)
}

// Search functionality
const searchQuery = ref('')
const isSearching = ref(false)

const handleSearch = async () => {
  const query = searchQuery.value.trim()
  if (!query) return

  isSearching.value = true

  try {
    await navigateTo({
      path: '/search',
      query: { q: query }
    })
  } catch (error) {
    console.error('Navigation error:', error)
  } finally {
    isSearching.value = false
  }
}

const logout = () => {
  authStore.logout()
  // Optional: Redirect to login page after logout
  navigateTo('/login')
}

const colorMode = useColorMode()
const isDarkMode = computed(() => colorMode.value === 'dark')
const toggleTheme = () => {
  colorMode.value = isDarkMode.value ? 'light' : 'dark'
}

const route = useRoute()
const isActive = (href: string) => {
  if (href === '/') return route.path === '/'
  return route.path.startsWith(href)
}
const navigation = [
  { name: 'ShiftHandle Next', href: '/' },
]

const userNavigation = [
  { name: 'Your Profile', href: '#' },
  { name: 'Settings', href: '#' },
]
</script>

