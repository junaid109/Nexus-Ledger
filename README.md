# ApexLedger 🚀

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0%20%7C%2010.0-purple)]()
[![Aspire](https://img.shields.io/badge/Orchestration-.NET%20Aspire-blue)]()

**ApexLedger** is a next-generation, high-performance payment ledger system designed for the fintech sector. Engineered for exactly-once processing and absolute data integrity, it leverages a distributed microservices architecture to handle high-throughput financial transactions with ease.

---

## 🏗️ Architectural Highlights

ApexLedger is built as a greenfield project showcasing modern software engineering patterns:

- **Microservices Architecture**: Decomposed into domain-centric services (Gateway, Settlement, Reconciliation).
- **Event-Driven Choreography**: Asynchronous communication via **Apache Kafka** ensures loose coupling and scalability.
- **Data Integrity**:
  - **Idempotency**: Redis-backed barriers in the Payment Gateway prevent duplicate processing.
  - **Transactional Outbox**: Guarantees atomic writes to the Ledger and message dispatching, eliminating "dual-write" issues.
- **Resilience**: Integrated **Polly** policies for circuit breaking and exponential backoff strategies.
- **Observability**: Full tracing with **OpenTelemetry** and **Jaeger** propagation across message boundaries.
- **Orchestration**: Fully orchestrated local development environment using **.NET Aspire**.

---

## 🛠️ Technology Stack

| Category | Technology |
|----------|------------|
| **Core** | .NET 9 / 10, C# |
| **API** | ASP.NET Core Web API |
| **Orchestration** | .NET Aspire |
| **Message Broker** | Confluent Kafka |
| **Database** | SQL Server (EF Core), Redis (Caching/Locks) |
| **Observability** | OpenTelemetry, Jaeger, Prometheus |
| **Testing** | xUnit, Pact.io (Contract Testing) |
| **Containerization** | Docker, Docker Compose |

---

## 🧩 Services Overview

1.  **Payment Gateway API**
    *   **Role**: Entry point for payment initiation.
    *   **Features**: Validates requests, enforces idempotency, and publishes `PaymentInitiated` events.
2.  **Settlement Service**
    *   **Role**: The core ledger worker.
    *   **Features**: Consumes events, creates ledger entries using T-SQL/EF Core, and manages the Transactional Outbox.
3.  **Reconciliation Worker**
    *   **Role**: Financial health check.
    *   **Features**: Daily comparison of internal ledger state against external bank statements.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or newer)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Aspire Workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=dotnet-cli)

### Running Locally

ApexLedger uses **.NET Aspire** to orchestrate the entire suite of services and infrastructure (Kafka, Redis, SQL Server) with a single command.

1.  **Clone the repository**
    ```bash
    git clone https://github.com/junaid109/Nexus-Ledger.git
    cd Nexus-Ledger
    ```

2.  **Run with Aspire**
    ```bash
    dotnet run --project ApexLedger.AppHost
    ```

3.  **Explore the Dashboard**
    The console output will provide a link to the Aspire Dashboard (typically `https://localhost:18888`), where you can view logs, traces, and metrics for all running services.

---

## 🧪 Testing

Run unit and integration tests:

```bash
dotnet test
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
