---
name: swarmpit-mcp
description: Operate Docker Swarm clusters through swarmpit MCP servers. Use this skill whenever a task touches a swarm through these tools in any way — deploying, updating, scaling, stopping, or deleting a stack or service; reading service logs, task state, or node health; managing configs, secrets, networks, volumes, or swarmpit users; checking what's running on a swarm; or debugging a failed deploy. Trigger on any mention of swarmpit, "the swarm", a staging/production swarm managed by swarmpit, or stack/service names that live there — even if the user doesn't say "MCP". The tools have sharp edges (400s on service updates, oversized list outputs, redaction round-trips) that this skill documents from live testing.
---

# Swarmpit MCP

Each configured swarmpit instance appears as its own MCP server; tool names follow `mcp__<server>__<tool>` and every server exposes the same tool set. Tool schemas are usually deferred — load them with ToolSearch (`select:mcp__<server>__get_stack,...`) before calling.

When multiple environments are configured (staging, production, …), default to the non-production one for anything experimental, and only touch production when the user explicitly means production. `swarmpit_info` returns the instance URL and redaction mode — call it when there's any doubt which swarm a server points at. On a shared swarm, name anything you create so ownership is obvious (`claude-<purpose>`), and always clean up.

## The one rule that saves the most pain

**Mutate stacks through `update_stack` with a full compose file. Do not reach for the service-level mutators first.**

`update_service`, `update_service_env`, and `scale_service` work by GET → modify → PUT of the service spec through swarmpit's request coercion. That round-trip 400s on any service with mounts, because the GET output contains `volumeOptions` shapes the update spec rejects (`{labels: null, driver: {name: null, options: null}}`). Swarmpit 2.4.3 fixed the bind-mount case (`volumeOptions: null`) but volume mounts without options still fail on current releases. Most real services have mounts, so treat a `reitit.coercion/request-coercion` 400 from these tools as expected, not as your bug — switch to `update_stack`.

The POST-style actions bypass the spec entirely and work regardless of mounts: `redeploy_service` (optionally with a new `tag`), `rollback_service`, `stop_service`, `redeploy_stack`, `rollback_stack`, `deactivate_stack`.

So: change env/replicas/ports/mounts → edit the compose and `update_stack`. Force a re-pull → `redeploy_service`. Emergency stop → `stop_service`. Read a single env value → `get_service_env` (read-only and safe).

## Stack lifecycle semantics

- `create_stack` / `update_stack` run `docker stack deploy` server-side. A small stack converges in seconds; verify with `get_stack_tasks`, not by assuming.
- The **stored stackfile is the source of truth**. `get_stack` returns it verbatim in `.compose`. `update_stack` overwrites it and redeploys.
- `get_stack_compose` is different: it returns compose **regenerated from live docker state**. It drifts from what you submitted — `version:` dropped, `deploy.replicas` omitted when default, `isolation: default` and `logging:` injected, secrets/configs gain `uid/gid/mode`, external networks renamed to their real names. Use it to inspect reality or to reconstruct a lost stackfile; never diff it against your input expecting a match.
- `deactivate_stack` is `docker stack rm`: services and the stack's default network are **removed entirely** (the tasks list goes empty), but the stackfile survives in CouchDB. Revive the stack by calling `update_stack` with the desired compose.
- `delete_stack` removes services **and** the stackfile, but **not** external resources (configs, secrets, non-stack networks, volumes) — delete those individually afterward.
- `get_stack` on a nonexistent stack does **not** 404 — it returns `{services: [], compose: ""}`. Check the `services` array, don't rely on an error.
- `create_stack_file` / `delete_stack_file` manage only the stored compose, no deployment.

## Reading without drowning

On a swarm of any size (say 30+ stacks / 100+ services), `list_stacks`, `list_services`, `list_tasks`, `get_nodes_timeseries`, and the all-services CPU/memory timeseries return hundreds of KB of JSON — past the tool-result limit, so the harness dumps them to a file instead of returning them. Don't fight it: parse the dump (`Get-Content $f -Raw | ConvertFrom-Json` in PowerShell, or `jq`) to extract names/ports/whatever, or skip the list entirely and use targeted tools (`get_stack`, `get_service`, `get_stack_tasks`) when you already know the name. Finding a free published port = extract every `ports[].hostPort` from the list_services dump.

Task lists (`get_stack_tasks`, `list_service_tasks`) include **shutdown task history** from prior deploys — filter on `state == "running"` before counting replicas. Fresh tasks show `stats: null` until the next stats-collection pass (~1 min); it's lag, not a problem. `get_task` takes a task ID; `get_task_timeseries` takes a task *name* (`stack_service.N`) and returns per-minute cpu%/memory-MB (requires the swarmpit InfluxDB to be running). `service_logs` takes `since` as a Go duration (`"30s"`, `"5m"`, `"24h"`, default 5m).

## Secrets, configs, and redaction

Check `swarmpit_info` for the server's redaction mode. In `sensitive` mode, env values whose **names** look secret (`*PASSWORD*`, `*TOKEN*`, `*KEY*`, …) come back as `[REDACTED]` in compose/stackfile output. Two things follow:

- **Round-trip is safe**: send `[REDACTED]` back through `update_stack` and the stored real value is preserved while your other edits apply. Never guess or blank a redacted value — leave the literal `[REDACTED]` token in place.
- **Reading one value** when you legitimately need it: `get_service_env` with explicit names returns real values (missing names return null). That's the intentional escape hatch — use it narrowly.

For values you're introducing, don't paste secrets into compose or tool args: `create_secret`, `create_config`, `update_service_env`, and the compose params all accept `{ "$env": "VAR" }` and `{ "$file": "/path" }` so the value never transits the conversation.

**Never put sensitive data in a docker config.** `get_stack_configs` returns config contents as plain base64 (`get_config` redacts it, the stack listing currently doesn't), and configs are readable to anyone with swarmpit access. Secrets never return their data through any tool — that's the storage for anything sensitive.

## Resources (config / secret / network / volume)

Create standalone, then attach by marking them `external: true` in the stack compose. All are unaffected by stack deletion; each `delete_*` needs `confirm: true`. Deletion fails while a service still references the resource — detach first via `update_stack`.

Volumes are `scope: local` (per-node): swarm auto-creates a copy on every node a task lands on, and `delete_volume` only removes the copy on the node swarmpit talks to. Copies on other nodes linger; they're empty and harmless for test volumes but don't expect cluster-wide cleanup.

## Users and dashboards

Swarmpit users live in CouchDB. `get_user`, `edit_user`, `delete_user` take the couch `_id`, **not** the username — `get_user "some-name"` 404s. Find the `_id` via `list_users` (which also exposes emails and masked API-token metadata — treat its output as sensitive). `pin_service_to_dashboard` / `unpin_service_from_dashboard` write to the *invoking user's* `service-dashboard` list — pins are per-user, and they take the service name.

## Destructive operations

Every `delete_*` requires `confirm: true`. `deactivate_stack`, `stop_service`, `delete_node`, and `edit_node` don't ask for confirmation — on a shared swarm, be certain of the target name before calling (a typo'd stack name that matches something real is the nightmare case; there is no dry-run). Never call `delete_node`/`edit_node` casually — that's swarm topology, not app management.

## Known error signatures

- `400 reitit.coercion/request-coercion` mentioning `volumeOptions` → the mounts round-trip bug above; use `update_stack` instead.
- `404 user doesn't exist` from `get_user` → you passed a username; look up the `_id`.
- Oversized-result error with a saved file path → expected for `list_*`/timeseries on big swarms; parse the file.
- Something you changed "isn't showing" → swarmpit memoizes reads (1s for services/tasks, 10s for disk usage); re-read after a beat before assuming failure.
