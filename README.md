# Business Management API

![ci](https://github.com/<user>/<repo>/actions/workflows/ci.yml/badge.svg)

ASP.NET Core (.NET 10) backend for products, customers and orders. It practises production-style
concerns: transactional stock reservation, JWT with roles, a transactional outbox with RabbitMQ,
and an idempotent consumer.

## Highlights
- Orders reserve stock with an atomic conditional UPDATE inside one transaction; 10 parallel requests for 5 units create exactly 5 orders (integration test).
- Transactional outbox + relay (`FOR UPDATE SKIP LOCKED`, exponential backoff); idempotent consumer, retry queue and dead-letter queue.
- Query-count tests prevent N+1; index choices verified with EXPLAIN ANALYZE (24 ms to under 1 ms on 100k orders).
- 200+ tests against real PostgreSQL and RabbitMQ (Testcontainers).

## Architecture
(two Mermaid diagrams, see below)

## Architecture - Transactional Outbox with RabbitMQ

```mermaid
flowchart TB
    Client[Client]
    API[ASP.NET Core API]
    DB[(PostgreSQL)]
    Relay[Worker: Outbox Relay]
    MQ[(RabbitMQ)]
    Consumer[Worker: Consumer]
    DLQ[(Dead Letter Queue)]

    Client -->|HTTP + JWT| API

    API -->|Save Order + Outbox Event| DB
    DB -->|Same Transaction| DB

    Relay -->|Poll pending events| DB
    Relay -->|Publish order.created| MQ

    MQ -->|Deliver message| Consumer
    MQ -.->|Failed messages| DLQ

    Consumer -->|Save Notification + Processed Marker| DB

    classDef service fill:#dbeafe,stroke:#2563eb,color:#1e3a8a
    classDef database fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef queue fill:#fef3c7,stroke:#d97706,color:#78350f

    class Client,API,Relay,Consumer service
    class DB database
    class MQ,DLQ queue
```

## Transactional Outbox - Sequence Diagram

```mermaid
sequenceDiagram
    autonumber

    participant Client
    participant API
    participant DB as PostgreSQL
    participant Relay as Outbox Relay
    participant MQ as RabbitMQ
    participant Consumer

    Client->>API: POST /api/orders
    API->>DB: Reserve stock, insert order and outbox row<br/>(one transaction)
    API-->>Client: 201 Created

    Relay->>DB: SELECT pending rows<br/>FOR UPDATE SKIP LOCKED
    Relay->>MQ: Publish order.created
    Relay->>DB: Mark outbox row as processed

    MQ->>Consumer: Deliver message

    Consumer->>DB: Insert notification and processed marker<br/>(one transaction)

    Consumer-->>MQ: ACK
```

## Tech stack
.NET 10 (LTS), ASP.NET Core, EF Core 10, PostgreSQL 18, RabbitMQ 4, xUnit + Testcontainers, Docker Compose, GitHub Actions

## Quick start
```bash
cp .env.example .env 
docker compose up -d --build
```
API: http://localhost:8080 (`/health`) · RabbitMQ UI: http://localhost:15672

## Try it
(register, login, create product, customer, order, cancel with curl; one error example)

## Run the tests
Requires Docker (Testcontainers). `dotnet test`

## Design decisions
(one line each, link to docs/decisions.md or PROJECT_STATUS.md)

## Known limitations
(from the debt list: no payments yet, JWT cannot be revoked, no rate limiting, outbox cleanup job missing, ...)

## Project layout