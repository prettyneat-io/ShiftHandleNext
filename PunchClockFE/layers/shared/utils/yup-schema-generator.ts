import * as yup from 'yup';
import type { Field } from '../components/forms/DynamicCreateForm.vue';

/**
 * Generates a Yup validation schema from an array of field definitions.
 * 
 * This version uses the field definitions directly instead of relying on
 * external validation metadata files, making it simpler and more maintainable.
 * 
 * @param fields An array of objects describing the form fields.
 * @param endpoint The API endpoint slug (e.g., 'staff', 'departments') - currently unused but kept for API compatibility.
 * @returns A Yup object schema.
 */
export function generateYupSchema(fields: Field[], endpoint?: string) {
  const shape: Record<string, yup.AnySchema> = {};

  fields.forEach(field => {
    let schema: yup.AnySchema;

    // Determine base type from field type
    switch (field.type) {
      case 'number':
        schema = yup.number().typeError(`${field.label} must be a number`);
        break;
      case 'date':
        schema = yup.date().typeError(`${field.label} must be a valid date`);
        break;
      case 'related':
        // Related fields typically pass IDs as strings or numbers
        schema = yup.string().typeError(`${field.label} must be selected`);
        break;
      case 'email':
        schema = yup.string().email(`${field.label} must be a valid email address`);
        break;
      default:
        schema = yup.string();
    }

    // Apply 'required' rule based on field configuration
    if (field.required) {
      schema = schema.required(`${field.label} is required`);
    } else {
      // For optional fields, allow null/undefined and transform empty strings to null
      schema = schema.transform((value) => (value === '' ? null : value)).nullable();
    }

    shape[field.key] = schema;
  });

  return yup.object().shape(shape);
}
