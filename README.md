# 🛒 KeystoneCommerce

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-512BD4?style=for-the-badge&logo=dotnet)

**KeystoneCommerce** is a modern, full-featured e-commerce platform built with ASP.NET Core 9.0, following Clean Architecture principles and industry best practices. It provides a robust foundation for building scalable online stores with enterprise-grade features including payment processing, inventory management, and comprehensive order management.

## ✨ Key Features

### 🛍️ **Customer Features**
- **Product Catalog**: Browse products with advanced filtering, search, and sorting capabilities
- **Shopping Cart**: Real-time cart management with session persistence
- **User Reviews**: Rate and review products with authenticated user feedback
- **Secure Checkout**: Multi-step checkout process with address validation
- **Order Tracking**: Complete order history and real-time status updates
- **Coupon System**: Apply discount coupons at checkout
- **Multiple Shipping Methods**: Choose from various shipping options

### 💳 **Payment & Processing**
- **Stripe Integration**: Secure payment processing with Stripe
- **Payment Tracking**: Comprehensive payment history and status monitoring
- **Inventory Reservations**: Automatic inventory locking during checkout
- **Refund Support**: Built-in payment refund capabilities

### 📊 **Admin Features**
- **Dashboard**: Comprehensive analytics and business insights
- **Product Management**: Full CRUD operations for products and galleries
- **Order Management**: Process, track, and manage customer orders
- **Banner Management**: Control promotional banners and campaigns
- **User Management**: ASP.NET Core Identity integration
- **Background Jobs**: Automated tasks with Hangfire

### 🔧 **Technical Features**
- **Clean Architecture**: Well-organized, maintainable codebase
- **Repository Pattern**: Data access abstraction layer
- **AutoMapper**: Seamless object-to-object mapping
- **FluentValidation**: Robust input validation
- **Serilog**: Structured logging to console and SQL Server
- **Hangfire**: Background job processing and scheduling
- **HTML Sanitization**: XSS protection with HtmlSanitizer
- **Global Exception Handling**: Centralized error management
- **Request Logging**: Comprehensive request/response logging

## 🏗️ Architecture

KeystoneCommerce follows **Clean Architecture** principles with clear separation of concerns:

```
KeystoneCommerce/
├── KeystoneCommerce.Domain/          # Enterprise business rules and entities
│   ├── Entities/                     # Domain entities (Product, Order, Payment, etc.)
│   └── Enums/                        # Domain enumerations
│
├── KeystoneCommerce.Application/     # Application business rules
│   ├── Services/                     # Business logic services
│   ├── Interfaces/                   # Service contracts
│   ├── DTOs/                         # Data transfer objects
│   ├── Notifications/                # Notification contracts
│   └── Common/                       # Shared application concerns
│
├── KeystoneCommerce.Infrastructure/  # External concerns
│   ├── Persistence/                  # Database context and configurations
│   ├── Repositories/                 # Data access implementations
│   ├── Services/                     # Infrastructure services (Email, Image, Identity)
│   ├── Validation/                   # FluentValidation validators
│   └── Migrations/                   # EF Core migrations
│
├── KeystoneCommerce.WebUI/           # Presentation layer
│   ├── Controllers/                  # MVC Controllers
│   ├── Views/                        # Razor views
│   ├── ViewModels/                   # View-specific models
│   ├── Middlewares/                  # Custom middleware
│   ├── Filters/                      # Action filters
│   └── wwwroot/                      # Static assets
│
├── KeystoneCommerce.Shared/          # Shared utilities and constants
│
└── KeystoneCommerce.Tests/           # Unit and integration tests
```

## 🚀 Technology Stack

### **Backend**
- **Framework**: ASP.NET Core 9.0 (MVC)
- **Language**: C# with .NET 9.0
- **ORM**: Entity Framework Core 9.0
- **Database**: Microsoft SQL Server
- **Authentication**: ASP.NET Core Identity
- **Validation**: FluentValidation 12.0
- **Logging**: Serilog 9.0
- **Mapping**: AutoMapper 15.0
- **Background Jobs**: Hangfire 1.8.22

### **Payment Integration**
- **Stripe.NET**: 50.0.0 for payment processing

### **Frontend**
- **Bootstrap Icons**: Modern icon library
- **jQuery**: Client-side interactions
- **jQuery Validation**: Form validation

### **Security**
- **HtmlSanitizer**: 9.0.886 for XSS protection
- **HTTPS**: Enforced secure connections
- **SQL Injection Protection**: Parameterized queries via EF Core
- **CSRF Protection**: Built-in anti-forgery tokens

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 9.0 SDK** or later ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **SQL Server** (LocalDB, Express, or full version)
- **Visual Studio 2022** (17.8+) or **Visual Studio Code** with C# extension
- **Git** for version control
- **Node.js** (optional, for frontend package management via libman)

## 🛠️ Installation & Setup

### 1. **Clone the Repository**

```bash
git clone https://github.com/Zyad-Eltayabi/KeystoneCommerce.git
cd KeystoneCommerce
```

### 2. **Configure Database Connection**

Update the connection string in `KeystoneCommerce.WebUI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=KeystoneCommerce;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

**For LocalDB:**
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KeystoneCommerce;Trusted_Connection=True;MultipleActiveResultSets=true"
```

### 3. **Configure Application Settings**

Update additional settings in `appsettings.json` or `appsettings.Development.json`:

**Email Configuration** (for notifications):
```json
"EmailOptions": {
  "SenderEmail": "your-email@example.com",
  "SenderName": "KeystoneCommerce",
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-email@example.com",
  "SmtpPassword": "your-app-password"
}
```

**Stripe Configuration** (for payments):
```json
"StripeSettings": {
  "PublishableKey": "pk_test_your_publishable_key",
  "SecretKey": "sk_test_your_secret_key",
  "WebhookSecret": "whsec_your_webhook_secret"
}
```

**Inventory Settings**:
```json
"InventoryOptions": {
  "ReservationExpiryMinutes": 15,
  "LowStockThreshold": 10
}
```

### 4. **Restore Dependencies**

```bash
dotnet restore
```

### 5. **Apply Database Migrations**

Navigate to the Infrastructure project directory and apply migrations:

```bash
cd KeystoneCommerce.Infrastructure
dotnet ef database update --startup-project ../KeystoneCommerce.WebUI
```

Or from the solution root:

```bash
dotnet ef database update --project KeystoneCommerce.Infrastructure --startup-project KeystoneCommerce.WebUI
```

### 6. **Build the Solution**

```bash
dotnet build
```

### 7. **Run the Application**

```bash
cd KeystoneCommerce.WebUI
dotnet run
```

The application will start and be available at:
- **HTTPS**: https://localhost:7001
- **HTTP**: http://localhost:5001

## 🎮 Running the Application

### **Development Mode**

```bash
dotnet run --project KeystoneCommerce.WebUI
```

### **Production Mode**

```bash
dotnet run --project KeystoneCommerce.WebUI --configuration Release
```

### **Watch Mode** (auto-reload on changes)

```bash
dotnet watch --project KeystoneCommerce.WebUI
```

### **Accessing Hangfire Dashboard**

Once the application is running, access the Hangfire dashboard at:
```
https://localhost:7001/hangfire
```

## 🧪 Testing

Run all tests in the solution:

```bash
dotnet test
```

Run tests with detailed output:

```bash
dotnet test --verbosity detailed
```

Run tests with coverage:

```bash
dotnet test /p:CollectCoverage=true
```

## 📦 Project Structure Details

### **Domain Layer** (`KeystoneCommerce.Domain`)
Contains enterprise business logic and entities:
- `Product`: Product catalog entities
- `Order`: Order management entities
- `Payment`: Payment processing entities
- `Review`: Product review entities
- `Banner`: Marketing banner entities
- `Coupon`: Discount coupon entities
- `ShippingAddress` & `ShippingMethod`: Shipping management
- `InventoryReservation`: Inventory locking

### **Application Layer** (`KeystoneCommerce.Application`)
Contains application-specific business rules:
- Service interfaces and implementations
- DTOs for data transfer
- Notification contracts
- Common settings and utilities

### **Infrastructure Layer** (`KeystoneCommerce.Infrastructure`)
Handles external concerns:
- **Persistence**: `ApplicationDbContext` with EF Core
- **Repositories**: Generic and specific repositories
- **Services**: Email, Image processing, Identity, Stripe integration
- **Validation**: FluentValidation rules
- **Hangfire**: Background job configuration

### **Presentation Layer** (`KeystoneCommerce.WebUI`)
MVC application with:
- Controllers for handling requests
- Razor views for UI rendering
- ViewModels for view-specific data
- Middlewares for cross-cutting concerns
- Static assets (CSS, JS, images)

## 🔐 Security Features

- **Authentication & Authorization**: ASP.NET Core Identity
- **XSS Protection**: HtmlSanitizer for user-generated content
- **CSRF Protection**: Anti-forgery tokens on all forms
- **SQL Injection Protection**: Parameterized queries via EF Core
- **Secure Password Storage**: Identity password hashing
- **HTTPS Enforcement**: Redirects all HTTP to HTTPS
- **Input Validation**: FluentValidation for all user inputs
- **Secure Payment Processing**: PCI-compliant Stripe integration

## 🔧 Configuration Management

The application uses a hierarchical configuration system:

1. **appsettings.json**: Base configuration
2. **appsettings.Development.json**: Development overrides
3. **Environment Variables**: Production secrets
4. **User Secrets**: Local development secrets

### Using User Secrets (Recommended for Development)

```bash
cd KeystoneCommerce.WebUI
dotnet user-secrets init
dotnet user-secrets set "StripeSettings:SecretKey" "sk_test_your_secret_key"
dotnet user-secrets set "EmailOptions:SmtpPassword" "your-password"
```

## 📊 Database Migrations

### Create a New Migration

```bash
dotnet ef migrations add MigrationName --project KeystoneCommerce.Infrastructure --startup-project KeystoneCommerce.WebUI
```

### Update Database

```bash
dotnet ef database update --project KeystoneCommerce.Infrastructure --startup-project KeystoneCommerce.WebUI
```

### Rollback Migration

```bash
dotnet ef database update PreviousMigrationName --project KeystoneCommerce.Infrastructure --startup-project KeystoneCommerce.WebUI
```

### Remove Last Migration

```bash
dotnet ef migrations remove --project KeystoneCommerce.Infrastructure --startup-project KeystoneCommerce.WebUI
```

## 🎯 Key Services

### **Application Services**
- `AccountService`: User account management
- `ProductService`: Product catalog operations
- `OrderService`: Order processing and management
- `PaymentService`: Payment tracking
- `CheckoutService`: Checkout workflow
- `ReviewService`: Product review management
- `CouponService`: Discount code handling
- `BannerService`: Marketing banner management
- `DashboardService`: Analytics and reporting

### **Infrastructure Services**
- `EmailService`: Email notifications
- `ImageService`: Image upload and processing
- `IdentityService`: Authentication and authorization
- `StripPaymentService`: Stripe payment integration
- `HangfireService`: Background job scheduling

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the Repository**
2. **Create a Feature Branch**: `git checkout -b feature/AmazingFeature`
3. **Commit Changes**: `git commit -m 'Add some AmazingFeature'`
4. **Push to Branch**: `git push origin feature/AmazingFeature`
5. **Open a Pull Request**

### Code Standards
- Follow C# coding conventions
- Write unit tests for new features
- Maintain clean architecture principles
- Document public APIs
- Use meaningful commit messages

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👤 Author

**Zyad Eltayabi**

- GitHub: [@Zyad-Eltayabi](https://github.com/Zyad-Eltayabi)
- Repository: [KeystoneCommerce](https://github.com/Zyad-Eltayabi/KeystoneCommerce)

## 🙏 Acknowledgments

- ASP.NET Core team for the excellent framework
- Clean Architecture principles by Robert C. Martin
- Stripe for payment processing capabilities
- Hangfire for background job processing
- The open-source community for various libraries used

## 📞 Support

For support, please open an issue in the [GitHub repository](https://github.com/Zyad-Eltayabi/KeystoneCommerce/issues).

---

**Built with ❤️ using ASP.NET Core 9.0**
