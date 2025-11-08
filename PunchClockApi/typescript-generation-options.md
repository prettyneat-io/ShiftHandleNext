# TypeScript API Client Generation Options

## 1. NSwag (Recommended for .NET)

### Install NSwag CLI globally:
```bash
dotnet tool install -g NSwag.ConsoleCore
```

### Or add to project:
```bash
dotnet add package NSwag.AspNetCore
dotnet add package NSwag.MSBuild
```

### Generate client with simple command:
```bash
# Generate from running API
nswag openapi2tsclient /input:https://localhost:5001/swagger/v1/swagger.json /output:./generated/api-client.ts /className:PunchClockApiClient

# Or generate from project
nswag aspnetcore2openapi /project:PunchClockApi.csproj /output:swagger.json
nswag openapi2tsclient /input:swagger.json /output:./generated/api-client.ts
```

## 2. OpenAPI Generator

### Install via npm:
```bash
npm install @openapitools/openapi-generator-cli -g
```

### Generate TypeScript client:
```bash
# From running API
openapi-generator-cli generate -i https://localhost:5001/swagger/v1/swagger.json -g typescript-axios -o ./generated

# With custom config
openapi-generator-cli generate \
  -i swagger.json \
  -g typescript-axios \
  -o ./generated/api-client \
  --additional-properties=npmName=punchclock-api-client,supportsES6=true,modelPropertyNaming=camelCase
```

## 3. Kiota (Microsoft's new tool)

### Install Kiota:
```bash
dotnet tool install -g Microsoft.OpenApi.Kiota
```

### Generate TypeScript client:
```bash
kiota generate -l typescript -d swagger.json -c PunchClockApiClient -o ./generated
```

## 4. TypeScript + tRPC (if you want to refactor)

If you're open to refactoring your API, tRPC provides end-to-end type safety:

```bash
npm install @trpc/server @trpc/client @trpc/react-query
npm install @trpc/next # if using Next.js
```

## Quick Setup with NSwag (Recommended)

Add this to your .csproj to auto-generate on build: