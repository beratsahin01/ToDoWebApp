# 📝 ToDoWebApp - Collaborative Task & Team Management Platform

[![CI - Build & Test](https://github.com/beratsahin01/ToDoWebApp/actions/workflows/ci.yml/badge.svg)](https://github.com/beratsahin01/ToDoWebApp/actions/workflows/ci.yml)
[![Live Demo](https://img.shields.io/badge/Live_Demo-onrender.com-success?style=flat&logo=render)](https://todowebapp-70cm.onrender.com)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![MSSQL](https://img.shields.io/badge/Database-SQL_Server-CC292B?logo=microsoftsqlserver)
![Redis](https://img.shields.io/badge/Cache-Redis-DC382D?logo=redis)
![Docker](https://img.shields.io/badge/Container-Docker-2496ED?logo=docker)
![Bootstrap](https://img.shields.io/badge/UI-Bootstrap_5-7952B3?logo=bootstrap)
![License](https://img.shields.io/badge/License-MIT-blue.svg)

A modern, high-performance, and secure **Task & WorkGroup Collaboration** web application built with **ASP.NET Core**, **Entity Framework Core**, **Redis distributed caching**, **Microsoft SQL Server**, and **Docker**.

---

## 🌟 Key Features

### 📋 Task Management
- **Full CRUD Operations:** Create, edit, complete, categorize, and delete tasks seamlessly.
- **Drag & Drop Organization:** Dynamically move tasks across categories using `SortableJS`.
- **Prioritization & Deadlines:** Assign priority tags (Low, Medium, High) and track approaching due dates with visual indicators.
- **Interactive Confetti:** Engaging celebrations upon completing milestones and tasks.

### 👥 WorkGroups & Team Spaces
- **Collaborative Rooms:** Create private or shared workspaces for projects and teams.
- **Role-Based Access Control (RBAC):** Three-tiered access system:
  - 👑 **Owner:** Full project oversight, delete workspace, manage members.
  - 🛠️ **Admin:** Manage and create tasks & categories.
  - 👁️ **Viewer:** Read-only access to tasks and discussions.
- **Team Invitations:** Invite friends directly into rooms with customizable roles.

### 🤝 Social & Friends System
- **Real-Time User Search:** Find team members and colleagues by username or email.
- **Friend Requests:** Send, accept, decline, and cancel pending friendship requests.
- **Task Discussions:** Embedded comment threads directly inside task cards for seamless team communication.

### 🎨 Modern UI & User Experience
- **Theme Switcher:** Seamless Dark Mode and Light Mode with persistent local storage.
- **Profile Customization:** Choose from instant Dicebear vector avatars or upload custom profile pictures.
- **Productivity Dashboard:** Real-time statistics tracking total, completed, in-progress, and high-priority tasks.

### 🛡️ Enterprise-Grade Security (Production-Ready)
- **DOM-based XSS Sanitization:** Comprehensive HTML character escaping across all user inputs before DOM rendering.
- **CSRF Protection:** Full Anti-Forgery Token validation on all mutating API endpoints (`PUT`, `POST`, `DELETE`, `PATCH`).
- **Secure File Uploads & Path Traversal Prevention:** Whitelisted image formats, MIME validation, and isolated file handling via sanitized filenames.
- **Brute-Force & Rate Limiting:** Fixed-window rate limiter powered by ASP.NET Core built-in rate limiting middleware.
- **Modern Cookie Security:** Configured with `HttpOnly`, `SameSite=Lax`, and contextual `SameAsRequest` secure policies.
- **Cryptographically Secure Tokens:** CSPRNG 32-byte cryptographic tokens for password reset flows with expiry timestamps.

### ⚡ Performance & Scalability
- **Redis Distributed Caching:** Automated caching layer with 5-minute sliding windows for rapid task reads.
- **Cache Invalidation:** Smart, automated invalidation on task mutations to keep clients synchronized.
- **Fail-Safe Resilience:** Graceful automatic fallback to the database if the Redis cache instance becomes unavailable.

---

## 🛠️ Tech Stack

| Category | Technologies |
| :--- | :--- |
| **Backend** | C#, .NET 10.0, ASP.NET Core (MVC & RESTful APIs), Entity Framework Core |
| **Database** | Microsoft SQL Server 2022 |
| **Caching** | Redis (StackExchange.Redis Distributed Cache) |
| **Frontend** | Vanilla JavaScript (ES6+), HTML5, CSS3, Bootstrap 5, SortableJS, Canvas-Confetti, FontAwesome |
| **Security & Auth** | Cookie Authentication, PasswordHasher, CSPRNG Tokens, AntiForgery, RateLimiter |
| **DevOps & Cloud** | Multi-stage Dockerfile, Docker Compose, GitHub Actions (CI/CD), Render |

---

## 🐳 Quick Start with Docker

Run the entire application stack (**Web App + SQL Server 2022 + Redis**) with a single command:

```bash
docker compose up -d
```

Once running, access the application in your browser:
👉 **[http://localhost:5000](http://localhost:5000)**

---

## 💻 Local Development Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server & Redis)

### Steps
1. **Start Database & Cache Containers:**
   ```bash
   docker compose up -d db redis_cache
   ```

2. **Apply Database Migrations:**
   ```bash
   dotnet ef database update
   ```

3. **Run the Web Application:**
   ```bash
   dotnet run
   ```
   Open your browser at: `http://localhost:5063`

---

## ⚙️ Environment Variables

| Variable | Description | Default |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Application runtime environment (`Development` / `Production`) | `Development` |
| `ConnectionStrings__DefaultConnection` | SQL Server database connection string | `Server=localhost;Database=TaskAppDb;...` |
| `ConnectionStrings__Redis` | Redis cache connection string | `localhost:6379` |
| `Resend__ApiKey` | Resend API Key for password reset transactional emails | `""` |

---

## 🚢 CI/CD Pipeline

Continuous Integration is automated using **GitHub Actions** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)):
- Automatically builds the solution on every push and pull request to `main`.
- Runs multi-stage Docker build verification.
- Displays real-time build status badge in the repository.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
