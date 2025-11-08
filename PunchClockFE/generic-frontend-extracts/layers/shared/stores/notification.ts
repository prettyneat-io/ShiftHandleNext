import { defineStore } from 'pinia'

interface Notification {
  id: number
  message: string
  type: 'success' | 'error'
}

export const useNotificationStore = defineStore('notification', {
  state: () => ({
    notifications: [] as Notification[],
  }),
  actions: {
    addNotification(notification: { message: string; type: 'success' | 'error', duration?: number }) {
      const id = Date.now() + Math.random()
      this.notifications.push({ id, ...notification })

      setTimeout(() => {
        this.removeNotification(id)
      }, notification.duration || 7000)
    },
    removeNotification(id: number) {
      this.notifications = this.notifications.filter(n => n.id !== id)
    },
  },
})
