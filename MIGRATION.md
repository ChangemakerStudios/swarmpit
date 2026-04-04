# Migration Plan: Swarmpit — Clojure/ClojureScript to .NET C# + React/MUI

## Context

Swarmpit is a lightweight Docker Swarm management UI currently built with Clojure (backend) and ClojureScript/Rum/Material-UI (frontend). The goal is to rewrite it in .NET C# (backend) and React with Material-UI (frontend).

**Why React instead of Angular:** The current frontend already IS React — Rum is just a thin ClojureScript wrapper around React, and it already uses Material-UI + Plotly + Recharts. Staying on React means:
- Same component library (MUI 4 → MUI 5/6, minor API changes)
- Same charting libraries (Plotly.js, Recharts) — zero porting
- Component structure maps 1:1 (Rum defc → React functional components)
- State model (atoms + cursors) maps cleanly to hooks (useState/useReducer) or Zustand
- ~2-3 weeks less effort than switching to Angular

**Current codebase size:**
- Backend: 45 Clojure files, ~6,300 LOC
- Frontend: 130 ClojureScript files, ~18,000 LOC
- Shared: 10 cljc files, ~1,930 LOC
- Java helpers: 2 files, ~350 LOC (Unix socket support)
- Tests: 12 test files

**This is doable.** The codebase is well-structured and moderate in size. The Clojure code is concise, so the C#/React equivalent will be larger in LOC but architecturally straightforward. No exotic Clojure features (macros, protocols used minimally) block a clean port.

---

## Recommended Tech Stack

### Backend (.NET 10+)
| Concern | Current (Clojure) | Proposed (.NET) |
|---|---|---|
| Web framework | Ring/httpkit/Reitit | ASP.NET Core Web API (controllers) |
| Auth | Buddy (JWT/JWS) | ASP.NET Core Identity + JwtBearer |
| Docker API | Custom HTTP client over Unix socket | **Docker.DotNet** (official, supports Unix sockets natively) |
| CouchDB | Custom HTTP client | **CouchDB.NET** (MyCouch) or switch to **LiteDB/MongoDB** |
| InfluxDB | influxdb-clj | **InfluxDB.Client** (official .NET client) |
| HTTP client | clj-http | HttpClient / IHttpClientFactory |
| SSE | core.async + httpkit | ASP.NET Core SSE via `IAsyncEnumerable` or SignalR |
| Scheduling | Chime | **Hangfire** or `IHostedService` + `PeriodicTimer` |
| YAML | clj-yaml | **YamlDotNet** |
| Logging | Timbre | Serilog |
| Config | Environ | ASP.NET Core Configuration (appsettings + env vars) |

**Key win: Docker.DotNet** handles Unix socket communication out of the box — no need to port the Java `HttpUnixSocket` / `UnixSocketFactory` helpers.

**CouchDB decision:** CouchDB works fine from .NET via MyCouch. However, if the team prefers, switching to **MongoDB** or even **LiteDB** (embedded) would simplify deployment. The CouchDB usage is straightforward document CRUD — no views or map/reduce that would be hard to migrate.

### Frontend (React 18+ / TypeScript)
| Concern | Current (ClojureScript) | Proposed (React/TS) |
|---|---|---|
| UI framework | Material-UI 4 (via Rum) | **MUI 5/6** (same library, upgraded) |
| State management | Rum atom + cursors | **Zustand** (similar mental model to atoms) or React Context + useReducer |
| Routing | Reitit (fragment-based) | **React Router v6** (history mode) |
| HTTP | cljs-http | **Axios** or fetch + TanStack Query |
| SSE | EventSource + custom handler | `useEffect` + `EventSource` (trivial) |
| Charts (time-series) | Plotly.js | **Plotly.js** (same library, keep as-is) |
| Charts (pie) | Recharts | **Recharts** (same library, keep as-is) |
| Code editor | CodeMirror 5 | **@uiw/react-codemirror** (CodeMirror 6) |
| Forms | Atom mutations | **React Hook Form** + MUI integration |
| Theming | Custom light/dark toggle | MUI `ThemeProvider` (same system, upgraded) |
| Build | Figwheel + Lein cljsbuild | **Vite** |

---

## Project Structure

### Backend
```
src/dotnet/
  Swarmpit.Api/                    # ASP.NET Core Web API project
    Controllers/                   # REST controllers (map from handler.clj)
      ServicesController.cs
      NodesController.cs
      NetworksController.cs
      VolumesController.cs
      SecretsController.cs
      ConfigsController.cs
      StacksController.cs
      TasksController.cs
      RegistriesController.cs
      UsersController.cs
      AuthController.cs
      StatsController.cs
      EventsController.cs
    Services/                      # Business logic (map from api.clj)
      DockerSwarmService.cs
      RegistryService.cs
      StackService.cs
      StatsService.cs
      UserService.cs
    Docker/                        # Docker API integration
      DockerClientFactory.cs
      Mappers/
        InboundMapper.cs           # Docker API → domain models
        OutboundMapper.cs          # Domain → Docker API
        ComposeMapper.cs           # Domain → docker-compose YAML
    Data/                          # Database layer
      CouchDb/
        CouchDbClient.cs
        CouchDbMigration.cs
      InfluxDb/
        InfluxDbClient.cs
    Auth/                          # Authentication/authorization
      JwtService.cs
      AuthorizationPolicies.cs
    Events/                        # SSE system
      EventChannel.cs
      EventProcessor.cs
    Registry/                      # Registry integrations
      DockerHubClient.cs
      V2RegistryClient.cs
      EcrClient.cs
      AcrClient.cs
      GitLabClient.cs
      GhcrClient.cs
    Models/                        # Domain models
    Background/                    # Background services
      AutoRedeployService.cs
    Program.cs
  Swarmpit.Api.Tests/              # xUnit tests
```

### Frontend (React + TypeScript + Vite)
```
src/react/src/
  api/                             # Axios clients
    docker.ts
    auth.ts
    registries.ts
    stats.ts
  hooks/                           # Custom hooks
    useEventSource.ts              # SSE hook
    useAuth.ts
  store/                           # Zustand stores (mirrors atom structure)
    formStore.ts
    layoutStore.ts
    authStore.ts
  pages/                           # Route-level components (mirrors current view.cljs)
    Dashboard/
    Services/                      # Largest — ~20 components
      ServiceList.tsx
      ServiceDetail.tsx
      ServiceCreate.tsx
      ServiceEdit.tsx
      forms/                       # 13 sub-form components (same as current)
        SettingsForm.tsx
        PortsForm.tsx
        NetworksForm.tsx
        MountsForm.tsx
        SecretsForm.tsx
        ConfigsForm.tsx
        HostsForm.tsx
        VariablesForm.tsx
        LabelsForm.tsx
        LogDriverForm.tsx
        ResourcesForm.tsx
        DeploymentForm.tsx
        PlacementForm.tsx
    Stacks/
    Nodes/
    Networks/
    Volumes/
    Secrets/
    Configs/
    Tasks/
    Users/
    Registries/                    # 6 registry type variants
      V2/
      DockerHub/
      Ecr/
      Acr/
      GitLab/
      Ghcr/
    Login.tsx
    Error.tsx
    NotFound.tsx
  components/                      # Shared UI (mirrors material/ wrappers)
    DataTable.tsx                   # Responsive list/table (mirrors common.cljs)
    StatusLabel.tsx
    ConfirmDialog.tsx
    CodeEditor.tsx
    PlotChart.tsx
  layout/
    AppLayout.tsx                  # Header + sidebar + content
    Header.tsx
    Sidebar.tsx
  theme/
    theme.ts                       # MUI theme (light + dark)
  utils/                           # Port of cljc shared code
    time.ts
    docker.ts
    format.ts
  App.tsx
  main.tsx
  router.tsx                       # React Router config
```

---

## Development Setup

```bash
# Dev ports
# API:    http://localhost:8500
# Client: http://localhost:8501

# Prerequisites: CouchDB + Docker Swarm
docker run -d --name swarmpit-db -p 5984:5984 couchdb:2.3.0
docker swarm init  # if not already in swarm mode

# Terminal 1: .NET API
cd src/dotnet/Swarmpit.Api
dotnet run

# Terminal 2: React dev server
cd src/react
npm run dev

# Initialize admin user (first time only)
curl -X POST http://localhost:8500/initialize \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

---

## Migration Phases

### Phase 1: Foundation (Week 1-2) ✅ COMPLETE
**Goal:** Skeleton app with auth, Docker connection, and one working page.

- [x] Create ASP.NET Core project with Docker.DotNet integration
- [x] Implement JWT auth (login, token generation)
- [x] Set up CouchDB client (user storage)
- [x] Scaffold React app with Vite + TypeScript + MUI 5 + React Router
- [x] Set up Zustand store, Axios client, auth interceptor
- [x] Implement login page + protected route wrapper
- [x] Port one simple CRUD domain end-to-end: **Nodes** (list + detail)
- [x] Dockerfile for .NET backend

**Validates:** Full stack works, Docker API accessible, auth flows.

### Phase 2: Core Docker Resources (Week 3-4)
**Goal:** All read-only Docker resource views working.

- [ ] Port Docker engine mappers (inbound/outbound) — this is the bulk of backend work
- [ ] Controllers + services for: Services, Tasks, Networks, Volumes, Secrets, Configs
- [ ] Shared `<DataTable>` component with filtering and responsive layout (port of common.cljs)
- [ ] React list + detail pages for each resource type
- [ ] Port shared utilities (time.ts, docker.ts, format.ts from cljc files)

### Phase 3: Service Management (Week 5-6)
**Goal:** Full service CRUD — the most complex feature.

- [ ] Service create/edit with all 13 form sections (React Hook Form + MUI)
- [ ] Service redeploy, rollback, stop, delete
- [ ] Service log viewer
- [ ] Compose file generation
- [ ] Port outbound mapper for service spec construction

### Phase 4: Stacks & Registries (Week 7-8)
**Goal:** Stack deployment and all 6 registry integrations.

- [ ] Stack CRUD with CodeMirror 6 editor (@uiw/react-codemirror)
- [ ] Stack deploy/rollback via Docker CLI
- [ ] All 6 registry types: v2, Docker Hub, ECR, ACR, GitLab, GHCR
- [ ] Repository browsing and tag fetching
- [ ] AWS SDK integration for ECR

### Phase 5: Real-time & Metrics (Week 8-9)
**Goal:** Live updates and dashboard.

- [ ] SSE: ASP.NET `IAsyncEnumerable` endpoint + React `useEventSource` hook
- [ ] InfluxDB integration for time-series storage
- [ ] Stats collection and caching
- [ ] Dashboard with Plotly.js + Recharts (same libs, just TSX wrappers)
- [ ] Auto-redeploy background service

### Phase 6: Users, Polish & Testing (Week 9-10)
**Goal:** Feature parity, testing, deployment.

- [ ] User management (CRUD, roles, API tokens)
- [ ] Dashboard pinning
- [ ] MUI dark/light theme (ThemeProvider — close to current implementation)
- [ ] Error handling, 404/403 pages
- [ ] Tests: xUnit (backend) + Vitest + React Testing Library (frontend)
- [ ] Update Docker Compose files
- [ ] Update CI/CD pipeline (GitHub Actions)
- [ ] Multi-arch Docker build

---

## Effort Estimate

| Phase | Effort | Notes |
|---|---|---|
| 1. Foundation | 2 weeks | Skeleton + auth + one page |
| 2. Core resources | 2 weeks | 6 resource types, mappers (faster — React not Angular) |
| 3. Service management | 2 weeks | Complex forms, but React Hook Form handles it well |
| 4. Stacks & registries | 2 weeks | 6 registry types |
| 5. Real-time & metrics | 1 week | SSE trivial in React; same chart libs |
| 6. Polish & testing | 1-2 weeks | MUI theming nearly identical to current |
| **Total** | **10-12 weeks** | **1 experienced full-stack dev** |

With 2 developers working in parallel (one backend, one frontend), this could compress to **6-8 weeks**.

**Compared to Angular:** Saves ~2-3 weeks by keeping the same UI framework (MUI), chart libraries (Plotly + Recharts), and component patterns.

---

## Key Risks & Mitigations

1. **Docker.DotNet API coverage** — Verify it supports all Swarm endpoints used (services, tasks, nodes, secrets, configs). Fallback: raw HttpClient to Docker socket.

2. **CouchDB .NET client maturity** — MyCouch is maintained but less popular. Mitigation: The CouchDB usage is simple HTTP CRUD; a thin custom client would work too. Or migrate to MongoDB (well-supported in .NET).

3. **Service form complexity** — 13 nested sub-forms with cross-field validation. Mitigation: React Hook Form handles nested/dynamic forms well; the current Rum form pattern maps almost directly.

4. **SSE in ASP.NET** — Less idiomatic than WebSockets. Mitigation: Use `IAsyncEnumerable<T>` with `text/event-stream` content type, or switch to **SignalR** (WebSocket-based). React SSE is trivial — just an `EventSource` in `useEffect`.

5. **Agent compatibility** — The agent is a separate service (not being rewritten). The .NET backend just needs to call the agent's REST API — straightforward.

---

## Incremental vs Big-Bang

**Big-bang rewrite is recommended** for this project because:
- The Clojure backend and ClojureScript frontend are tightly coupled through shared route definitions (routes.cljc)
- The atom-based state model doesn't map to an incremental migration pattern
- The codebase is small enough (~26K LOC total) that a full rewrite is manageable
- Running two backends in parallel adds operational complexity that isn't worth it

However, you can **ship incrementally within the rewrite** by following the phases above — each phase produces a working (partial) application.

---

## Verification Plan

1. **Unit tests:** Port existing 12 test files to xUnit; add React component tests (Vitest + React Testing Library)
2. **Integration tests:** Docker API tests against a real Docker daemon (same as current `:integration` tag)
3. **Manual smoke test:** Deploy the new stack alongside the old one, compare behavior for:
   - Login/auth flow
   - Service CRUD (create, edit, redeploy, rollback)
   - Stack deploy from compose file
   - Registry add/browse for each type
   - Real-time updates (SSE)
   - Dashboard charts
   - Node/network/volume/secret/config CRUD
4. **Multi-arch build:** Verify Docker image builds for amd64, arm64, arm/v7

---

## Future Iteration: shadcn/ui + Tailwind

After the initial migration is complete, there's an option to swap MUI for **shadcn/ui + Tailwind CSS**. This would be a separate effort (~2-3 weeks) that gives a lighter, more customizable UI. Not a priority for the initial migration.
