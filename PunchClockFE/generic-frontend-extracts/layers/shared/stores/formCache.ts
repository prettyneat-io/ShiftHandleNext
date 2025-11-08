import { defineStore } from 'pinia'

type CacheEntry = { data: any[]; ts: number }

export const useFormCache = defineStore('formCache', {
  state: () => ({
    byKey: {} as Record<string, CacheEntry>
  }),
  actions: {
    get(key: string, maxAgeMs = 2 * 60 * 1000) {
      const hit = this.byKey[key]
      if (!hit) return null
      if (Date.now() - hit.ts > maxAgeMs) return null
      return hit.data
    },
    set(key: string, data: any[]) {
      this.byKey[key] = { data, ts: Date.now() }
    },
    invalidate(key?: string) {
      if (!key) this.byKey = {}
      else delete this.byKey[key]
    }
  }
})
