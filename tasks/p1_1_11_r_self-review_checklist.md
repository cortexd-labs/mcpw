# Task: `[R]` Self-review checklist

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.11 `container.*` (8 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` Self-review checklist

## Tool Specifications

### Feature: container.*
## 11. `container.*` — Container Management

### Test Spec: container.*
## 11. `container.*`

### Feature: container.* — Container Management
## 11. `container.*` — Container Management

### Feature: container.exec
### `container.exec` 🟡 Operate

Execute a command inside a running container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `command` | array | Command + arguments (e.g., ["ls", "-la"]) |
| `user` | string (optional) | User to run as |
| `working_dir` | string (optional) | Working directory inside container |
| `env` | array (optional) | Additional environment variables ["KEY=VALUE"] |

**Response:**
| Field | Type | Description |
|---|---|---|
| `exit_code` | integer | Command exit code |
| `stdout` | string | Standard output |
| `stderr` | string | Standard error |

**Implementation:** Docker API `POST /containers/{id}/exec` + `POST /exec/{id}/start`

---

### Test Spec: container.exec
### `container.exec`

**Happy Path:**

- 🎭 Executes simple command → returns stdout
- 🎭 Failed command → returns non-zero `exit_code` and stderr
- 🎭 `user` parameter runs as specified user
- 🎭 `working_dir` changes execution directory
- 🎭 `env` injects additional environment variables

**Input Validation:**

- ✅ Empty `command` array → error
- ✅ Non-existent container → error

**Error Handling:**

- ✅ Container not running → error "Container is not running"
- ✅ Command not found in container → non-zero exit code with stderr
- ✅ Command timeout → error (configurable)

**Security:**

- 🔒 Requires Operate tier
- 🔒 Command and output logged in audit trail
- 🔒 Cannot execute interactive/TTY sessions

**Edge Cases:**

- ⚡ Command with special characters in arguments
- ⚡ Very large stdout output → truncated at configurable limit
- ⚡ Long-running command → completes or times out

---

### Feature: container.images
### `container.images` 🟢 Read

List Docker images.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter_name` | string (optional) | Filter by repository name |
| `dangling` | boolean (optional) | Only show dangling images. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `images` | array | `[{id, repository, tag, created, size_mb, containers}]` |
| `count` | integer | Total images |

**Implementation:** Docker API `GET /images/json`

---

### Test Spec: container.images
### `container.images`

**Happy Path:**

- ✅ Returns list of images
- ✅ Each image has `id`, `repository`, `tag`, `size_mb`
- ✅ `filter_name` filters by repository
- ✅ `dangling: true` only shows untagged images

**Error Handling:**

- ✅ Docker not running → error
- ✅ No images → empty array

---

### Feature: container.inspect
### `container.inspect` 🟢 Read

Detailed container information.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |

**Response:** All fields from container object plus:
| Field | Type | Description |
|---|---|---|
| `full_id` | string | Full container ID |
| `command` | string | Entrypoint + command |
| `environment` | array | Environment variables |
| `restart_policy` | object | `{name, max_retries}` |
| `resource_limits` | object | `{cpu_shares, memory_limit_bytes, memory_reservation_bytes}` |
| `health_check` | object / null | `{test, interval, timeout, retries, status}` |
| `ip_address` | string | Container IP |
| `mac_address` | string | Container MAC |
| `log_driver` | string | Log driver name |
| `platform` | string | "linux" / "windows" |

**Implementation:** Docker API `GET /containers/{id}/json`

---

### Test Spec: container.inspect
### `container.inspect`

**Happy Path:**

- ✅ Returns full details for valid container ID
- ✅ Returns full details for valid container name
- ✅ `environment` array lists all env vars
- ✅ `health_check` populated for container with health check
- ✅ `platform` shows "linux" or "windows"

**Error Handling:**

- ✅ Invalid container ID → error "Container not found"
- ✅ Partial container ID → matches if unique
- ✅ Ambiguous partial ID → error "Multiple containers match"

**Edge Cases:**

- ⚡ Container with no health check → `health_check: null`
- ⚡ Container with restart policy → shown in `restart_policy`
- ⚡ Container with resource limits → `resource_limits` populated

---

### Feature: container.list
### `container.list` 🟢 Read

List Docker containers.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `all` | boolean (optional) | Include stopped containers. Default: true |
| `filter_name` | string (optional) | Filter by name (contains) |
| `filter_image` | string (optional) | Filter by image name |
| `filter_status` | string (optional) | "running" / "exited" / "paused" / "created" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `containers` | array | Container objects |
| `count` | integer | Total count |

**Container object:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID (short) |
| `name` | string | Container name |
| `image` | string | Image name:tag |
| `status` | string | Human-readable status |
| `state` | string | "running" / "exited" / "paused" / "created" / "restarting" / "dead" |
| `created` | string | ISO 8601 creation time |
| `ports` | array | `[{host_port, container_port, protocol, host_ip}]` |
| `networks` | array | Network names |
| `mounts` | array | `[{source, destination, type}]` |
| `labels` | object | Container labels |
| `exit_code` | integer / null | Exit code if exited |

**Implementation:** Docker Engine API via Windows named pipe `//./pipe/docker_engine`

---

### Test Spec: container.list
### `container.list`

**Happy Path:**

- ✅ Returns containers when Docker is running
- ✅ `all: false` only returns running containers
- ✅ `all: true` includes stopped containers
- ✅ `filter_name` matches by container name (contains)
- ✅ `filter_image` matches by image name
- ✅ `filter_status: "running"` returns only running
- ✅ Each container has `id`, `name`, `image`, `state`
- ✅ Port mappings are correctly listed
- ✅ `count` matches `containers.length`

**Error Handling:**

- ✅ Docker not installed → error "Docker not available"
- ✅ Docker daemon not running → error "Docker daemon not running"
- ✅ Docker named pipe not accessible → error "Cannot connect to Docker"

**Edge Cases:**

- ⚡ Container with no port mappings → empty `ports` array
- ⚡ Container with many labels → all returned
- ⚡ Container in "restarting" state
- ⚡ Windows container vs Linux container → both listed
- ⚡ `filter_name` and `filter_status` combined → AND logic

---

### Feature: container.logs
### `container.logs` 🟢 Read

Get container logs.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `tail` | integer (optional) | Last N lines. Default: 100 |
| `since` | string (optional) | ISO 8601 start time |
| `until` | string (optional) | ISO 8601 end time |
| `stdout` | boolean (optional) | Include stdout. Default: true |
| `stderr` | boolean (optional) | Include stderr. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `container` | string | Container ID |
| `logs` | string | Log output |
| `line_count` | integer | Lines returned |

**Implementation:** Docker API `GET /containers/{id}/logs`

---

### Test Spec: container.logs
### `container.logs`

**Happy Path:**

- ✅ Returns stdout logs for running container
- ✅ `tail: 10` returns last 10 lines
- ✅ `since` filters by time
- ✅ `stderr: false` excludes stderr
- ✅ `stdout: false` excludes stdout

**Error Handling:**

- ✅ Container not found → error
- ✅ Container with no logs (just started) → empty string

**Edge Cases:**

- ⚡ Very large logs (>100MB) → respects `tail` limit
- ⚡ Binary output in logs → handled without crash
- ⚡ `tail: 0` → empty or error

---

### Feature: container.restart
### `container.restart` 🟡 Operate

Restart a container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `timeout_seconds` | integer (optional) | Grace period. Default: 10 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID |
| `name` | string | Container name |
| `restarted` | boolean | Success |

**Implementation:** Docker API `POST /containers/{id}/restart`

---

### Feature: container.start
### `container.start` 🟡 Operate

Start a stopped container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID |
| `name` | string | Container name |
| `started` | boolean | Success |
| `state` | string | New state |

**Implementation:** Docker API `POST /containers/{id}/start`

---

### Test Spec: container.start
### `container.start` / `container.stop` / `container.restart`

**Happy Path:**

- 🎭 Start stopped container → `state: "running"`
- 🎭 Stop running container → `state: "exited"`
- 🎭 Restart running container → `state: "running"`, new uptime

**Error Handling:**

- ✅ Start already running → error "Container already running"
- ✅ Stop already stopped → error "Container is not running"
- ✅ Stop timeout → force kill after `timeout_seconds`
- ✅ Container not found → error

**Security:**

- 🔒 Requires Operate tier

---

### Feature: container.stop
### `container.stop` 🟡 Operate

Stop a running container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `timeout_seconds` | integer (optional) | Grace period before kill. Default: 10 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID |
| `name` | string | Container name |
| `stopped` | boolean | Success |
| `state` | string | New state |

**Implementation:** Docker API `POST /containers/{id}/stop`

---

