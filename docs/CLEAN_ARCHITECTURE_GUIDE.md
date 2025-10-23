# 🏗️ Clean Architecture Implementation Guide - crmHuman

## 📚 Table of Contents
1. [Overview](#overview)
2. [Current Architecture](#current-architecture)
3. [Design Patterns Applied](#design-patterns-applied)
4. [Project Structure](#project-structure)
5. [Best Practices](#best-practices)
6. [Examples](#examples)

## 🎯 Overview

Project crmHuman đã được refactor theo **Clean Architecture** principles với các improvements sau:

### ✅ What's Improved
- ✓ **Result Pattern** - Better error handling
- ✓ **Service Interfaces** - Clear contracts
- ✓ **Separation of Concerns** - Clear layer boundaries
- ✓ **SOLID Principles** - Better maintainability
- ✓ **Code Organization** - Cleaner structure

### 🎨 Architecture Diagram

```
┌──────────────────────────────────────────────────────┐
│                  Presentation Layer                  │
│                    (crmHuman)                        │
│  ┌────────────┐  ┌──────────┐  ┌──────────────┐   │
│  │   Pages    │  │  Models  │  │ DisplayModels│   │
│  │ (Razor)    │  │  (DTO)   │  │ (ViewModels) │   │
│  └────────────┘  └──────────┘  └──────────────┘   │
└────────────────────┬─────────────────────────────────┘
                     │ Depends On ↓
┌────────────────────▼─────────────────────────────────┐
│              Business Logic Layer                    │
│            (VS.Human.Business)                       │
│  ┌─────────────┐  ┌────────────┐  ┌─────────────┐ │
│  │  Services   │  │  Interfaces│  │   Models    │ │
│  │ (IEmpBiz)   │  │(Contracts) │  │   (DTOs)    │ │
│  └─────────────┘  └────────────┘  └─────────────┘ │
└────────────────────┬─────────────────────────────────┘
                     │ Depends On ↓
┌────────────────────▼─────────────────────────────────┐
│             Data Access Layer                        │
│              (VS.Human.Rep)                          │
│  ┌──────────────┐  ┌────────────┐  ┌────────────┐ │
│  │ Repositories │  │ UnitOfWork │  │   Dapper   │ │
│  │  (Repos)     │  │  (Pattern) │  │   (ORM)    │ │
│  └──────────────┘  └────────────┘  └────────────┘ │
└────────────────────┬─────────────────────────────────┘
                     │ Depends On ↓
┌────────────────────▼─────────────────────────────────┐
│                 Domain Layer                         │
│              (VS.Human.Item)                         │
│  ┌──────────────┐  ┌────────────┐                  │
│  │   Entities   │  │   Models   │                  │
│  │  (Domain)    │  │  (Core)    │                  │
│  └──────────────┘  └────────────┘                  │
└──────────────────────────────────────────────────────┘
```

## 🔧 Design Patterns Applied

### 1. **Repository Pattern** ✅
**Purpose**: Abstraction layer giữa business logic và data access

**Implementation**:
```csharp
// Interface
public interface IEmployeeRep
{
    Task<bool> AddOrUpdate(Employee item);
    Task<Employee> GetById(int id);
    Task<BaseList> GetAll(EmployeeRequest request);
}

// Implementation
public class EmployeeRep : RepositoryBase<Employee>, IEmployeeRep
{
    // Concrete implementation với Dapper
}
```

**Benefits**:
- ✓ Testability - Easy to mock
- ✓ Flexibility - Easy to change data source
- ✓ Maintainability - Centralized data access logic

### 2. **Unit of Work Pattern** ✅
**Purpose**: Quản lý transactions và coordinate repositories

**Implementation**:
```csharp
public interface IUnitOfWork
{
    IEmployeeRep EmployeeRep { get; set; }
    ICandidateRep CandidateRep { get; set; }
    IOrderRep OrderRep { get; set; }
    // ... other repositories
}
```

**Benefits**:
- ✓ Transaction management
- ✓ Single point of access to repositories
- ✓ Consistency across operations

### 3. **Dependency Injection Pattern** ✅
**Purpose**: Inversion of Control, loose coupling

**Implementation**:
```csharp
// Registration in Ioc.cs
public static IServiceCollection Config(this IServiceCollection services)
{
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IEmpBusiness, EmployeeBusiness>();
    // ... other services
    return services;
}

// Usage in Pages
public class EmployeeInfoModel : BaseModel
{
    private readonly IEmpBusiness _empBusiness;
    
    public EmployeeInfoModel(IEmpBusiness empBusiness)
    {
        _empBusiness = empBusiness;
    }
}
```

**Benefits**:
- ✓ Loose coupling
- ✓ Easy testing
- ✓ Better maintainability

### 4. **Result Pattern** ✅ NEW
**Purpose**: Better error handling và meaningful responses

**Implementation**:
```csharp
// Success result
var result = Result<Employee>.Success(employee, "Employee retrieved successfully");

// Failure result
var result = Result.Failure("Employee not found", new List<string> { "Invalid ID" });

// Usage
if (result.IsSuccess)
{
    var employee = result.Data;
    // Process employee
}
else
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        // Log or display error
    }
}
```

**Benefits**:
- ✓ Explicit error handling
- ✓ Type-safe results
- ✓ Better API responses

### 5. **Service Layer Pattern** ✅
**Purpose**: Business logic encapsulation

**Implementation**:
```csharp
public interface IEmpBusiness
{
    Task<bool> Add(EmployeeInfoAdd item);
    Task<bool> Update(EmployeeInfoAdd item);
    Task<Employee> GetById(int id);
}

public class EmployeeBusiness : BaseBusiness, IEmpBusiness
{
    private readonly IUnitOfWork _unitOfWork;
    
    public EmployeeBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        : base(unitOfWork, httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
    }
    
    // Business logic implementation
}
```

### 6. **DTO Pattern** ✅
**Purpose**: Data transfer between layers

**Examples**:
- `EmployeeInfoAdd` - For adding employees
- `EmployeeDetailUpdate` - For updating employees
- `EmployeeIndexModel` - For listing employees

**Benefits**:
- ✓ Separation between domain and presentation
- ✓ Data validation
- ✓ Security (don't expose internal models)

## 📁 Project Structure

```
hcns/
├── crmHuman/                          # Presentation Layer
│   ├── Pages/                         # Razor Pages
│   │   ├── EmployeeInfo.cshtml        # View
│   │   └── EmployeeInfo.cshtml.cs     # PageModel (Controller logic)
│   ├── DisplayModel/                  # ViewModels
│   │   └── EmployeeDisplayEdit.cs
│   ├── Model/                         # DTOs for presentation
│   └── Program.cs                     # App startup & DI configuration
│
├── VS.Human.Business/                 # Business Logic Layer
│   ├── Imp/                           # Service implementations
│   │   └── EmployeeBusiness.cs
│   ├── Common/                        # Shared utilities
│   │   ├── Result.cs                  # Result pattern
│   │   └── IBaseService.cs            # Base service interface
│   ├── Model/                         # Business DTOs
│   │   └── EmployeeAdd.cs
│   ├── IEmpBusiness.cs                # Service interfaces
│   ├── BaseBusiness.cs                # Base service class
│   └── Ioc.cs                         # DI registration
│
├── VS.Human.Rep/                      # Data Access Layer
│   ├── EmployeeRep.cs                 # Repository implementation
│   ├── IEmployeeRep.cs                # Repository interface
│   ├── UnitOfWork.cs                  # Unit of Work implementation
│   ├── IUnitOfWork.cs                 # Unit of Work interface
│   ├── RepositoryBase.cs              # Base repository
│   └── Ioc.cs                         # DI registration
│
├── VS.Human.Item/                     # Domain Layer
│   └── Employee.cs                    # Domain entities
│
└── VS.Human.Utility/                  # Shared Utilities
    └── Common helper classes
```

## 📋 Best Practices

### 1. **Layer Independence**
```csharp
// ✅ GOOD - Depend on abstractions
public class EmployeeInfoModel
{
    private readonly IEmpBusiness _empBusiness;  // Interface
    
    public EmployeeInfoModel(IEmpBusiness empBusiness)
    {
        _empBusiness = empBusiness;
    }
}

// ❌ BAD - Direct dependency on implementation
public class EmployeeInfoModel
{
    private readonly EmployeeBusiness _empBusiness;  // Concrete class
}
```

### 2. **Single Responsibility**
```csharp
// ✅ GOOD - Each service has one responsibility
public class EmployeeBusiness : IEmpBusiness
{
    // Only employee-related business logic
}

public class CandidateBusiness : ICandidateBusiness
{
    // Only candidate-related business logic
}

// ❌ BAD - Mixed responsibilities
public class HRBusiness
{
    // Employee + Candidate + Orders logic mixed together
}
```

### 3. **Use DTOs for Data Transfer**
```csharp
// ✅ GOOD - Separate DTOs for each operation
public class EmployeeInfoAdd      // For adding
{
    public string FullName { get; set; }
    public string Phone { get; set; }
    // Only fields needed for adding
}

public class EmployeeDetailUpdate  // For updating
{
    public int Id { get; set; }
    public string FullName { get; set; }
    // Only fields that can be updated
}

// ❌ BAD - Reusing domain entity for everything
public async Task<bool> Add(Employee item) { }  // Don't expose domain directly
```

### 4. **Async/Await Properly**
```csharp
// ✅ GOOD - Proper async implementation
public async Task<Employee> GetById(int id)
{
    return await _unitOfWork.EmployeeRep.GetById(id);
}

// ❌ BAD - Blocking async operation
public async Task<Employee> GetById(int id)
{
    return _unitOfWork.EmployeeRep.GetById(id).Result;  // Don't use .Result
}
```

### 5. **Error Handling with Result Pattern**
```csharp
// ✅ GOOD - Using Result pattern
public async Task<Result<Employee>> GetByIdAsync(int id)
{
    try
    {
        if (id <= 0)
            return Result<Employee>.Failure("Invalid employee ID");
            
        var employee = await _unitOfWork.EmployeeRep.GetById(id);
        
        if (employee == null)
            return Result<Employee>.Failure("Employee not found");
            
        return Result<Employee>.Success(employee);
    }
    catch (Exception ex)
    {
        return Result<Employee>.Failure($"Error: {ex.Message}");
    }
}

// ❌ BAD - Throwing exceptions for flow control
public async Task<Employee> GetById(int id)
{
    if (id <= 0)
        throw new ArgumentException("Invalid ID");  // Don't throw for validation
        
    var employee = await _unitOfWork.EmployeeRep.GetById(id);
    
    if (employee == null)
        throw new NotFoundException("Not found");  // Don't throw for expected scenarios
        
    return employee;
}
```

## 💡 Examples

### Example 1: Complete CRUD Flow

```csharp
// 1. Presentation Layer (Page Model)
public class EmployeeInfoModel : PageModel
{
    private readonly IEmpBusiness _empBusiness;
    
    public EmployeeInfoModel(IEmpBusiness empBusiness)
    {
        _empBusiness = empBusiness;
    }
    
    public async Task<IActionResult> OnPostUpdate(EmployeeDetailUpdate request)
    {
        // Validation
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        // Call business layer
        var result = await _empBusiness.Update(request);
        
        if (!result)
            return BadRequest("Update failed");
            
        return Ok(new { success = true });
    }
}

// 2. Business Layer
public class EmployeeBusiness : BaseBusiness, IEmpBusiness
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<bool> Update(EmployeeInfoAdd itemUpdate)
    {
        // Business validation
        if (string.IsNullOrEmpty(itemUpdate.Phone))
            return false;
            
        // Map DTO to Entity
        var item = new Employee
        {
            Id = itemUpdate.Id,
            FullName = itemUpdate.FullName,
            Phone = itemUpdate.Phone,
            // ... other fields
            UpdatedBy = GetUserId(),
            UpdateAt = DateTime.Now
        };
        
        // Call repository
        return await _unitOfWork.EmployeeRep.AddOrUpdate(item);
    }
}

// 3. Repository Layer
public class EmployeeRep : RepositoryBase<Employee>, IEmployeeRep
{
    public async Task<bool> AddOrUpdate(Employee item)
    {
        if (item.Id > 0)
        {
            // Update logic
            return await Update(item);
        }
        // Add logic
        return await Add(item);
    }
    
    private async Task<bool> Update(Employee item)
    {
        var parameter = new
        {
            item.Id,
            item.FullName,
            item.Phone,
            // ... all fields
        };
        
        return await ExecuteSQL("sp_emp_update", parameter);
    }
}
```

## 🎯 Migration Guide

Để migrate existing code sang Clean Architecture:

### Step 1: Identify Layers
```
Current Code → Target Layer
- Razor Pages → Presentation
- Business classes → Business Logic
- Repository classes → Data Access
- Entity classes → Domain
```

### Step 2: Create Interfaces
```csharp
// For every service, create an interface
public interface IEmployeeBusiness
{
    Task<bool> Add(EmployeeInfoAdd item);
    Task<bool> Update(EmployeeInfoAdd item);
}
```

### Step 3: Register in DI
```csharp
services.AddScoped<IEmployeeBusiness, EmployeeBusiness>();
```

### Step 4: Update Consumers
```csharp
// Change from
private readonly EmployeeBusiness _empBusiness;

// To
private readonly IEmpBusiness _empBusiness;
```

## 📈 Benefits Achieved

### Before Clean Architecture
- ❌ Tight coupling between layers
- ❌ Hard to test
- ❌ Mixed responsibilities
- ❌ Poor error handling
- ❌ Difficult to maintain

### After Clean Architecture
- ✅ Loose coupling
- ✅ Easy to test (can mock interfaces)
- ✅ Clear separation of concerns
- ✅ Better error handling with Result pattern
- ✅ Easy to maintain and extend
- ✅ SOLID principles applied
- ✅ Better code organization

## 🚀 Next Steps

1. **Add Unit Tests** - Test business logic in isolation
2. **Add Validation Layer** - FluentValidation for DTOs
3. **Add Caching** - Improve performance
4. **Add Logging** - Structured logging with Serilog
5. **Add API Documentation** - Swagger/OpenAPI
6. **Performance Optimization** - Query optimization, async patterns

## 📚 Resources

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microsoft - Clean Architecture](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [SOLID Principles](https://www.digitalocean.com/community/conceptual_articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design)

---

**Created**: 2025-10-23  
**Version**: 1.0  
**Status**: ✅ Implemented & Documented

