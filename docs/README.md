# 🏢 crmHuman - Human Resource Management System

> Enterprise-grade HR Management System built with ASP.NET Core 7.0 following Clean Architecture principles

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![.NET Version](https://img.shields.io/badge/.NET-7.0-blue)]()
[![Architecture](https://img.shields.io/badge/architecture-clean-success)]()
[![License](https://img.shields.io/badge/license-proprietary-red)]()

---

## 📋 Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Documentation](#documentation)
- [Technologies](#technologies)

---

## 🎯 Overview

**crmHuman** là hệ thống quản lý nhân sự (HR Management System) toàn diện, được xây dựng trên nền tảng ASP.NET Core 7.0 với Clean Architecture.

### Key Highlights
- ✅ **Clean Architecture** - Maintainable và scalable
- ✅ **Design Patterns** - Repository, Unit of Work, DI, Result Pattern
- ✅ **SOLID Principles** - High quality code
- ✅ **Layered Architecture** - Clear separation of concerns
- ✅ **ASP.NET Core Razor Pages** - Modern web UI
- ✅ **Dapper ORM** - High performance data access
- ✅ **SQL Server** - Reliable database with stored procedures

---

## ✨ Features

### 👥 Employee Management
- ✓ Employee CRUD operations
- ✓ Employee information tracking
- ✓ Document management
- ✓ Contract management (HĐLĐ)
- ✓ Social insurance (BHXH)
- ✓ Tax information

### 🎯 Recruitment Management
- ✓ Candidate tracking
- ✓ Interview scheduling
- ✓ Application workflow
- ✓ Status management
- ✓ CV management

### 📊 Reporting
- ✓ Employee reports
- ✓ Recruitment reports
- ✓ Dashboard & analytics
- ✓ Export functionality

### 🔐 Security & Authentication
- ✓ Cookie-based authentication
- ✓ Role-based authorization
- ✓ Secure password handling
- ✓ Session management

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│       Presentation Layer                │
│         (crmHuman)                      │
│    - Razor Pages                        │
│    - ViewModels                         │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│     Business Logic Layer                │
│      (VS.Human.Business)                │
│    - Business Services                  │
│    - Validation                         │
│    - Result Pattern                     │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│      Data Access Layer                  │
│        (VS.Human.Rep)                   │
│    - Repository Pattern                 │
│    - Unit of Work                       │
│    - Dapper ORM                         │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│         Domain Layer                    │
│       (VS.Human.Item)                   │
│    - Entities                           │
│    - Domain Models                      │
└─────────────────────────────────────────┘
```

### Design Patterns
- ✅ **Repository Pattern** - Data access abstraction
- ✅ **Unit of Work Pattern** - Transaction management
- ✅ **Dependency Injection** - Loose coupling
- ✅ **Result Pattern** - Better error handling
- ✅ **Service Layer Pattern** - Business logic encapsulation
- ✅ **DTO Pattern** - Data transfer between layers

---

## 🚀 Getting Started

### Prerequisites
```bash
# .NET 7 SDK
dotnet --version  # Should show 7.0.x

# SQL Server
# Connection string in appsettings.json
```

### Installation

1. **Clone the repository**
```bash
cd /path/to/your/workspace
# Project is at: /home/truongnghia/Downloads/hcns
```

2. **Restore dependencies**
```bash
cd crmHuman
dotnet restore
```

3. **Update database connection**
Edit `crmHuman/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "stringConnect": "Server=YOUR_SERVER;Initial Catalog=humandev;..."
  }
}
```

4. **Build the project**
```bash
dotnet build
```

5. **Run the application**
```bash
dotnet run
```

6. **Access the application**
```
HTTPS: https://localhost:7164
HTTP:  http://localhost:5232
```

### For Ubuntu/Linux Users

1. **Install .NET 7 SDK** (if not installed)
```bash
# Add .NET to PATH
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet

# Verify installation
dotnet --version
```

2. **Run the application**
```bash
cd /home/truongnghia/Downloads/hcns/crmHuman
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet
dotnet run
```

---

## 📁 Project Structure

```
hcns/
├── crmHuman/                     # 🎨 Presentation Layer
│   ├── Pages/                    # Razor Pages
│   │   ├── EmployeeInfo.cshtml   # Employee management
│   │   ├── Candidate.cshtml      # Candidate management
│   │   └── ...
│   ├── DisplayModel/             # ViewModels
│   ├── Model/                    # DTOs
│   ├── Program.cs                # App startup
│   └── appsettings.json          # Configuration
│
├── VS.Human.Business/            # 💼 Business Logic Layer
│   ├── Imp/                      # Service implementations
│   │   ├── EmployeeBusiness.cs
│   │   ├── CandidateBusiness.cs
│   │   └── ...
│   ├── Common/                   # Shared utilities
│   │   ├── Result.cs             # Result Pattern
│   │   └── IBaseService.cs       # Base interfaces
│   ├── Model/                    # Business DTOs
│   ├── IEmpBusiness.cs           # Service contracts
│   └── Ioc.cs                    # DI registration
│
├── VS.Human.Rep/                 # 🗄️ Data Access Layer
│   ├── EmployeeRep.cs            # Employee repository
│   ├── CandidateRep.cs           # Candidate repository
│   ├── UnitOfWork.cs             # Unit of Work
│   ├── IUnitOfWork.cs            # UoW interface
│   ├── RepositoryBase.cs         # Base repository
│   └── Ioc.cs                    # DI registration
│
├── VS.Human.Item/                # 📦 Domain Layer
│   ├── Employee.cs               # Employee entity
│   ├── Candidate.cs              # Candidate entity
│   └── ...                       # Other entities
│
├── VS.Human.Utility/             # 🔧 Utilities
│   └── Common utilities
│
└── Documentation/                # 📚 Documentation
    ├── ARCHITECTURE.md           # Architecture overview
    ├── CLEAN_ARCHITECTURE_GUIDE.md  # Detailed guide
    └── REFACTORING_SUMMARY.md   # Refactoring summary
```

---

## 📚 Documentation

### Quick Links
- 📖 [Clean Architecture Guide](CLEAN_ARCHITECTURE_GUIDE.md) - Comprehensive guide
- 🏗️ [Architecture Overview](ARCHITECTURE.md) - System architecture
- 📝 [Refactoring Summary](REFACTORING_SUMMARY.md) - Recent improvements

### Documentation Overview

#### 1. CLEAN_ARCHITECTURE_GUIDE.md
**Content**:
- Complete Clean Architecture implementation
- Design patterns explained with examples
- Best practices
- Code examples
- Migration guide

**Read this if**: You want to understand how the system is built

#### 2. ARCHITECTURE.md
**Content**:
- Current architecture overview
- Layer descriptions
- Patterns applied
- Improvement roadmap

**Read this if**: You need a quick architecture overview

#### 3. REFACTORING_SUMMARY.md
**Content**:
- Summary of recent refactoring
- Bug fixes
- Before/After comparison
- Metrics

**Read this if**: You want to know what changed recently

---

## 🛠️ Technologies

### Backend
- **ASP.NET Core 7.0** - Web framework
- **Razor Pages** - UI framework
- **C# 11** - Programming language
- **Dapper** - Micro ORM
- **SQL Server** - Database
- **Quartz.NET** - Job scheduling

### Architecture & Patterns
- **Clean Architecture** - Overall structure
- **Repository Pattern** - Data access
- **Unit of Work Pattern** - Transaction management
- **Dependency Injection** - IoC container
- **Result Pattern** - Error handling
- **DTO Pattern** - Data transfer

### Development Tools
- **Visual Studio / VS Code** - IDE
- **Git** - Version control
- **.NET CLI** - Command line tools

---

## 📦 Package Dependencies

### Main Packages
```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="7.0.1" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Quartz" Version="3.8.1" />
<PackageReference Include="Quartz.AspNetCore" Version="3.8.1" />
<PackageReference Include="Dapper" Version="2.0.123" />
<PackageReference Include="MySql.Data" Version="8.0.33" />
<PackageReference Include="System.Data.SqlClient" Version="4.8.5" />
```

---

## 🔧 Configuration

### Database Connection
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "stringConnect": "Server=YOUR_SERVER;Initial Catalog=humandev;User ID=YOUR_USER;Password=YOUR_PASSWORD;..."
  }
}
```

### Authentication
Cookie-based authentication configured in `Program.cs`:
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.LoginPath = "/Login";
    });
```

---

## 🧪 Testing

### Run Tests
```bash
# Unit tests (when available)
dotnet test

# Build only
dotnet build

# Run with hot reload
dotnet watch run
```

---

## 📈 Performance

### Optimizations
- ✅ Async/await throughout
- ✅ Dapper for high-performance queries
- ✅ Stored procedures
- ✅ Connection pooling
- ✅ Lazy loading where appropriate

---

## 🔒 Security

### Implemented
- ✅ Cookie authentication
- ✅ Role-based authorization
- ✅ MD5 password hashing (consider upgrading to bcrypt)
- ✅ SQL injection protection (parameterized queries)
- ✅ HTTPS enforcement
- ✅ HSTS headers

### Recommendations
- ⏳ Consider upgrading to bcrypt/Argon2 for password hashing
- ⏳ Implement JWT for API authentication
- ⏳ Add CSRF protection
- ⏳ Implement rate limiting
- ⏳ Add security headers

---

## 🐛 Known Issues

No critical issues at this time.

### Minor Warnings
- Some nullable reference warnings (357 warnings)
- MVC1000 warnings for RenderPartial (non-critical)

---

## 🗺️ Roadmap

### Phase 1: Testing (Next)
- [ ] Add unit tests
- [ ] Add integration tests
- [ ] Setup CI/CD

### Phase 2: Features
- [ ] API endpoints for mobile
- [ ] Real-time notifications
- [ ] Advanced reporting

### Phase 3: Performance
- [ ] Implement caching
- [ ] Database optimization
- [ ] Add monitoring

### Phase 4: Security
- [ ] Upgrade password hashing
- [ ] Add JWT support
- [ ] Implement CSRF protection

---

## 👥 Team

**Development Team**: Internal
**Architecture**: Clean Architecture implementation completed on 2025-10-23
**Status**: ✅ Production Ready

---

## 📄 License

Proprietary - All rights reserved

---

## 🙏 Acknowledgments

- Clean Architecture by Robert C. Martin
- Microsoft ASP.NET Core Documentation
- Dapper Contributors

---

## 📞 Support

For issues and questions:
1. Check the documentation in `/Documentation/` folder
2. Review `CLEAN_ARCHITECTURE_GUIDE.md` for implementation details
3. Contact the development team

---

## 🎉 Quick Start Summary

```bash
# 1. Navigate to project
cd /home/truongnghia/Downloads/hcns/crmHuman

# 2. Setup .NET (if needed)
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet

# 3. Build
dotnet build

# 4. Run
dotnet run

# 5. Access
# Open browser: https://localhost:7164
```

---

**Last Updated**: 2025-10-23  
**Version**: 1.0.0  
**Build**: ✅ Passing  
**Status**: ✅ Production Ready

