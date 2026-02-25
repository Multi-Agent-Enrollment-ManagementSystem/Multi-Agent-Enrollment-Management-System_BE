# MAEMS API - Role Management

## Tổng quan
API quản lý Role được xây dựng theo **Clean Architecture** với các design patterns:
- **CQRS Pattern** (Command Query Responsibility Segregation) với MediatR
- **Repository Pattern** 
- **Unit of Work Pattern**
- **Dependency Injection**
- **AutoMapper** cho object mapping

## Cấu trúc Project

```
MAEMS_BE/
├── MAEMS.Domain/               # Domain Layer (Entities, Interfaces)
│   ├── Entities/              # Domain entities
│   ├── Interfaces/            # Repository interfaces
│   └── Common/                # Shared types (BaseResponse, etc.)
│
├── MAEMS.Application/         # Application Layer (Business Logic)
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Features/              # CQRS Features
│   │   └── Roles/
│   │       └── Queries/       # Query handlers
│   └── Mappings/              # AutoMapper profiles
│
├── MAEMS.Infrastructure/      # Infrastructure Layer (Data Access)
│   ├── Models/                # EF Core generated models
│   └── Repositories/          # Repository implementations
│
└── MAEMS.API/                 # Presentation Layer (API Endpoints)
    ├── Controllers/           # API Controllers
    └── Middleware/            # Custom middleware
```

## API Endpoints

### 1. Get All Roles
**GET** `/api/roles`

Lấy tất cả roles trong hệ thống.

**Response:**
```json
{
  "success": true,
  "message": "Roles retrieved successfully",
  "data": [
    {
      "roleId": 1,
      "name": "Admin",
      "isActive": true
    },
    {
      "roleId": 2,
      "name": "User",
      "isActive": true
    }
  ],
  "errors": []
}
```

### 2. Get Role By ID
**GET** `/api/roles/{id}`

Lấy thông tin role theo ID.

**Parameters:**
- `id` (int): Role ID

**Response (Success):**
```json
{
  "success": true,
  "message": "Role retrieved successfully",
  "data": {
    "roleId": 1,
    "name": "Admin",
    "isActive": true
  },
  "errors": []
}
```

**Response (Not Found):**
```json
{
  "success": false,
  "message": "Role with ID 999 not found",
  "data": null,
  "errors": ["Role not found"]
}
```

### 3. Get Active Roles
**GET** `/api/roles/active`

Lấy danh sách roles đang hoạt động (isActive = true).

**Response:**
```json
{
  "success": true,
  "message": "Active roles retrieved successfully",
  "data": [
    {
      "roleId": 1,
      "name": "Admin",
      "isActive": true
    }
  ],
  "errors": []
}
```

## Design Patterns Được Sử Dụng

### 1. CQRS Pattern với MediatR
- **Queries**: GetAllRolesQuery, GetRoleByIdQuery, GetActiveRolesQuery
- **Handlers**: Xử lý logic nghiệp vụ độc lập
- Ưu điểm: Tách biệt read/write, dễ test, dễ mở rộng

### 2. Repository Pattern
```csharp
// Generic Repository
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    // ...
}

// Specific Repository
public interface IRoleRepository : IGenericRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
    Task<IEnumerable<Role>> GetActiveRolesAsync();
}
```

### 3. Unit of Work Pattern
```csharp
public interface IUnitOfWork : IDisposable
{
    IRoleRepository Roles { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### 4. Dependency Injection
- Tất cả dependencies được register trong `DependencyInjection.cs`
- Application Layer: MediatR, AutoMapper, FluentValidation
- Infrastructure Layer: DbContext, Repositories, UnitOfWork

## Cách chạy API

### 1. Cập nhật Connection String
Mở `MAEMS.API/appsettings.json` và cập nhật connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your_PostgreSQL_Connection_String"
  }
}
```

### 2. Build và Run
```bash
cd MAEMS.API
dotnet build
dotnet run
```

### 3. Truy cập Swagger UI
Mở trình duyệt và truy cập:
```
https://localhost:7xxx/swagger
```

## Testing với Postman

### Get All Roles
```
GET https://localhost:7xxx/api/roles
```

### Get Role By ID
```
GET https://localhost:7xxx/api/roles/1
```

### Get Active Roles
```
GET https://localhost:7xxx/api/roles/active
```

## Tính năng đã implement

✅ Clean Architecture (Domain, Application, Infrastructure, API)
✅ CQRS Pattern với MediatR
✅ Repository Pattern và Unit of Work
✅ Generic Repository
✅ AutoMapper cho object mapping
✅ Global Exception Handler Middleware
✅ BaseResponse wrapper cho tất cả responses
✅ CORS Configuration
✅ Swagger Documentation
✅ Dependency Injection
✅ Async/Await pattern
✅ Type aliases để tránh ambiguous references

## Mở rộng

### Thêm Command (Create, Update, Delete)
Tạo các Commands trong `MAEMS.Application/Features/Roles/Commands/`:
- CreateRoleCommand
- UpdateRoleCommand
- DeleteRoleCommand

### Thêm Validation
Sử dụng FluentValidation để validate requests:
```csharp
public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
```

### Thêm Logging
Inject ILogger vào handlers để log activities.

### Thêm Caching
Implement caching layer với Redis hoặc MemoryCache.

## NuGet Packages Đã Sử Dụng

### MAEMS.Application
- MediatR (14.0.0) - CQRS implementation
- AutoMapper.Extensions.Microsoft.DependencyInjection (12.0.1)
- FluentValidation.DependencyInjectionExtensions (12.1.1)

### MAEMS.Infrastructure
- Microsoft.EntityFrameworkCore.Design (8.0.10)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.4)

### MAEMS.API
- Swashbuckle.AspNetCore (6.6.2) - Swagger
- Microsoft.AspNetCore.OpenApi (8.0.19)
