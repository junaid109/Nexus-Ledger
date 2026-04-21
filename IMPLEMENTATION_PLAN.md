# NexusLedger Implementation Plan

## Overview
NexusLedger is a high-performance, event-driven payment ledger system built on .NET 9/10. It uses a microservices architecture with Kafka for choreography, SQL Server for the ledger, and Redis for idempotency. The system orchestrates services using .NET Aspire and ensures resilience and observability through Polly and OpenTelemetry.

## Architecture
- **Microservices**:
  - `NexusLedger.PaymentGateway`: Web API entry point, idempotent with Redis.
  - `NexusLedger.SettlementService`: Event-driven worker (Kafka), Transactional Outbox.
  - `NexusLedger.ReconciliationWorker`: Background service for daily check & balance.
- **Data Stores**: SQL Server (Ledger), Redis (Idempotency cache).
- **Messaging**: Kafka (Choreography).
- **Observability**: OpenTelemetry, Jaeger.
- **Orchestration**: .NET Aspire.

## Phases

### Phase 1: Foundation & Infrastructure
- [x] Create Solution `NexusLedger.sln`.
- [x] Create `docker-compose.yml` for external dependencies (SQL, Kafka, Zookeeper, Redis, Jaeger).
- [x] Scaffold projects with Clean Architecture (Domain, Application, Infrastructure, API/Worker) for:
  - `NexusLedger.PaymentGateway`
  - `NexusLedger.SettlementService`
  - `NexusLedger.ReconciliationWorker`
- [x] Setup Git repository.

### Phase 2: Aspire Orchestration
- [x] create `NexusLedger.AppHost` project.
- [x] create `NexusLedger.ServiceDefaults` project.
- [x] Register services in AppHost (SQL, Redis, Kafka, Projects).
- [x] Wire up ServiceDefaults in all microservices.

### Phase 3: Payment Gateway (API & Idempotency)
- [x] Implement `Domain` entities (PaymentRequest Model).
- [x] Implement `Infrastructure` (Redis caching / IdempotencyFilter).
- [ ] Implement `Application` UseCases (ProcessPayment).
- [x] Implement `API` Controller/Endpoints with `X-Idempotency-Key` middleware.
- [ ] Integrate Kafka Producer for `PaymentInitiated` event.

### Phase 4: Settlement Service (Transactional Outbox)
- [x] Implement `Domain` entities (LedgerEntry).
- [x] Implement `Infrastructure` with EF Core (SQL Server).
- [ ] Implement Transactional Outbox pattern.
- [x] Implement Kafka Consumer for `PaymentInitiated`.
- [x] Publish `PaymentValidated` / `SettlementCompleted` events (Logic exists in Worker).

### Phase 5: Reconciliation Worker
- [x] Create mocked External Bank Source (CSV/JSON/Mock API).
- [x] Implement Daily Reconciliation Job (IHostedService).
- [x] Compare internal SQL Ledger vs External Bank source.
- [ ] Alerting/Event on discrepancy.

### Phase 6: Observability, Resilience & Testing
- [ ] Configure OpenTelemetry Tracing (propagating TraceID).
- [ ] Apply Polly retries/circuit breakers for external calls.
- [ ] Add xUnit tests for core logic.
- [ ] Add Pact.io CDC tests.

### Phase 7: Frontend Dashboard (Next.js & Tailwind)
*Reflecting the "High-Performance Fintech" aesthetic.*

#### 1. Setup & Orchestraion
- [ ] Initialize Next.js 14+ project (`NexusLedger.Web`) with TypeScript and Tailwind CSS.
- [ ] Integreate with Aspire AppHost as a Node.js project.
- [ ] Setup `shadcn/ui` for component library.
- [ ] Configure `TanStack Query` for state management and API integration.

#### 2. Core Layout & Navigation
- [ ] Implement Responsive Sidebar (Collapsible) with categories:
    - *Main*: Dashboard, Analytics, Reports.
    - *Services*: Virtual Terminal, Payment Links, Invoices.
    - *Settings*: Team, Developers (API Keys), Account.
- [ ] Build Top Navigation Bar (Search, Notifications, Profile, Theme Toggle).
- [ ] Implement Breadcrumbs and dynamic page titles.

#### 3. Dashboard (Overview)
- [ ] Create **Summary Cards Component**: Animated counters for "Total Revenue", "Net Profit", "Active Users" with trend indicators.
- [ ] Implement **Revenue Chart**: Area chart using `Recharts` showing revenue over time.
- [ ] Build **Live Transaction Feed**: Real-time list of recent payments with status badges (Success/Pending/Failed).
- [ ] Add **Quick Actions**: Buttons for "New Payment", "Generate Link", "Refund".

#### 4. Payment Management Views
- [ ] **Transactions List**: Data table with sorting, filtering (Date, Status, Amount), and CSV export.
- [ ] **Transaction Detail Modal**: Slide-over or modal showing full payment lifecycle properties (Metadata, Timeline, Risk Score).
- [ ] **Virtual Terminal**:
    - Credit Card Input Form with validation.
    - Currency selector.
    - Customer email/reference fields.

#### 5. Wallet & Settlement
- [ ] **Wallet View**: Display current balance, held funds, and payout schedule.
- [ ] **Settlements Table**: History of deposits to the connected bank account.
- [ ] **Bank Accounts**: Manage connected payout accounts (Add/Remove).

#### 6. Settings & Developer Tools
- [ ] **API Keys Management**: Generate/Revoke keys, view usage stats.
- [ ] **Webhooks**: Configure webhook URLs and view delivery attempts (Redelivery UI).
- [ ] **Team Management**: Invite users, assign roles (Admin, Viewer, Developer).

## Current Step
## Current Step
Starting Phase 7: Frontend Dashboard setup.
