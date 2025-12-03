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
- 6 consultants per branch with unique South African names

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
- `Consultants` - Branch consultants who handle appointments

## Booking Rules

The appointment booking system enforces the following rules:

### Operating Hours
- Default operating hours: **08:00 - 17:00**
- Branch-specific operating hours are respected when configured
- Bookings cannot be made outside operating hours or on closed days

### Time Slots
- All appointments must be scheduled on **15-minute increments** (e.g., 09:00, 09:15, 09:30, 09:45)
- Appointment duration is determined by the selected service

### Multi-Consultant Booking
- Multiple customers can book the same time slot at a branch (up to the number of available consultants)
- A unique index on `(ConsultantId, AppointmentDate, StartTime)` prevents a single consultant from being double-booked
- Server-side validation checks consultant availability before accepting a booking

### Consultant Auto-Assignment
- When creating an appointment, a consultant is automatically assigned from the selected branch
- A consultant is considered available if they have no overlapping appointments in the requested time window
- Overlap detection uses robust logic: `existing.Start < requestedEnd && requestedStart < existing.End`
- If no consultants are available, the booking is rejected with an appropriate error message

### Real-Time Availability Wizard
The booking wizard provides real-time feedback on appointment availability:
- **Step-by-step wizard** guides users through selecting branch, date, and time
- **Color-coded availability indicators** show slot status:
  - 🟢 **Green** - High availability (more than 50% of consultants available)
  - 🟡 **Yellow** - Limited availability (50% or fewer consultants available)
  - 🟠 **Orange/Red** - Last slot! (only 1 consultant available)
- **Mobile-responsive design** adapts to all screen sizes
- Slots are only shown if at least one consultant is available

### Customer Details
- Customer information (First Name, Last Name, Email, Phone) is pre-populated from the logged-in user's account
- Customer fields are rendered as readonly on the booking form
- Server-side validation re-resolves customer information from the authenticated user to prevent tampering

## Consultant Seeding

At application startup, the system automatically seeds 6 consultants per branch:
- Each consultant has a unique South African first name: **Thabo, Sipho, Nomsa, Lerato, Kabelo, Ayanda**
- The surname matches the branch name (e.g., "Thabo Sandton" for Sandton City Branch)
- The seeder is idempotent: it only adds consultants if fewer than 6 exist for a branch
- Consultants are stored in the `Consultants` table with a foreign key to their branch

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

## Authentication Integration

The MVC web application integrates with the API's authentication system using cookies to bridge server-rendered pages with the API-first authentication.

### Architecture Overview

1. **AccountController**: Handles login, register, and logout actions by posting to the API endpoints (`/api/auth/login`, `/api/auth/register`, `/api/auth/logout`).
2. **AccessTokenHandler**: A `DelegatingHandler` that automatically attaches access tokens to outgoing API requests and handles token refresh on 401 responses.
3. **Cookie Authentication**: The web app uses ASP.NET Core cookie authentication to maintain user sessions.

### Cookie Settings

The application uses secure, HttpOnly cookies with the following configuration:

| Cookie | Purpose | Settings |
|--------|---------|----------|
| `access_token` | JWT access token for API calls | HttpOnly, Secure, SameSite=Strict |
| `refresh_token` | Token for refreshing access tokens | HttpOnly, Secure, SameSite=Strict |
| `AppointmentBooking.Auth` | ASP.NET Core authentication cookie | HttpOnly, Secure, SameSite=Strict |

### Token Refresh Contract

The `AccessTokenHandler` automatically handles token refresh:

1. Attaches `access_token` cookie value as Bearer token to outgoing requests
2. On 401 Unauthorized response, attempts refresh via `POST /api/auth/refresh`
3. Updates cookies with new tokens on successful refresh
4. Retries the original request with the new access token

**Expected API Response Format for `/api/auth/refresh`:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "accessTokenExpiryMinutes": 60,
  "refreshTokenExpiryDays": 7
}
```

### Secure Cookie Requirements

For production deployments:

- **HTTPS Required**: Cookies are configured with `Secure` flag, requiring HTTPS
- **SameSite=Strict**: Prevents CSRF attacks by not sending cookies on cross-site requests
- **HttpOnly**: Prevents JavaScript access to tokens, mitigating XSS attacks

### Environment Variables

Configure the following for production:

| Variable | Description | Example |
|----------|-------------|---------|
| `ApiSettings__BaseUrl` | Base URL of the authentication API | `https://api.example.com` |
| `ConnectionStrings__DefaultConnection` | Database connection string | (see appsettings) |

### Development Setup

1. Ensure the API is running on the configured `ApiSettings:BaseUrl` (default: `http://localhost:5000`)
2. Update `appsettings.json` or set environment variables
3. Run the web application

### Security Recommendations

1. **Always use HTTPS** in production
2. **Set strong JWT keys** via environment variables
3. **Configure proper CORS** policies on the API
4. **Enable HSTS** (HTTP Strict Transport Security)
5. **Rotate refresh tokens** on each use
6. **Set appropriate token expiry** times based on security requirements
