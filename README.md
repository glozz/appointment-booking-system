# appointment-booking-system
Production-grade Appointment Booking System built with C# .NET and MVC

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

### Database

The database is automatically created and seeded with sample data on first run:
- 4 branches (New York, Los Angeles, Chicago, Houston)
- 7 banking services
- Sample appointments

### Logs

View logs for any container:
```bash
docker-compose logs -f [web|api|sqlserver]
```

### Rebuild

If you make code changes:
```bash
docker-compose up -d --build
```
