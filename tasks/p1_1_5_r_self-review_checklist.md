# Task: `[R]` Self-review checklist

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.5 `network.*` (10 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` Self-review checklist

## Tool Specifications

### Feature: network.*
## 5. `network.*` — Network Stack

### Test Spec: network.*
## 5. `network.*`

### Feature: network.* — Network Stack
## 5. `network.*` — Network Stack

### Feature: network.connections
### `network.connections` 🟢 Read

List active TCP/UDP connections.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `state` | string (optional) | Filter: "established" / "time_wait" / "close_wait" / "all". Default: "all" |
| `pid` | integer (optional) | Filter by process ID |
| `port` | integer (optional) | Filter by local or remote port |
| `limit` | integer (optional) | Max results. Default: 200 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `connections` | array | Connection objects |
| `count` | integer | Total matching |

**Connection object:**
| Field | Type | Description |
|---|---|---|
| `protocol` | string | "tcp" / "udp" |
| `local_address` | string | Local IP |
| `local_port` | integer | Local port |
| `remote_address` | string | Remote IP |
| `remote_port` | integer | Remote port |
| `state` | string | TCP state (e.g., "established", "time_wait") |
| `pid` | integer | Owning process ID |
| `process_name` | string | Process name |

**Implementation:** `IPGlobalProperties.GetActiveTcpConnections()` + `GetExtendedTcpTable` / `GetExtendedUdpTable`

---

### Test Spec: network.connections
### `network.connections`

**Happy Path:**

- ✅ Returns active connections
- ✅ `state: "established"` shows only ESTABLISHED
- ✅ `pid` filter returns only connections for that process
- ✅ `port` filter matches local or remote port
- ✅ Each connection has valid state string

**Edge Cases:**

- ⚡ `state: "time_wait"` may have thousands of entries → respects limit
- ⚡ Connection state changes during enumeration
- ⚡ UDP "connections" (stateless) have limited info
- ⚡ `limit: 1` returns 1 connection

---

### Feature: network.dns
### `network.dns` 🟢 Read

DNS configuration and resolution.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `resolve` | string (optional) | Hostname or IP to resolve |
| `type` | string (optional) | Record type: "A" / "AAAA" / "MX" / "CNAME" / "PTR". Default: "A" |

**Response (no resolve — config only):**
| Field | Type | Description |
|---|---|---|
| `hostname` | string | Machine hostname |
| `domain` | string | Primary DNS domain |
| `dns_servers` | array | Configured DNS servers (per interface) `[{interface, servers}]` |
| `search_suffixes` | array | DNS search suffix list |

**Response (with resolve):**
| Field | Type | Description |
|---|---|---|
| `query` | string | Queried name |
| `type` | string | Record type |
| `results` | array | Resolved records `[{address, ttl}]` or `[{exchange, priority}]` for MX |
| `elapsed_ms` | integer | Resolution time |

**Implementation:** `NetworkInterface.GetIPProperties().DnsAddresses` + `Dns.GetHostAddresses()` or PowerShell `Resolve-DnsName` for MX/CNAME

---

### Test Spec: network.dns
### `network.dns`

**Happy Path (config):**

- ✅ Returns `hostname` matching system hostname
- ✅ Returns `dns_servers` per interface
- ✅ Returns `search_suffixes` if configured

**Happy Path (resolve):**

- ✅ `resolve: "localhost"` returns 127.0.0.1
- ✅ `resolve: "127.0.0.1", type: "PTR"` returns localhost
- ✅ Resolving public domain returns valid IP
- ✅ `elapsed_ms` > 0

**Error Handling:**

- ✅ `resolve: "nonexistent.invalid"` → error "Name not resolved"
- ✅ DNS server unreachable → timeout error

**Security:**

- 🔒 Cannot resolve internal hostnames that would leak network topology (configurable)

**Edge Cases:**

- ⚡ CNAME chain resolution
- ⚡ Domain with many A records
- ⚡ IPv6 AAAA resolution
- ⚡ Very slow DNS response (near timeout)

---

### Feature: network.firewall
### `network.firewall` 🟢 Read

List Windows Firewall rules.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `profile` | string (optional) | "domain" / "private" / "public" / "all". Default: "all" |
| `direction` | string (optional) | "in" / "out" / "all". Default: "all" |
| `enabled_only` | boolean (optional) | Only show enabled rules. Default: true |
| `filter_name` | string (optional) | Filter by rule name (contains match) |

**Response:**
| Field | Type | Description |
|---|---|---|
| `rules` | array | Firewall rule objects |
| `count` | integer | Total matching |
| `firewall_enabled` | object | `{domain: bool, private: bool, public: bool}` |

**Firewall rule object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Rule name |
| `description` | string | Rule description |
| `enabled` | boolean | Whether rule is active |
| `direction` | string | "in" / "out" |
| `action` | string | "allow" / "block" |
| `protocol` | string | "tcp" / "udp" / "icmpv4" / "any" |
| `local_ports` | string | Port range or "any" |
| `remote_ports` | string | Port range or "any" |
| `local_addresses` | string | Address range or "any" |
| `remote_addresses` | string | Address range or "any" |
| `profiles` | array | ["domain", "private", "public"] |
| `program` | string / null | Application path |
| `service` | string / null | Service short name |
| `group` | string / null | Rule group |

**Implementation:** COM `INetFwPolicy2.Rules` enumeration

---

### Test Spec: network.firewall
### `network.firewall`

**Happy Path:**

- ✅ Returns `firewall_enabled` per profile
- ✅ Returns non-empty rules array (Windows has default rules)
- ✅ `direction: "in"` only returns inbound rules
- ✅ `enabled_only: true` excludes disabled rules
- ✅ `profile: "domain"` filters by profile
- ✅ Each rule has all required fields

**Edge Cases:**

- ⚡ Rule with "any" for all fields
- ⚡ Rule with IP range notation
- ⚡ Rule with port range (e.g., "8000-9000")
- ⚡ Rule tied to a specific service
- ⚡ Thousands of rules → returns within reasonable time

---

### Feature: network.firewall.add
### `network.firewall.add` 🟡 Operate

Add a firewall rule.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Rule name |
| `direction` | string | "in" / "out" |
| `action` | string | "allow" / "block" |
| `protocol` | string | "tcp" / "udp" / "any" |
| `local_ports` | string (optional) | Port or range (e.g., "80", "8080-8090") |
| `remote_addresses` | string (optional) | IP or range. Default: "any" |
| `program` | string (optional) | Application path |
| `profile` | string (optional) | "domain" / "private" / "public" / "all". Default: "all" |
| `description` | string (optional) | Rule description |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Rule name created |
| `created` | boolean | Success |

**Implementation:** COM `INetFwPolicy2.Rules.Add()`

---

### Test Spec: network.firewall.add
### `network.firewall.add`

**Happy Path:**

- 🎭 Creates inbound allow rule → verifiable in `network.firewall`
- 🎭 Creates outbound block rule
- 🎭 Rule with specific port, program, and profile

**Input Validation:**

- ✅ Missing `name` → error
- ✅ Missing `direction` → error
- ✅ Invalid `protocol` → error
- ✅ Invalid port range (e.g., "99999") → error
- ✅ Duplicate rule name → error

**Security:**

- 🔒 Requires Operate privilege tier
- 🔒 Cannot create rule that opens all ports inbound
- 🔒 Rule creation logged in audit trail

---

### Feature: network.firewall.remove
### `network.firewall.remove` 🟡 Operate

Remove a firewall rule.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Rule name to remove |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Rule removed |
| `removed` | boolean | Success |

**Implementation:** COM `INetFwPolicy2.Rules.Remove()`

---

### Test Spec: network.firewall.remove
### `network.firewall.remove`

**Happy Path:**

- 🎭 Removes existing rule → no longer appears in list

**Error Handling:**

- ✅ Non-existent rule name → error
- ✅ Protected system rule → error

---

### Feature: network.interfaces
### `network.interfaces` 🟢 Read

List network interfaces with full configuration.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `interfaces` | array | NIC objects |
| `count` | integer | Total count |

**NIC object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Adapter name |
| `description` | string | Adapter description |
| `id` | string | Adapter GUID |
| `type` | string | "ethernet" / "wifi" / "loopback" / "tunnel" / "ppp" |
| `status` | string | "up" / "down" / "testing" / "unknown" |
| `mac_address` | string | MAC address |
| `speed_mbps` | integer | Link speed in Mbps |
| `ipv4_addresses` | array | `[{address, subnet, gateway}]` |
| `ipv6_addresses` | array | `[{address, prefix_length}]` |
| `dns_servers` | array | DNS server addresses |
| `dhcp_enabled` | boolean | Whether DHCP is active |
| `dhcp_server` | string / null | DHCP server address |
| `dns_suffix` | string / null | Connection-specific DNS suffix |
| `mtu` | integer | Maximum transmission unit |
| `bytes_sent` | integer | Total bytes sent |
| `bytes_received` | integer | Total bytes received |
| `packets_sent` | integer | Total packets sent |
| `packets_received` | integer | Total packets received |
| `errors_in` | integer | Inbound errors |
| `errors_out` | integer | Outbound errors |

**Implementation:** `NetworkInterface.GetAllNetworkInterfaces()` + `GetIPProperties()` + `GetIPStatistics()`

---

### Test Spec: network.interfaces
### `network.interfaces`

**Happy Path:**

- ✅ Returns non-empty list (at least loopback)
- ✅ Contains loopback interface with IP 127.0.0.1
- ✅ Each interface has `name`, `status`, `type`
- ✅ Active interface has non-empty `ipv4_addresses`
- ✅ MAC address format is "XX:XX:XX:XX:XX:XX" or "XX-XX-XX-XX-XX-XX"
- ✅ `speed_mbps` > 0 for connected interfaces
- ✅ Traffic counters (`bytes_sent`, `bytes_received`) are non-negative

**Edge Cases:**

- ⚡ VPN adapter (Tailscale, WireGuard) appears in list
- ⚡ Hyper-V virtual switch adapter
- ⚡ Interface with multiple IPv4 addresses
- ⚡ Interface with IPv6 only
- ⚡ Disconnected interface → `status: "down"`, null IP info
- ⚡ Interface with DHCP vs static → `dhcp_enabled` is correct

---

### Feature: network.ping
### `network.ping` 🟢 Read

Ping a host.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `host` | string | Hostname or IP address |
| `count` | integer (optional) | Number of pings. Default: 4 |
| `timeout_ms` | integer (optional) | Per-ping timeout. Default: 5000 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `host` | string | Target host |
| `resolved_address` | string | Resolved IP |
| `results` | array | `[{status, roundtrip_ms, ttl}]` |
| `sent` | integer | Packets sent |
| `received` | integer | Packets received |
| `lost` | integer | Packets lost |
| `loss_percent` | float | Loss percentage |
| `avg_ms` | float | Average roundtrip |
| `min_ms` | float | Minimum roundtrip |
| `max_ms` | float | Maximum roundtrip |

**Implementation:** `System.Net.NetworkInformation.Ping.SendPingAsync()`

---

### Test Spec: network.ping
### `network.ping`

**Happy Path:**

- ✅ Pinging 127.0.0.1 → all succeed, low latency
- ✅ `count: 1` sends exactly 1 ping
- ✅ Returns correct `sent`, `received`, `lost` counts
- ✅ `loss_percent` = 0 for successful pings
- ✅ `avg_ms`, `min_ms`, `max_ms` are calculated correctly
- ✅ `resolved_address` is an IP address

**Error Handling:**

- ✅ Unreachable host → `loss_percent: 100`
- ✅ Unresolvable hostname → error
- ✅ Timeout on all pings → all results show timeout

**Edge Cases:**

- ⚡ `count: 100` → completes in reasonable time
- ⚡ Ping to IPv6 address
- ⚡ Host that drops some packets → partial loss

---

### Feature: network.ports
### `network.ports` 🟢 Read

List listening ports.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `protocol` | string (optional) | "tcp" / "udp" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `listeners` | array | Listener objects |
| `count` | integer | Total count |

**Listener object:**
| Field | Type | Description |
|---|---|---|
| `protocol` | string | "tcp" / "udp" |
| `local_address` | string | Bind address |
| `local_port` | integer | Bind port |
| `pid` | integer | Owning process ID |
| `process_name` | string | Process name |
| `state` | string | "listening" (TCP always listening for this tool) |

**Implementation:** `IPGlobalProperties.GetActiveTcpListeners()` + `GetActiveUdpListeners()` + `GetExtendedTcpTable` for PID mapping

---

### Test Spec: network.ports
### `network.ports`

**Happy Path:**

- ✅ Returns list of listening ports
- ✅ Contains common ports (135 RPC, 445 SMB on domain machine)
- ✅ `protocol: "tcp"` returns only TCP listeners
- ✅ `protocol: "udp"` returns only UDP listeners
- ✅ Each listener has `pid` > 0 and valid `process_name`
- ✅ `local_port` is valid port number (1-65535)

**Edge Cases:**

- ⚡ Port bound to 0.0.0.0 vs 127.0.0.1 vs specific IP
- ⚡ Same port on different IPs (multiple listeners)
- ⚡ High port number (>49152) for ephemeral range
- ⚡ IPv6 listeners ([::]:80)

---

### Feature: network.routing
### `network.routing` 🟢 Read

Show routing table.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `routes` | array | Route objects |
| `count` | integer | Total routes |

**Route object:**
| Field | Type | Description |
|---|---|---|
| `destination` | string | Destination network |
| `prefix_length` | integer | Subnet prefix length |
| `next_hop` | string | Gateway address |
| `interface_index` | integer | Interface index |
| `interface_alias` | string | Interface name |
| `metric` | integer | Route metric |
| `protocol` | string | "local" / "netmgmt" / "dhcp" / "static" |
| `type` | string | "unicast" / "broadcast" / "multicast" |

**Implementation:** `Get-NetRoute` via PowerShell SDK or `GetIpForwardTable2`

---

### Test Spec: network.routing
### `network.routing`

**Happy Path:**

- ✅ Returns non-empty route table
- ✅ Contains default route (0.0.0.0/0)
- ✅ Contains loopback route (127.0.0.0/8)
- ✅ Each route has valid `metric` > 0

---

### Feature: network.traceroute
### `network.traceroute` 🟢 Read

Trace route to a host.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `host` | string | Target hostname or IP |
| `max_hops` | integer (optional) | Maximum hops. Default: 30 |
| `timeout_ms` | integer (optional) | Per-hop timeout. Default: 5000 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `host` | string | Target host |
| `hops` | array | `[{hop, address, hostname, roundtrip_ms, status}]` |
| `reached` | boolean | Whether target was reached |

**Implementation:** `Ping.SendPingAsync()` with incrementing TTL

---

### Test Spec: network.traceroute
### `network.traceroute`

**Happy Path:**

- ✅ Trace to 127.0.0.1 → 1 hop
- ✅ Returns `hops` array with incrementing hop numbers
- ✅ `reached: true` when target is reached
- ✅ Each hop has `address` (or "\*" for timeout)

**Error Handling:**

- ✅ Unresolvable target → error
- ✅ Unreachable target → `reached: false`, partial hops

**Edge Cases:**

- ⚡ Hop that doesn't respond (shown as \*)
- ⚡ `max_hops: 1` → single hop result
- ⚡ Asymmetric routing (different path each time)

---

