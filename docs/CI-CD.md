# CI/CD

The project can use GitHub Actions for CI and CD.

## CI goals

CI should verify that the application builds successfully.

Recommended CI jobs:

```text
backend build
frontend build
Docker Compose build
```

The CI workflow should use least privileges:

```yaml
permissions:
  contents: read
```

## CD goals

CD can build and publish Docker images to GitHub Container Registry.

Recommended images:

```text
ghcr.io/<owner>/efx-simulator-backend:latest
ghcr.io/<owner>/efx-simulator-frontend:latest
```

Also publish commit-specific tags:

```text
ghcr.io/<owner>/efx-simulator-backend:<sha>
ghcr.io/<owner>/efx-simulator-frontend:<sha>
```

The CD workflow needs:

```yaml
permissions:
  contents: read
  packages: write
```

## Lowercase GHCR image owner

GHCR image names must be lowercase. A safe way to handle this in GitHub Actions is to use a step output:

```yaml
- name: Set lowercase image owner
  id: image_owner
  shell: bash
  run: |
    echo "owner=${GITHUB_REPOSITORY_OWNER,,}" >> "$GITHUB_OUTPUT"
```

Then reference it:

```yaml
${{ steps.image_owner.outputs.owner }}
```

## Production compose file

A production-style compose file can reference published images rather than building locally.

Example:

```yaml
services:
  frontend:
    image: ghcr.io/<owner>/efx-simulator-frontend:latest
    ports:
      - "3000:8080"
    depends_on:
      - backend

  backend:
    image: ghcr.io/<owner>/efx-simulator-backend:latest
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_URLS: "http://+:8080"
      Redis__ConnectionString: "redis:6379"
    depends_on:
      - redis

  redis:
    image: redis:7
```
