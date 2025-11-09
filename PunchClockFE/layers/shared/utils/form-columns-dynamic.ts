import type { Field } from '../components/forms/DynamicCreateForm.vue'

/**
 * Maps validation metadata types to form field types
 */
function mapTypeToFieldType(type: string): Field['type'] {
  switch (type) {
    case 'String':
      return 'text'
    case 'Int':
    case 'Float':
      return 'number'
    case 'DateTime':
      return 'date'
    default:
      return 'text'
  }
}

/**
 * Determines if a field is a foreign key relationship based on naming convention
 */
function isRelatedField(key: string): boolean {
  return key.endsWith('Id') && key !== 'id'
}

/**
 * Extracts the entity name from a foreign key field name
 * e.g., "strainId" -> "Strain", "locationId" -> "Location"
 */
function getRelatedEntityName(fieldKey: string): string {
  // Remove "Id" suffix
  const baseName = fieldKey.replace(/Id$/, '')
  // Capitalize first letter
  return baseName.charAt(0).toUpperCase() + baseName.slice(1)
}

/**
 * Converts an endpoint path to an entity name
 * e.g., "/api/strains" -> "Strain", "strains" -> "Strain"
 */
function endpointToEntityName(endpoint: string): string {
  // Remove /api/ prefix if present
  let entityPath = endpoint.replace(/^\/api\//, '')
  
  // Remove leading/trailing slashes
  entityPath = entityPath.replace(/^\/|\/$/g, '')
  
  // Convert plural to singular (simple approach)
  if (entityPath.endsWith('ies')) {
    entityPath = entityPath.slice(0, -3) + 'y'
  } else if (entityPath.endsWith('s')) {
    entityPath = entityPath.slice(0, -1)
  }
  
  // Capitalize first letter of each word (handle kebab-case)
  return entityPath
    .split('-')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join('')
}

/**
 * Fetches and generates field definitions for a given endpoint.
 * 
 * NOTE: This function previously relied on validation-meta.json which has been removed.
 * For new implementations, create entity-specific composables (like useStaffColumns)
 * that define columns based on the TypeScript types from the API client.
 * 
 * This function is kept for backwards compatibility but will return an empty array
 * and log a warning.
 * 
 * @deprecated Use entity-specific column composables instead (e.g., useStaffColumns)
 */
export async function fetchColumnsForEndpoint(endpoint: string): Promise<Field[]> {
  console.warn(
    `fetchColumnsForEndpoint is deprecated. ` +
    `Create an entity-specific composable (e.g., useStaffColumns) ` +
    `that defines columns based on TypeScript types from the API client.`
  )
  return []
}
