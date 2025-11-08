import type { AbFormSchema, AbSchemaFile } from "~/types/Form"

let _cache: AbFormSchema[] | null = null

export async function fetchSchemas($fetchFn?: typeof $fetch): Promise<AbFormSchema[]> {
  if (_cache) return _cache
  // Use injected $fetch on server if provided (Nuxt SSR-safe)
  const f = $fetchFn || $fetch
  const data = await f<AbSchemaFile>('/_assets/ab_form_schemas.json', {
    // Works in both SSR & SPA when you expose it via server route below
  })
  _cache = data.schemas
  return _cache!
}

export async function getSchemaBySlug(slug: string, $fetchFn?: typeof $fetch) {
  const schemas = await fetchSchemas($fetchFn)
  return schemas.find(s =>
    s.spreadsheetName &&
    s.spreadsheetName
      .toLowerCase()
      .replace(/[\s\/&]+/g, '-')
      .replace(/[^a-z0-9\-]/g, '')
      .replace(/\-+/g, '-')
      .replace(/^\-|\-$/g, '') === slug
  ) || null
}
