export type Col = {
  key: string
  label: string
  type?: 'text' | 'number' | 'date' | 'paragraph' | 'choice' | 'email' | 'select' | 'checkbox'
  displayPath?: string
  include?: string
  sortable?: boolean
  required?: boolean
  endpoint?: string
  valuePath?: string
  options?: string[]
  defaultValue?: any
}
