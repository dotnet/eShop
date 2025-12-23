/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_IDENTITY_URL: string
  readonly VITE_WAREHOUSE_API_URL: string
  readonly VITE_CATALOG_API_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
