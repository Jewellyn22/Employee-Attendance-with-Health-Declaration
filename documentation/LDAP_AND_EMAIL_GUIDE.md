# LDAP and Email Integration Guide

This document explains how LDAP authentication and email functionality work in the ICOPS Status Tracker application.

## Overview

The application integrates with two external services:
- **LDAP Service** - For buyer authentication via Active Directory
- **Email Service** - For sending notifications and automated reports

---

## LDAP Integration

### Purpose
LDAP (Lightweight Directory Access Protocol) is used to authenticate **Buyer users** against the company's Active Directory. End users authenticate via the Legacy API instead.

### Configuration
The LDAP service is configured in `appsettings.json`:

```json
"ApiHosts": {
  "Ldap": "http://127.0.0.1:9008"
}
```

### Connection Details

| Property | Value |
|----------|-------|
| **Host/IP** | 127.0.0.1 |
| **Port** | 9008 |
| **Protocol** | HTTP |
| **Base URL** | `http://127.0.0.1:9008` |

### Authentication Flow

#### 1. Buyer Login Endpoint
- **URL**: `/Auth/Login`
- **Controller**: `AuthController`
- **Method**: `LoginPost(string username, string password)`

#### 2. Authentication Process

```mermaid
sequenceDiagram
    participant User
    participant AuthController
    participant LdapService
    participant LdapRepository
    participant LDAP API (127.0.0.1:9008)
    participant UserService
    participant PeopleCore

    User->>AuthController: POST LoginPost(username, password)
    AuthController->>LdapService: Login(username, password)
    LdapService->>LdapRepository: Login(username, password)
    LdapRepository->>LDAP API: POST /Auth/Login {username, password}
    LDAP API-->>LdapRepository: LdapUserModel
    LdapRepository-->>LdapService: Response<LdapUserModel>
    LdapService-->>AuthController: Response<LdapUserModel>

    alt Authentication Failed
        AuthController-->>User: Error Message
    else Authentication Success
        AuthController->>UserService: GetUser(employeeNumber)
        alt User Not Found
            AuthController->>UserService: AddUser(ldapUser)
            AuthController-->>User: "Contact MIS to activate account"
        else User Found
            alt User Not Active
                AuthController-->>User: "Contact MIS to activate account"
            else User Active
                AuthController->>PeopleCore: GetEmployee(employeeNumber)
                AuthController->>AuthController: Set Session Variables
                AuthController-->>User: Redirect to Home
            end
        end
    end
```

#### 3. API Endpoint

**LDAP Login Request:**
- **Endpoint**: `Auth/Login`
- **Method**: POST
- **Request Body**:
```json
{
  "username": "string",
  "password": "string"
}
```

**LDAP Login Response:**
```json
{
  "username": "string",
  "office": "string",
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "memberOf": ["string"]
}
```

### LDAP User Model

| Property | Type | Description |
|----------|------|-------------|
| `Username` | string | AD username |
| `Office` | string? | Employee number (mapped from AD Office field) |
| `FirstName` | string? | User's first name |
| `LastName` | string? | User's last name |
| `DisplayName` | string? | Full display name |
| `MemberOf` | string[] | AD group memberships |

### Key Implementation Details

1. **Employee Number Mapping**: The `Office` field from Active Directory is used as the employee number
2. **User Creation**: If the authenticated user doesn't exist in the local database, they are automatically created with `IsActive = 0`
3. **User Activation**: Newly created users require MIS to activate their account before login
4. **Session Management**: Upon successful authentication, session variables are set:
   - `EmployeeNumber`
   - `EmployeeId`
   - `DisplayName`
   - `Role` ("Buyer")

### Files

| File | Description |
|------|-------------|
| `ICOPSStatusTrackerLibrary/ExternalApis/Ldap/Services/LdapService.cs` | LDAP service layer |
| `ICOPSStatusTrackerLibrary/ExternalApis/Ldap/Repositories/LdapRepository.cs` | LDAP repository (API calls) |
| `ICOPSStatusTrackerLibrary/ExternalApis/Ldap/Models/LdapUserModel.cs` | LDAP user data model |
| `ICOPSStatusTracker/Controllers/AuthController.cs` | Authentication controller |

---

## Email Integration

### Purpose
The email service is used for:
1. **End User Notifications** - Notifying users about purchase request status updates
2. **Automated Reports** - Sending auto-cancel PR service reports to administrators

### Configuration
The email service is configured in `appsettings.json`:

```json
"ApiHosts": {
  "Email": "http://127.0.0.1:9013"
}
```

### Connection Details

| Property | Value |
|----------|-------|
| **Host/IP** | 127.0.0.1 |
| **Port** | 9013 |
| **Protocol** | HTTP |
| **Base URL** | `http://127.0.0.1:9013` |

### Email Service API

**Send Email Request:**
- **Endpoint**: `Email/SendEmail`
- **Method**: POST
- **Request Body**:
```json
{
  "toRecipient": "recipient@example.com",
  "ccRecipient": "cc@example.com",
  "bccRecipient": "bcc@example.com",
  "subject": "Email Subject",
  "body": "Email body content",
  "isHtml": 1
}
```

### Email Parameters DTO

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ToRecipient` | string | `""` | Semicolon-separated list of TO recipients |
| `CcRecipient` | string | `""` | Semicolon-separated list of CC recipients |
| `BccRecipient` | string | `""` | Semicolon-separated list of BCC recipients |
| `Subject` | string | `"[Empty Subject]"` | Email subject line |
| `Body` | string | `""` | Email body content |
| `IsHtml` | int | `1` | Format flag (1=HTML, 0=Plain text) |

### Usage Patterns

#### 1. End User Notifications

Implemented in `NotificationService.NotifyEndUser()`:

```csharp
// Get recipients from PeopleCore
string requesterEmail = await GetPeopleCoreEmail(requesterId);
string endUserEmail = await GetPeopleCoreEmail(endUserC);
string contactPersonEmail = contactEmail;

// Get CC recipients
string mainBuyerEmail = await GetPeopleCoreEmail(mainBuyerId);
string assistantBuyerEmail = await GetPeopleCoreEmail(assistantId);

// Build email
var emailRequest = new SendEmailRequestParameterDto
{
    ToRecipient = string.Join(';', toRecipients),
    CcRecipient = string.Join(';', ccRecipients),
    BccRecipient = configBccRecipients,
    Subject = emailSubject,
    Body = emailTemplate
};

await _emailService.SendEmail(emailRequest);
```

**Config Table Settings** (for end user notifications):
| Key | Description |
|-----|-------------|
| `EnableEmailSending` | Toggle email sending (1=enabled, 0=disabled) |
| `RerouteToTemporaryEmail` | Redirect all emails to temp recipient (1=yes, 0=no) |
| `TemporaryEmailRecipient` | Temporary email address for rerouting |
| `BccRecipient` | Default BCC recipient(s) |
| `EndUserEmailNotificationTemplate` | Email body template |
| `EndUserEmailNotificationSubject` | Email subject template |

#### 2. Auto-Cancel PR Service Reports

Implemented in `AutoCancelPRService` background service:

```csharp
// Build results table HTML
string resultsTable = BuildResultsTable(results);

// Replace placeholders in email template
string emailBody = emailBodyTemplate
    .Replace("{DateTime}", currentDateTime)
    .Replace("{TotalProcessed}", results.Count.ToString())
    .Replace("{SuccessCount}", successCount.ToString())
    .Replace("{FailedCount}", failedCount.ToString())
    .Replace("{ResultsTable}", resultsTable);

var emailRequest = new SendEmailRequestParameterDto
{
    ToRecipient = emailTo,
    CcRecipient = emailCc,
    BccRecipient = emailBcc,
    Subject = emailSubject,
    Body = emailBody,
    IsHtml = 1
};

await emailService.SendEmail(emailRequest);
```

**Config Table Settings** (for auto-cancel reports):
| Key | Description |
|-----|-------------|
| `AutoCancelPREmailEnabled` | Enable/disable report emails (1=enabled, 0=disabled) |
| `AutoCancelPREmailTo` | TO recipient(s) |
| `AutoCancelPREmailCc` | CC recipient(s) |
| `AutoCancelPREmailBcc` | BCC recipient(s) |
| `AutoCancelPREmailSubject` | Email subject template |
| `AutoCancelPREmailBody` | Email body template |

### Email Template Placeholders

**End User Notifications:**
| Placeholder | Replaced With |
|-------------|---------------|
| `{{RequestNo}}` | Purchase Request Number |
| `{{ItemC}}` | Item Code |
| `{{ItemName}}` | Item Name |
| `{{NecessityOfAssessment}}` | Necessity text |
| `{{Qty}}` | Quantity |
| `{{Uomc}}` | Unit of Measure |
| `{{Description}}` | Item Description |
| `{{Remarks}}` | Additional Remarks |

**Auto-Cancel PR Report:**
| Placeholder | Replaced With |
|-------------|---------------|
| `{DateTime}` | Current date/time |
| `{TotalProcessed}` | Total PRs processed |
| `{SuccessCount}` | Successful cancellations |
| `{FailedCount}` | Failed cancellations |
| `{ResultsTable}` | HTML results table |
| `{StartDateTime}` | Service start time |
| `{EndDateTime}` | Service end time |
| `{OverallDuration}` | Total duration formatted |

### Files

| File | Description |
|------|-------------|
| `ICOPSStatusTrackerLibrary/ExternalApis/Email/Services/EmailService.cs` | Email service layer |
| `ICOPSStatusTrackerLibrary/ExternalApis/Email/Repositories/EmailRepository.cs` | Email repository (API calls) |
| `ICOPSStatusTrackerLibrary/ExternalApis/Email/Dtos/SendEmailRequestParameterDto.cs` | Email request DTO |
| `ICOPSStatusTrackerLibrary/Services/NotificationService.cs` | End user notification service |
| `ICOPSStatusTracker/BackgroundServices/AutoCancelPRService.cs` | Auto-cancel PR service with email reports |

---

## Service Architecture

### Dependency Injection

Both LDAP and Email services are registered in `Program.cs`:

```csharp
// LDAP Registration
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<ILdapRepository, LdapRepository>();

// Email Registration
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
```

### Common API Request Pattern

Both services use the shared `ApiRequest` class from `DataLibrary.DataAccess`:

```csharp
private readonly ApiRequest _apiRequest = new(config["ApiHosts:Ldap"] ?? string.Empty);

public async Task<Response<T>> Post<T>(ApiRequestParameter parameter)
{
    // Makes HTTP POST request to external API
    // Returns Response<T> with Success, Message, and Data properties
}
```

---

## Summary Table

| Feature | LDAP | Email |
|---------|------|-------|
| **Host** | 127.0.0.1 | 127.0.0.1 |
| **Port** | 9008 | 9013 |
| **Protocol** | HTTP | HTTP |
| **Base URL** | `http://127.0.0.1:9008` | `http://127.0.0.1:9013` |
| **Main Endpoint** | `/Auth/Login` | `/Email/SendEmail` |
| **Primary Use** | Buyer Authentication | Notifications & Reports |
| **User Type** | Buyer Role | End Users & Admins |
| **Configuration** | `appsettings.json` only | `appsettings.json` + Config table |

---

## Related Documentation

- [CLAUDE.md](./CLAUDE.md) - Project overview and development rules
- External API integration details for PeopleCore, LegacyApi, and Ows
