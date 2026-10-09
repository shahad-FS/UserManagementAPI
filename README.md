# User Management API

A RESTful Web API built with **ASP.NET Core Minimal APIs** to manage users, validate incoming data, handle errors, and secure endpoints using JWT authentication.

This project combines three development tasks into one application:

1. Building a User Management API with CRUD operations.
2. Debugging and improving validation, error handling, and pagination.
3. Implementing middleware for request logging, centralized exception handling, and JWT authentication.

## Table of Contents

* [Features](#features)
* [Technology Stack](#technology-stack)
* [Project Structure](#project-structure)
* [Getting Started](#getting-started)
* [Configuration](#configuration)
* [Authentication](#authentication)
* [API Endpoints](#api-endpoints)
* [Validation and Error Handling](#validation-and-error-handling)
* [Middleware Pipeline](#middleware-pipeline)
* [Testing](#testing)
* [Current Limitations](#current-limitations)
* [Future Improvements](#future-improvements)

## Features

### User Management

* Create users.
* Retrieve all users with pagination.
* Retrieve a user by ID.
* Update an existing user.
* Delete a user.
* Prevent duplicate email addresses.

### Input Validation and Error Handling

* Validate required names and email addresses.
* Return appropriate HTTP status codes.
* Handle nonexistent users.
* Reject invalid pagination parameters.
* Centralize unexpected exception handling.
* Return generic error messages without exposing internal exception details.

### Middleware and Security

* Log HTTP methods, request paths, response status codes, and request duration.
* Validate JWT access tokens.
* Protect user management endpoints using authorization requirements.
* Provide a development-only endpoint for obtaining test tokens.
* Use User Secrets to store local development credentials and the JWT signing key.

## Technology Stack

* **C#**
* **ASP.NET Core Minimal APIs**
* **.NET**
* **JWT Bearer Authentication**
* **Microsoft.Extensions.Logging**
* **Postman** or **Visual Studio Code REST Client**
* **In-memory storage** for user data

## Project Structure

```text
UserManagementAPI/
├── Middleware/
│   └── RequestLoggingMiddleware.cs
├── Models/
│   ├── User.cs
│   └── UserRequest.cs
├── Services/
│   ├── UserStore.cs
│   └── UserValidation.cs
├── Program.cs
├── appsettings.json
├── UserManagementAPI.csproj
├── UserManagementAPI.http
└── README.md
```

### Main Components

| Component                                | Responsibility                                                                    |
| ---------------------------------------- | --------------------------------------------------------------------------------- |
| `Program.cs`                             | Registers services, configures JWT, defines endpoints, and configures middleware. |
| `Models/User.cs`                         | Defines the user entity returned by the API.                                      |
| `Models/UserRequest.cs`                  | Defines the data accepted when creating or updating a user.                       |
| `Services/UserStore.cs`                  | Implements in-memory storage and CRUD operations.                                 |
| `Services/UserValidation.cs`             | Validates user names and email addresses.                                         |
| `Middleware/RequestLoggingMiddleware.cs` | Logs request details and response status codes.                                   |
| `appsettings.json`                       | Stores non-secret application configuration.                                      |
| `UserManagementAPI.http`                 | Contains HTTP requests for manually testing the API.                              |

## Getting Started

### Prerequisites

Install the following tools:

* .NET SDK compatible with the project's target framework.
* Visual Studio Code or another .NET-compatible IDE.
* Postman or the REST Client extension for VS Code.

### 1. Clone the Repository

```bash
git clone <repository-url>
cd UserManagementAPI
```

Replace `<repository-url>` with your repository URL. If you already have the project locally, navigate to its root directory instead.

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure User Secrets

Initialize User Secrets if they have not already been configured:

```bash
dotnet user-secrets init
```

Set the JWT signing key and development-only login credentials:

```bash
dotnet user-secrets set "Jwt:SigningKey" "Replace-With-A-Local-Development-Key-At-Least-32-Characters"
dotnet user-secrets set "DemoAuth:Username" "demo"
dotnet user-secrets set "DemoAuth:Password" "Replace-With-A-Strong-Local-Password"
```

These are example values for local development. Use your own development credentials and never commit real secrets to source control.

### 4. Build the Project

```bash
dotnet build
```

Resolve any build errors before proceeding.

### 5. Run the Application

```bash
dotnet run
```

The terminal displays the local HTTP and HTTPS addresses. Use the actual address shown by your application when sending requests.

If Swagger is configured in the project, it can also be used to explore and test the endpoints.

## Configuration

The `appsettings.json` file contains the non-secret configuration required by the application.

Example:

```json
{
  "Jwt": {
    "Issuer": "UserManagementAPI",
    "Audience": "UserManagementClient"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

The JWT signing key and development credentials are provided through User Secrets rather than being stored in this file.

The JWT issuer, audience, signing key, and token validation settings must match the values used by the trusted token issuer.

## Authentication

The API uses JWT Bearer authentication to protect user management endpoints.

### Obtain a Development Token

In the Development environment, send a `POST` request to:

```http
POST /api/auth/token
Content-Type: application/json
```

Request body:

```json
{
  "username": "demo",
  "password": "Replace-With-A-Strong-Local-Password"
}
```

A successful response contains an access token:

```json
{
  "accessToken": "<jwt-access-token>",
  "tokenType": "Bearer",
  "expiresIn": 1800
}
```

Use the returned token in the `Authorization` header:

```http
Authorization: Bearer <jwt-access-token>
```

The development token endpoint is intended only for local testing. It is not a production-ready login system.

## API Endpoints

All `/api/users` endpoints require a valid JWT access token.

| Method   | Endpoint          | Description                             | Expected Success            |
| -------- | ----------------- | --------------------------------------- | --------------------------- |
| `GET`    | `/api/public`     | Check API availability                  | `200 OK`                    |
| `POST`   | `/api/auth/token` | Obtain a development token              | `200 OK`                    |
| `GET`    | `/api/users`      | Retrieve users with pagination          | `200 OK`                    |
| `GET`    | `/api/users/{id}` | Retrieve a user by ID                   | `200 OK`                    |
| `POST`   | `/api/users`      | Create a user                           | `201 Created`               |
| `PUT`    | `/api/users/{id}` | Update a user                           | `200 OK`                    |
| `DELETE` | `/api/users/{id}` | Delete a user                           | `204 No Content`            |
| `GET`    | `/api/test-error` | Trigger a test exception in Development | `500 Internal Server Error` |

### Create a User

```http
POST /api/users
Authorization: Bearer <jwt-access-token>
Content-Type: application/json
```

```json
{
  "name": "Sara",
  "email": "sara@example.com"
}
```

A successful request returns `201 Created` and the created user, including its assigned ID.

### Retrieve Users with Pagination

```http
GET /api/users?page=1&pageSize=10
Authorization: Bearer <jwt-access-token>
```

The response includes the users on the requested page, the total number of users, the current page, and the page size.

```json
{
  "items": [
    {
      "id": 1,
      "name": "Sara",
      "email": "sara@example.com"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10
}
```

The default page is `1`, the default page size is `10`, and the maximum page size is `100`.

### Retrieve a User

```http
GET /api/users/1
Authorization: Bearer <jwt-access-token>
```

### Update a User

```http
PUT /api/users/1
Authorization: Bearer <jwt-access-token>
Content-Type: application/json
```

```json
{
  "name": "Sara Ahmed",
  "email": "sara.ahmed@example.com"
}
```

### Delete a User

```http
DELETE /api/users/1
Authorization: Bearer <jwt-access-token>
```

A successful deletion returns `204 No Content`.

## Validation and Error Handling

The API uses HTTP status codes to communicate the outcome of each request.

| Status Code                 | Meaning                                   | Example                            |
| --------------------------- | ----------------------------------------- | ---------------------------------- |
| `200 OK`                    | Request completed successfully            | Retrieving or updating a user      |
| `201 Created`               | Resource created successfully             | Creating a user                    |
| `204 No Content`            | Request succeeded without a response body | Deleting a user                    |
| `400 Bad Request`           | Invalid input                             | Missing name or invalid pagination |
| `401 Unauthorized`          | Authentication is missing or invalid      | Missing or invalid JWT             |
| `404 Not Found`             | Requested resource does not exist         | Retrieving a nonexistent user      |
| `409 Conflict`              | Request conflicts with existing data      | Duplicate email address            |
| `500 Internal Server Error` | Unexpected server-side error              | Unhandled exception                |

The application validates required fields, performs basic email-format validation, prevents duplicate email addresses, and checks pagination parameters.

Unexpected exceptions are logged internally and returned to clients as generic error responses.

## Middleware Pipeline

The application uses middleware to process incoming HTTP requests.

The pipeline is configured to:

1. Log incoming requests and outgoing response status codes.
2. Handle unexpected exceptions centrally.
3. Redirect HTTP requests to HTTPS when applicable.
4. Route requests to the matching endpoint.
5. Authenticate users using JWT Bearer tokens.
6. Enforce authorization requirements.
7. Execute the selected endpoint.

Authentication identifies the caller, while authorization determines whether the caller is permitted to access a protected resource.

## Testing

The project includes a `UserManagementAPI.http` file for manually testing endpoints from Visual Studio Code using the REST Client extension.

The test scenarios cover:

* Successful user creation, retrieval, update, and deletion.
* Missing or invalid input.
* Duplicate email addresses.
* Requests for nonexistent users.
* Pagination boundaries.
* Requests without a token or with an invalid token.
* Centralized exception handling.
* Request logging.

Run the application before sending requests. Use the HTTPS port displayed by `dotnet run` and obtain a development token before testing protected endpoints.

The user store is in-memory, so data is reset when the application restarts. Some tests depend on the order in which requests are executed.

## Current Limitations

This project is an educational implementation rather than a production-ready user management system.

* User records are stored in memory and are not persisted across restarts.
* The development token endpoint uses fixed credentials configured through User Secrets.
* Email validation checks format but does not verify email ownership.
* Authorization currently requires authentication but does not implement detailed roles or permission policies.
* Automated unit and integration tests are not included in this version.
* Production deployment, secret management, and database configuration have not been implemented.

## Future Improvements

Potential next steps include:

* Replace in-memory storage with Entity Framework Core and SQL Server or PostgreSQL.
* Add automated unit and integration tests.
* Integrate a proper identity provider for user registration, login, and token issuance.
* Add role-based or policy-based authorization.
* Add structured logging and production monitoring.
* Introduce database-level uniqueness constraints for email addresses.
* Add rate limiting and additional security controls.
* Configure production secrets and deployment settings.

## Learning Objectives

By completing this project, developers can practice:

* Building RESTful APIs with ASP.NET Core Minimal APIs.
* Implementing CRUD operations.
* Separating models, validation, and data access responsibilities.
* Handling HTTP status codes and validation errors.
* Implementing pagination.
* Creating custom middleware.
* Configuring JWT authentication and authorization.
* Testing APIs using HTTP requests.
