export function slugify(input: string) {
  return input
    .toLowerCase()
    .replace(/[\s\/&]+/g, '-')
    .replace(/[^a-z0-9\-]/g, '')
    .replace(/\-+/g, '-')
    .replace(/^\-|\-$/g, '')
}

export function snakeToCamel(s: string) {
  return s.replace(/_([a-z])/g, (_, c) => c.toUpperCase())
}
