# 🏦 FirstBank Transaction Anomaly Detector API

**Enterprise Backend Architecture & Security Implementation**

*Note: This project is an architectural simulation built during a Software Engineering (SIWES) Internship to demonstrate production-ready cloud deployments, containerization, and API security.*

## 📋 Project Overview
The Transaction Anomaly Detector is a robust, containerized financial backend service. It is designed to process incoming financial data, validate transactions, detect potential anomalies, and enforce strict zero-trust security protocols. 

## 🏗️ Architecture & Tech Stack
* **Framework:** ASP.NET Core Web API (C#)
* **Data Access:** Dapper (Micro-ORM) for high-performance, parameterized SQL queries
* **Database:** SQL Server (Containerized)
* **Observability:** Serilog + Seq for structured, searchable event logging
* **Containerization:** Docker & Docker Compose for isolated, reproducible environments

## 🔒 Security Controls
* **Authentication:** Stateless JWT (JSON Web Tokens) with secure issuer/audience validation
* **Data Protection:** Passwords securely hashed using BCrypt
* **SQL Injection Prevention:** 100% parameterized queries enforced via Dapper
* **Secrets Management:** Configuration injected strictly via Environment Variables (no hardcoded secrets)
* **Least Privilege:** Database user accounts restricted to required table operations only

## 🚀 Getting Started (Local Development)
To run this API environment locally, ensure you have Docker Desktop installed.

1. Clone the repository:
   ```bash
   git clone [https://github.com/YOUR-USERNAME/firstbank-anomaly-detector.git](https://github.com/YOUR-USERNAME/firstbank-anomaly-detector.git)