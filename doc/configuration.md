# Configuration

Swarmpits behavior can be reconfigured via Environment Variables.

## `SWARMPIT_DOCKER_SOCK`
Docker SOCK location used by docker client. In case of socket proxy use following format: `http://TCPENDPOINT:2375`.
Default is `/var/run/docker.sock`.

## `SWARMPIT_DOCKER_API`
Docker API version used by docker client.
Default is `1.44`. Docker Engine 29.0+ requires at least API `1.44`.

## `SWARMPIT_DOCKER_HTTP_TIMEOUT`
Docker client http timeout in ms.
Default is `5000`.

## `SWARMPIT_DB`
Swarmpit database (CouchDB) url. 
Default is `http://localhost:5984`.

## `SWARMPIT_INFLUXDB`
Swarmpit statistics database (InfluxDB) url. If `nil` statistics are disabled. 
Default is `nil`.

## `SWARMPIT_AGENT_URL`
Set address of agent. If `nil`, value is calculated dynamically. For DEV purpose only!!! 
Default is `nil`.

## `SWARMPIT_WORK_DIR`
Swarmpit working directory location.
Default is `/tmp`.

## `SWARMPIT_API_TOKEN_EXPIRY_DAYS`
Lifetime in days for personal API tokens generated in the UI. `nil` (the default) keeps the legacy behaviour where API tokens never expire. Set to a positive integer (e.g. `90`) to force rotation.
Default is `nil`.

## `SWARMPIT_EVENT_TOKEN`
Shared secret the agent must present on `POST /events`, the endpoint it pushes node stats and docker events to.

The agent ships no credentials, so when this is `nil` the endpoint accepts unauthenticated pushes and the stock agent works with no extra configuration. Requiring a login token here instead would silently stop all stats collection.

Set it to any random string to lock the endpoint down. The agent cannot send headers, but its `EVENT_ENDPOINT` is a full URL, so pass the secret as a query parameter and keep the two in sync:

```yaml
app:
  environment:
    - SWARMPIT_EVENT_TOKEN=<your-secret>
agent:
  environment:
    - EVENT_ENDPOINT=http://app:8080/events?token=<your-secret>
```

Logged-in users are always allowed through, so the UI is unaffected either way.
Default is `nil` (endpoint open, matching the agent's out-of-the-box behaviour).

## `SWARMPIT_INSTANCE_NAME`
Custom name shown in place of the swarmpit logo in the sidebar and top bar, and prepended to the browser tab title as `{instance_name} :: {page} :: swarmpit`. Useful when running multiple swarmpit instances against different clusters so you can tell them apart at a glance.
Default is `nil` (shows the swarmpit logo).
