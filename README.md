# CraftTrip — Vietnamese Traditional Craft Village Experience Marketplace

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4.svg)](https://dotnet.microsoft.com/apps/aspnet/mvc)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-5C2D91.svg)](https://learn.microsoft.com/ef/core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

CraftTrip is a digital marketplace connecting traditional Vietnamese craft villages, local artisans, workshop hosts, and travelers seeking authentic, hands-on craft experiences.

---

## Table of Contents
- [Project Overview](#project-overview)
- [Main Business Objectives](#main-business-objectives)
- [User Roles & Permissions](#user-roles--permissions)
- [Core Features](#core-features)
  - [Guest Experience](#guest-experience)
  - [Host / Artisan Management](#host--artisan-management)
  - [Platform Administration](#platform-administration)
  - [Electronic Tickets & Verification](#electronic-tickets--verification)
  - [Financials & Payouts](#financials--payouts)
- [Architecture & Tech Stack](#architecture--tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Configuration Guide](#configuration-guide)
  - [Database Configuration](#database-configuration)
  - [VNPay Payment Gateway](#vnpay-payment-gateway)
  - [SMTP Email Delivery](#smtp-email-delivery)
- [Getting Started](#getting-started)
- [Running Automated Tests](#running-automated-tests)
- [Security & Architecture Guardrails](#security--architecture-guardrails)
- [Default Seed Accounts](#default-seed-accounts)

---

## Project Overview

Traditional craft villages in Vietnam (such as Bát Tràng ceramics, Chuông conical hats, Vạn Phúc silk, and Phú Vinh bamboo weaving) hold centuries of rich cultural heritage. CraftTrip bridges the gap between traditional craft communities and modern travelers by providing an intuitive digital platform where artisans can showcase workshops and guests can book immersive cultural experiences.

---

## Main Business Objectives

- **Preserve & Promote Culture:** Digitize authentic Vietnamese traditional craft villages and artisanal techniques.
- **Direct Artisan Support:** Empower artisans and workshop hosts to manage schedules, track revenue, and receive transparent payouts.
- **Frictionless Discovery & Booking:** Allow travelers to filter workshops by craft category, village, province, and available dates.
- **Guaranteed Capacity & Trust:** Provide 15-minute slot reservation holds, real-time capacity management, 48-hour 100% refund cancellation policies, and cryptographic QR e-tickets.

---

## User Roles & Permissions

CraftTrip enforces role-based access control with three user roles:

| Role | Scope & Permissions |
| :--- | :--- |
| **Guest** | Browse workshops, explore craft villages, book workshop sessions, make secure payments via VNPay, receive QR e-tickets via email, manage personal bookings, and cancel eligible sessions ( $\ge$ 48h prior). |
| **Host** | Apply for artisan verification, manage workshop listings and session schedules, view upcoming bookings, check in guests via QR code scanner, and track revenue and payout requests. |
| **Admin** | Review and approve workshop listings, manage craft categories and village master data, audit transactions, oversee host payouts, and monitor platform health. |

---

## Core Features

### Guest Experience
- **Village & Workshop Exploration:** Interactive search and filters by village, craft discipline, rating, and schedule availability.
- **Seamless Booking Flow:** Live seat capacity reservation with 15-minute pending hold to prevent overselling.
- **VNPay Integration:** Complete integration supporting both real sandbox gateway endpoints and a development simulator for cardless local testing.
- **"Vé của tôi" (My E-Tickets):** Dedicated ticket wallet displaying dynamic QR codes, session date/time, artisan contact, and real-time status badges.

### Host / Artisan Management
- **Storefront & Workshop Management:** Create and edit workshop profiles, set capacity limits, define pricing, and configure schedules.
- **QR Code Check-In:** Dedicated host check-in terminal to scan or enter guest ticket codes, preventing duplicate attendance.
- **Artisan Financial Dashboard:** Track gross earnings, platform commission deductions, and pending/completed payout statuses.

### Platform Administration
- **Marketplace Dashboard:** High-level overview of active listings, platform commission, and system metrics.
- **Workshop Moderation:** Multi-state approval lifecycle (Draft $\to$ Pending Review $\to$ Published / Rejected).
- **Financial Audit & Payout Engine:** Automated payout eligibility processing (matured 24 hours post-session) and financial transaction logs.

### Electronic Tickets & Verification
- **Cryptographic QR Codes:** Generated via QRCoder using unique ticket hash payloads (`CRAFTTRIP-TICKET:{TicketCode}:{BookingCode}`).
- **Anti-Fraud Single-Use Check:** Tickets are marked as redeemed upon check-in; subsequent scans are immediately rejected with alert timestamps.

### Email Delivery
- **Automated E-Ticket Delivery:** Responsive HTML confirmation emails containing ticket barcodes, workshop details, location directions, and cancellation terms.
- **Fail-Safe Mechanism:** Payment processing and ticket generation remain 100% resilient even when SMTP delivery encounters external transport failures.

---

## Architecture & Tech Stack

CraftTrip follows Clean Architecture principles:

- **Framework:** .NET 8.0 / ASP.NET Core MVC
- **Database & ORM:** Entity Framework Core 8.0 with dual-engine provider support:
  - **SQLite:** Zero-configuration default for local development, rapid setup, and isolated testing.
  - **SQL Server / Azure SQL:** Enterprise relational storage for staging and production deployments.
- **Identity & Security:** ASP.NET Core Identity with role-based authorization, anti-forgery verification tokens (`ValidateAntiForgeryToken`), and IDOR protection.
- **Front-End:** Bootstrap 5, Bootstrap Icons, responsive mobile-first UI with authentic Vietnamese terracotta / warm wood brand identity.
- **Payment Gateway:** VNPay SDK & Sandbox Simulation mode.
- **Testing:** xUnit, Moq, EF Core In-Memory Database provider.

---

## Project Structure

```text
CraftTrip/
├── CraftTrip.Core/                # Domain entities, value objects, domain enums, interfaces
│   ├── Entities/                  # ApplicationUser, Workshop, Booking, Ticket, Payment, Payout
│   ├── Enums/                     # UserRole, WorkshopStatus, BookingStatus, AttendanceStatus
│   └── Interfaces/                # Core service abstractions
├── CraftTrip.Infrastructure/      # Data access, EF Core DbContext, migrations, seeders
│   └── Data/                      # CraftTripDbContext, DatabaseSeeder
├── CraftTrip.Services/            # Application business logic, DTOs, service implementations
│   ├── DTOs/                      # Data transfer contracts
│   ├── Implementations/           # BookingService, PaymentService, EmailService, FinancialService
│   └── Interfaces/                # Application service interfaces
├── CraftTrip.Web/                 # Presentation layer: ASP.NET Core MVC
│   ├── Areas/                     # Admin & Host feature modules
│   ├── Controllers/               # Account, Workshop, Booking, Home controllers
│   ├── ViewModels/                # ViewModels for presentation views
│   ├── Views/                     # Razor views, partials, and layouts
│   └── wwwroot/                   # Static CSS, JavaScript, vendor libraries, images
└── CraftTrip.Tests/               # Automated unit, integration, and workflow tests
    ├── BusinessRules/             # Financial and domain rule verifications
    ├── Security/                  # IDOR, authorization, and boundary tests
    └── Workflows/                 # End-to-end booking, payment, and check-in workflows
```

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Visual Studio 2022 (v17.8+) or Visual Studio Code with the C# Dev Kit extension
- *(Optional)* Microsoft SQL Server 2019+ or LocalDB (if choosing SQL Server over SQLite)

---

## Configuration Guide

Configuration files are located in `CraftTrip.Web/appsettings.json`. An example configuration template is provided in `CraftTrip.Web/appsettings.example.json`.

### Database Configuration

The application features an automatic provider selector (`Database:Provider = "Auto"`):
- If `DefaultConnection` points to an active SQL Server instance, it uses SQL Server.
- Otherwise, it seamlessly falls back to local SQLite (`crafttrip.db`).

To force a specific provider:
```json
"Database": {
  "Provider": "Sqlite" // Options: "Auto", "Sqlite", "SqlServer"
}
```

### VNPay Payment Gateway

```json
"VNPay": {
  "TmnCode": "YOUR_TMN_CODE",
  "HashSecret": "YOUR_HASH_SECRET",
  "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
  "SimulationMode": true
}
```
> **Note:** Set `"SimulationMode": true` for development environments to use the built-in VNPay payment simulator without requiring active sandbox test cards.

### SMTP Email Delivery

Configure your SMTP credentials via `appsettings.json` or environment variables:

| Setting Key | Environment Variable | Description |
| :--- | :--- | :--- |
| `Smtp:Host` | `SMTP_HOST` | SMTP server host (e.g., `smtp.gmail.com`) |
| `Smtp:Port` | `SMTP_PORT` | Port (`587` for TLS / STARTTLS) |
| `Smtp:Username` | `SMTP_USERNAME` | SMTP account username / email |
| `Smtp:Password` | `SMTP_PASSWORD` | SMTP password or Google App Password |
| `Smtp:FromEmail`| `SMTP_FROM_EMAIL` | Sender address |
| `Smtp:FromName` | `SMTP_FROM_NAME` | Sender display name |
| `Smtp:EnableSsl` | `SMTP_ENABLE_SSL` | `true` |

---

## Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/kainansa/CraftTrip.git
   cd CraftTrip
   ```

2. **Restore dependencies & build:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run the application:**
   ```bash
   dotnet run --project CraftTrip.Web
   ```

4. **Navigate to the application:**
   Open your browser and navigate to `http://localhost:5166`.
   The application automatically initializes and seeds required master data, demo workshops, and default accounts on first run.

---

## Running Automated Tests

CraftTrip includes an automated test suite verifying business logic, financial calculations, anti-fraud check-ins, and booking workflows:

```bash
dotnet test
```

Expected output:
```text
Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34
```

---

## Security & Architecture Guardrails

- **Sensitive Data Isolation:** All production secrets, passwords, private database files, and local certificates are strictly excluded via `.gitignore`.
- **Anti-Forgery Token Verification:** All state-modifying actions enforce `[ValidateAntiForgeryToken]`.
- **IDOR Protection:** All booking retrieval, ticket view, and cancellation operations enforce ownership checks (`booking.GuestId == currentUserId`).
- **Capacity Concurrency Protection:** EF Core concurrency checks prevent double-booking or over-allocation of workshop slots.
- **Monetary Precision:** All currency amounts are strictly stored and computed using `decimal` to avoid floating-point rounding errors.

---

## Default Seed Accounts

The initial database seed provisions the following test accounts:

| Role | Email | Password |
| :--- | :--- | :--- |
| **Admin** | `admin@crafttrip.vn` | `CraftTrip@2026!` |
| **Host (Artisan)** | `artisan@crafttrip.vn` | `CraftTrip@2026!` |
| **Guest** | `guest@crafttrip.vn` | `CraftTrip@2026!` |

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
