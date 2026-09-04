// Default de desarrollo local -- coincide con el perfil `http` de
// src/Api/Properties/launchSettings.json. En dev/staging/prod, cd-dev.yml
// sobreescribe este archivo con el FQDN real del Container App justo
// antes de `npm run build` (el FQDN no existe hasta el `terraform apply`
// de ese mismo run -- ver Design Notes de spec 1.5).
export const environment = {
  apiBaseUrl: 'http://localhost:5075',
};
