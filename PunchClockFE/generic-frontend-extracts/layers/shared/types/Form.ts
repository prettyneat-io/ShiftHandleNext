export type AbFieldType = 'text' | 'number' | 'date' | 'paragraph' | 'choice'

export interface AbField {
  key: string
  label: string
  type: AbFieldType
}

export interface AbFormSchema {
  spreadsheetId: string
  spreadsheetName: string
  spreadsheetUrl: string
  dataSheetId: number
  dataSheetName: string
  layout: 'vertical' | 'horizontal'
  valueColumns: string[]
  fields: AbField[]
}

export interface AbSchemaFile {
  generatedAt: string
  schemas: AbFormSchema[]
}
