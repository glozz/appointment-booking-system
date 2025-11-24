# Appointment Booking System

A production-ready appointment booking system built with C# .NET 8 featuring complete authentication, role-based access control, and REST API.

## Features

### Authentication & Security
- **User Registration** with email validation and password requirements
- **JWT Authentication** with access tokens and refresh tokens
- **Email Verification** with expiring tokens (24 hours)
- **Password Reset** functionality with secure tokens (1 hour expiry)
- **Password Hashing** using bcrypt with cost factor 12
- **Session Management** with auto-expiration and multiple device support
- **Account Lockout** after 5 failed login attempts (15 minutes)
- **Role-Based Access Control (RBAC)** with Admin and User roles

### Password Requirements
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character

### API Endpoints

#### Authentication (`/api/auth`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/register` | Register a new user |
| POST | `/login` | Login with credentials |
| POST | `/logout` | Logout current session |
| POST | `/refresh` | Refresh access token |
| POST | `/verify-email` | Verify email address |
| POST | `/forgot-password` | Request password reset |
| POST | `/reset-password` | Reset password with token |

#### Users (`/api/users`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/profile` | Get current user's profile |
| PUT | `/profile` | Update profile |
| PUT | `/change-password` | Change password |
| GET | `/sessions` | List active sessions |
| DELETE | `/sessions/{id}` | Revoke specific session |
| DELETE | `/sessions` | Revoke all other sessions |
| GET | `/login-history` | View login history |

#### Admin Users (`/api/admin/users`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List users (paginated, searchable) |
| GET | `/{id}` | Get user details |
| PUT | `/{id}` | Update user |
| POST | `/{id}/activate` | Activate user |
| POST | `/{id}/deactivate` | Deactivate user |
| DELETE | `/{id}` | Soft delete user |
| PUT | `/{id}/role` | Change user role |
| POST | `/{id}/unlock` | Unlock locked account |
| GET | `/{id}/activity` | View user activity |

#### Admin Dashboard (`/api/admin`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/dashboard` | Get dashboard statistics |

#### Dashboard (`/api/dashboard`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get user dashboard data |

#### Notifications (`/api/notifications`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get notifications |
| GET | `/unread-count` | Get unread count |
| PUT | `/{id}/read` | Mark as read |
| PUT | `/read-all` | Mark all as read |
| DELETE | `/{id}` | Delete notification |

#### Appointments (`/api/appointments`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List all appointments |
| POST | `/` | Create appointment |
| GET | `/{id}` | Get appointment |
| PUT | `/{id}` | Update appointment |
| POST | `/{id}/cancel` | Cancel appointment |

#### System
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check |
| GET | `/api/appointment-types` | List appointment types |

### Default Admin Account
- Email: `admin@appointmentbooking.com`
- Password: `Admin@123` (default, should be changed in production)

> **Security Note:** Set the `ADMIN_DEFAULT_PASSWORD` environment variable to override the default admin password in production environments.

## Project Structure

```
src/
├── AppointmentBooking.Core/        # Domain entities, enums, interfaces
├── AppointmentBooking.Application/ # Business logic, DTOs, services
├── AppointmentBooking.Infrastructure/ # Data access, repositories
├── AppointmentBooking.API/         # REST API controllers
└── AppointmentBooking.Web/         # MVC Web application

tests/
└── AppointmentBooking.Tests/       # Unit tests
```

## Docker Deployment

### Prerequisites
- Docker Desktop installed
- Docker Compose installed

### Quick Start

1. **Clone the repository:**
   ```bash
   git clone https://github.com/glozz/appointment-booking-system.git
   cd appointment-booking-system
   ```

2. **Run with Docker Compose:**
   ```bash
   docker-compose up -d
   ```

3. **Access the applications:**
   - Web UI: http://localhost:5001
   - REST API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger

4. **Stop the containers:**
   ```bash
   docker-compose down
   ```

### Container Details

- **SQL Server**: Port 1433 (password: YourStrong@Passw0rd)
- **REST API**: Port 5000
- **Web MVC**: Port 5001

## Local Development

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)

### Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/glozz/appointment-booking-system.git
   cd appointment-booking-system
   ```

2. **Restore packages:**
   ```bash
   dotnet restore
   ```

3. **Update connection string** in `appsettings.json`

4. **Run the API:**
   ```bash
   cd src/AppointmentBooking.API
   dotnet run
   ```

5. **Run tests:**
   ```bash
   dotnet test
   ```

## Configuration

### JWT Settings (`appsettings.json`)
```json
{
  "Jwt": {
    "Key": "YourSecretKeyAtLeast32CharactersLong!",
    "Issuer": "AppointmentBooking",
    "Audience": "AppointmentBooking",
    "ExpiryMinutes": 60
  }
}
```

### Environment Variables
- `ConnectionStrings__DefaultConnection` - Database connection string
- `Jwt__Key` - JWT signing key
- `App__BaseUrl` - Base URL for email links
- `ADMIN_DEFAULT_PASSWORD` - Default admin password (overrides hardcoded default)

## Security Features

- Password hashing with bcrypt (cost factor 12)
- JWT tokens with configurable expiry
- Refresh token rotation
- Account lockout protection
- Session management with revocation
- Soft delete for data retention
- Activity logging for audit trail
- Input validation with FluentValidation
- SQL injection prevention via Entity Framework

## Testing

The project includes comprehensive unit tests for:
- Authentication service (registration, login, password management)
- User service (profile, sessions, admin operations)

Run tests:
```bash
dotnet test --verbosity normal
```

## Database

The database is automatically created and seeded with:
- Admin user account
- 4 branches with operating hours
- 9 banking services
- 4 appointment types

### Database Tables
- `Users` - User accounts with authentication data
- `Sessions` - Active user sessions
- `ActivityLogs` - Audit trail
- `Notifications` - User notifications
- `AppointmentTypes` - Types of appointments
- `Appointments` - Booked appointments
- `Customers` - Customer information
- `Branches` - Branch locations
- `Services` - Available services

## Logs

View logs for any container:
```bash
docker-compose logs -f [web|api|sqlserver]
```

## Rebuild

If you make code changes:
```bash
docker-compose up -d --build
```

## License

MIT License
