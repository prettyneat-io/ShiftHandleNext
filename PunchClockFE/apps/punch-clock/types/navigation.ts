/**
 * Navigation types for Punch Clock application
 */

export interface NavigationItem {
  name: string
  href: string
  icon: any
  badge?: string | number
}

export interface NavigationGroup {
  title: string
  icon: any
  items: NavigationItem[]
}
