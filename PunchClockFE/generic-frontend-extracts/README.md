# Generic Frontend Extract

This is a generic frontend template extracted from the AB Cultivation project.

## What's Included

### Shared Layer (`layers/shared`)
- **Components**: All generic UI components (buttons, forms, cards, modals, headers, sidebar, etc.)
- **Layouts**: Auth and default layouts
- **Stores**: Authentication, loading, notifications, form cache, and refresh stores
- **Composables**: Form schemas, list views, export data, theme manager
- **Plugins**: API client and theme management
- **Utils**: String utilities, validation schema generator
- **Styles**: Complete CSS including fonts and Tailwind configuration
- **Authentication Pages**: Login, signup, forgot password

### App Template (`apps/app-template`)
- Base application structure
- Generic middleware (auth)
- Loading plugin
- Configuration files (nuxt.config, tsconfig, etc.)

## What's NOT Included

The following cultivation-specific items were excluded:
- Form endpoints configuration (`form-endpoints.ts`)
- Form columns definitions (`form-columns.ts`, `form-columns-dynamic.ts`)
- Cultivation-specific pages (inventory, logs, etc.)
- Zod schemas (cultivation domain models)
- Dashboard components (cultivation-specific)
- Overview components and pages

## Getting Started

### 1. Copy to Your New Project

```bash
cp -r generic-frontend-extracts/layers your-project/layers
cp -r generic-frontend-extracts/apps/app-template your-project/apps/your-app-name
```

### 2. Install Dependencies

```bash
cd your-project
pnpm install
```

### 3. Configure Your App

Update the following files in your app:
- `apps/your-app-name/nuxt.config.ts` - Configure API endpoints
- `apps/your-app-name/package.json` - Update app name
- Create your domain-specific pages in `apps/your-app-name/pages/`
- Create your domain-specific data files in `layers/shared/data/`

### 4. Customize for Your Domain

For a punch clock application, you might create:
- `pages/time-entries/` - Time tracking pages
- `pages/employees/` - Employee management
- `data/time-tracking-endpoints.ts` - API endpoints for punch clock features
- `components/TimeCard.vue` - Domain-specific components

## Key Features

- ✅ Complete authentication system with JWT
- ✅ Form generation and validation system
- ✅ Responsive layouts with sidebar navigation
- ✅ Loading states and notifications
- ✅ Theme management
- ✅ Export functionality
- ✅ List view with filtering and pagination
- ✅ Modal and dialog components
- ✅ Breadcrumb navigation

## Architecture

This follows a Nuxt 3 layer architecture:
- **Shared Layer**: Contains all reusable components, stores, and utilities
- **App Layer**: Extends the shared layer with app-specific configuration and pages

Each new app can extend the shared layer and add its own domain-specific logic.

## Notes

- All components use Vue 3 Composition API
- State management with Pinia
- Form validation with Vee-Validate
- Styling with Tailwind CSS
- Type-safe with TypeScript

## License

Use this template for your projects. Customize as needed.
