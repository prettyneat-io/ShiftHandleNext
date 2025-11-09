import * as yup from 'yup';
import type { Field } from '../components/forms/DynamicCreateForm.vue';
import validationMetaJson from './validation-meta.json';

// Define the structure of the imported JSON
interface FieldMeta {
  type: string;
  isRequired: boolean;
  isId: boolean;
}
type ValidationMeta = {
  [model: string]: {
    [field: string]: FieldMeta;
  };
};
const validationMeta = validationMetaJson as ValidationMeta;

// --- NEW: Explicit mapping from plural endpoint slug to singular model name ---
export const endpointToModelMap: Record<string, string> = {
  'locations': 'Location',
  'lighting-cycles': 'LightingCycle',
  'strains': 'Strain',
  'seeds': 'Seed',
  'batches': 'Batch',
  'plants': 'Plant',
  'plant-stages': 'PlantStage',
  'products': 'Product',
  'batch-packages': 'BatchPackage',
  'inventory-items': 'InventoryItem',
  'premises-inspections': 'PremisesInspection',
  'compost-logs': 'CompostLog',
  'waste-management-logs': 'WasteManagementLog',
  'pest-control-logs': 'PestControlLog',
  'secure-location-access-logs': 'SecureLocationAccessLog',
  'video-storage-inspections': 'VideoStorageInspection',
  'plant-movements': 'PlantMovement',
  'watering-visual-inspections': 'WateringVisualInspection',
  'plant-interventions': 'PlantIntervention',
  'runoff-ph-inspections': 'RunoffPhInspection',
  'operation-recovery-data': 'OperationRecoveryData',
  'cleaning-logs': 'CleaningLog',
};

/**
 * Generates a Yup validation schema from an array of field definitions,
 * using metadata automatically generated from the Prisma schema.
 * @param fields An array of objects describing the form fields.
 * @param endpoint The API endpoint slug (e.g., 'batch-packages').
 * @returns A Yup object schema.
 */
export function generateYupSchema(fields: Field[], endpoint: string) {
  const shape: Record<string, yup.AnySchema> = {};

  // Use the map to get the correct model name
  const modelName = endpointToModelMap[endpoint];
  const modelMeta = modelName ? validationMeta[modelName] : undefined;

  if (!modelMeta) {
    console.warn(`Validation metadata not found for endpoint: "${endpoint}". Could not map to a model.`);
    return yup.object().shape({});
  }

  fields.forEach(field => {
    const meta = modelMeta[field.key];
    if (!meta) return; // Skip fields not in the Prisma model (like 'actions')

    let schema: yup.AnySchema;

    // Determine base type
    switch (field.type) {
      case 'number':
        schema = yup.number().typeError(`${field.label} must be a number`);
        break;
      case 'date':
        schema = yup.date().typeError(`${field.label} must be a valid date`);
        break;
      case 'related':
        schema = yup.number().typeError(`${field.label} must be selected`);
        break;
      default:
        schema = yup.string();
    }

    // Apply 'required' rule based on the metadata from the JSON file
    if (meta.isRequired && !meta.isId) { // isId check prevents requiring ID on create
      schema = schema.required(`${field.label} is required`);
    } else {
      schema = schema.transform((value) => (value === '' ? null : value)).nullable();
    }

    shape[field.key] = schema;
  });

  return yup.object().shape(shape);
}
