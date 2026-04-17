[![swarmpit](https://raw.githubusercontent.com/swarmpit/swarmpit/master/resources/public/img/logo.svg?sanitize=true)](https://swarmpit.io)

Lightweight mobile-friendly & AI-friendly Docker Swarm management UI

## About this fork

This is a maintained fork of [swarmpit/swarmpit](https://github.com/swarmpit/swarmpit) by [ChangemakerStudios](https://github.com/ChangemakerStudios). The upstream project is in maintenance mode, so this fork provides bug fixes and improvements including:

- Fix image tag loss on service edit
- Silence agent `/logs/` 404 spam
- Dark mode support
- Modernized Docker Compose spec with skip image resolution toggle

Swarmpit provides simple and easy to use interface for your Docker Swarm cluster. You can manage your stacks, services, secrets, volumes, networks etc. After linking your Docker Hub account or custom registry, private repositories can be easily deployed on Swarm. Best of all, you can share this management console securely with your whole team.

Everything the UI does is also exposed via a REST API (Swagger docs at `/api-docs` on any running instance) and, for LLM-driven workflows, an [MCP server](https://github.com/swarmpit/mcp) — so you can automate deployments or drive Swarmpit from any MCP-compatible client (Claude Code, opencode, etc.).

Swarmpit doesn't compromise your privacy as it is completely self-hosted and will never gather any metrics or other data from you.

## Docker Images

This fork publishes multi-architecture images (amd64, arm64, armv7, armv5):

```
ghcr.io/changemakerstudios/swarmpit:latest
```

### Quick start

```yaml
services:
  app:
    image: ghcr.io/changemakerstudios/swarmpit:latest  # or pin to a specific tag
    environment:
      - SWARMPIT_DB=http://db:5984
      - SWARMPIT_INFLUXDB=http://influxdb:8086
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    ports:
      - 888:8080
```

Or use the full [docker-compose.yml](docker-compose.yml) included in this repo:

```bash
docker stack deploy -c docker-compose.yml swarmpit
```

## Installation

The only dependency for Swarmpit deployment is Docker with Swarm initialized. Docker 1.13 and newer is supported. Linux hosts on x86 and ARM architectures are supported.

[This stack](docker-compose.yml) is a composition of 4 services:

* app - Swarmpit
* [agent](https://github.com/swarmpit/agent) - Swarmpit agent
* db - CouchDB (Application data)
* influxdb - InfluxDB (Cluster statistics)

We strongly recommend specifying the following volumes with a shared-volume driver type of your choice:

* db-data
* influxdb-data

Alternatively, you can link the db service to a specific node by using [constraint](https://docs.docker.com/compose/compose-file/#placement).

Swarmpit is published on port `888` by default.

## Environment Variables

Refer to the following [document](https://github.com/swarmpit/swarmpit/blob/master/doc/configuration.md)

## MCP Server

Manage Swarmpit from any MCP-compatible client (Claude Code, opencode, etc.) via [swarmpit/mcp](https://github.com/swarmpit/mcp). The server runs locally and holds API tokens — they never enter the LLM conversation context.

Generate a token in Swarmpit UI: **Profile → API Access → Generate token**, then add to your MCP client config:

```json
{
  "mcpServers": {
    "swarmpit": {
      "command": "npx",
      "args": ["github:swarmpit/mcp"],
      "env": {
        "SWARMPIT_URL": "https://swarmpit.example.com",
        "SWARMPIT_TOKEN": "your-api-token",
        "SWARMPIT_REDACT": "sensitive"
      }
    }
  }
}
```

See the [mcp repo](https://github.com/swarmpit/mcp) for the full tool list, redaction modes, and multi-instance setup.

## User Configuration

By default Swarmpit offers you to configure the first user using the web interface. If you want to automate this process, you can use docker config to provide a users.yaml file.

Refer to the following [document](https://github.com/swarmpit/swarmpit/blob/master/doc/USER_CONFIG.md) for details.

## Development

Swarmpit is written purely in Clojure and utilizes React on the front-end. CouchDB is used to persist application data & InfluxDB for cluster statistics.

Everything about building, issue reporting and setting up the development environment can be found in [CONTRIBUTING.md](CONTRIBUTING.md)
