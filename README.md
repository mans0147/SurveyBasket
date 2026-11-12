# SurveyBasket

SurveyBasket is an ASP.NET Core REST API for creating polls, collecting authenticated responses, and viewing poll results. It uses ASP.NET Core Identity, JWT bearer authentication, Entity Framework Core, and SQL Server.

## Features

- Create, edit, publish, unpublish, and delete polls.
- Add and manage questions with multiple answer choices.
- Register accounts, confirm email addresses, sign in, update profiles, change passwords, and request password resets.
- Manage users, roles, account locks, and administrator password resets.
- Allow each signed-in user to submit one response per active poll.
- Show live vote totals and percentages to users who have voted and to administrators.
- Send email notifications when an administrator publishes a poll, when SMTP is configured.
- Document and try the API through Swagger, including bearer token authorization.

## Requirements

- .NET 8 SDK
- SQL Server or SQL Server LocalDB
- `dotnet-ef` 8.0.8 for applying the database migrations

The default connection string uses SQL Server LocalDB. Override it through User Secrets if your SQL Server uses a different address.

## Configure local secrets

Run these commands from the repository root in PowerShell. The API requires a JWT signing key and an initial administrator account before it can start. The initial administrator is created with the `Admin` role on first startup. Its username defaults to `admin`; its email and password come from User Secrets.

```powershell
$jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "JWT:Key" $jwtKey --project SurveyBasket.API
dotnet user-secrets set "Admin:Email" "admin@example.com" --project SurveyBasket.API
dotnet user-secrets set "Admin:Username" "admin" --project SurveyBasket.API
$securePassword = Read-Host "Initial administrator password" -AsSecureString
$adminPassword = [Net.NetworkCredential]::new("", $securePassword).Password
dotnet user-secrets set "Admin:Password" $adminPassword --project SurveyBasket.API
Remove-Variable jwtKey, securePassword, adminPassword
```

The administrator password must contain at least 10 characters, including an uppercase letter, a lowercase letter, a digit, and a symbol. Keep these values in User Secrets or environment variables; never commit them.

To use a different SQL Server connection string:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your SQL Server connection string" --project SurveyBasket.API
```

Email confirmation, password recovery, and poll notifications use SMTP. `Email:BaseUrl` must be the public API address that recipients can open. Set these values in User Secrets before using those features:

```powershell
dotnet user-secrets set "Email:Enabled" "true" --project SurveyBasket.API
dotnet user-secrets set "Email:Host" "your SMTP host" --project SurveyBasket.API
dotnet user-secrets set "Email:Port" "587" --project SurveyBasket.API
dotnet user-secrets set "Email:From" "verified-sender@example.com" --project SurveyBasket.API
dotnet user-secrets set "Email:BaseUrl" "https://localhost:7289" --project SurveyBasket.API
dotnet user-secrets set "Email:Username" "your SMTP username" --project SurveyBasket.API
$smtpPassword = Read-Host "SMTP password" -AsSecureString
$smtpPasswordValue = [Net.NetworkCredential]::new("", $smtpPassword).Password
dotnet user-secrets set "Email:Password" $smtpPasswordValue --project SurveyBasket.API
Remove-Variable smtpPassword, smtpPasswordValue
```

## Build, migrate, and run

From the repository root:

```powershell
dotnet restore SurveyBasket.sln
dotnet build SurveyBasket.sln
dotnet tool install --global dotnet-ef --version 8.0.8
dotnet ef database update --project SurveyBasket.API
dotnet run --project SurveyBasket.API --launch-profile https
```

If `dotnet-ef` 8.0.8 is already installed, skip the install command. Swagger is available at [https://localhost:7289/swagger](https://localhost:7289/swagger) in Development. The HTTP launch profile uses `http://localhost:5190`.

## Authentication and accounts

Register at `POST /api/auth/register` with first name, last name, email, and a strong password. The API sends an email confirmation link; sign-in is available after confirmation using `POST /api/auth/login` with email and password. The initial administrator signs in with username `admin` (sent in the request's `email` field) and its configured password. The response contains a JWT access token.

For protected requests, use:

```text
Authorization: Bearer <access-token>
```

The Swagger page has an **Authorize** button for this token. The initial administrator is seeded from the `Admin` User Secrets settings. Publicly registered accounts receive the `User` role. Administrators can create accounts, change roles, lock or unlock users, and reset a user's password. Signed-in users can view and update their own profile or change their password. Account confirmation and self-service password recovery require working SMTP settings.

## API routes

| Method | Route | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Public | Register and send an email confirmation link |
| GET | `/api/auth/confirm-email` | Public | Confirm an account using the emailed token |
| POST | `/api/auth/resend-confirmation` | Public | Resend an account confirmation link |
| POST | `/api/auth/login` | Public | Sign in and receive a JWT |
| POST | `/api/auth/forgot-password` | Public | Send password reset instructions |
| POST | `/api/auth/reset-password` | Public | Reset a password using the emailed token |
| GET/PUT | `/api/auth/me` | Signed-in user | Read or update the current profile |
| POST | `/api/auth/change-password` | Signed-in user | Change the current password |
| GET | `/api/users` | Administrator | List user accounts |
| POST | `/api/users` | Administrator | Create an account and assign a role |
| PUT/DELETE | `/api/users/{id}` | Administrator | Update or delete an account |
| POST | `/api/users/{id}/reset-password` | Administrator | Set a replacement password for an account |
| GET | `/api/polls` | Public | List currently active, published polls |
| GET | `/api/polls/{id}` | Public | Read an active poll and its questions/answers |
| GET | `/api/polls/manage` | Administrator | List all polls, including drafts |
| POST/PUT/DELETE | `/api/polls` and `/api/polls/{id}` | Administrator | Create, edit, or delete a poll |
| PUT | `/api/polls/{id}/publish` | Administrator | Publish or unpublish a poll |
| POST | `/api/polls/{pollId}/questions` | Administrator | Add a question and its answer choices |
| GET/PUT/DELETE | `/api/polls/{pollId}/questions/{questionId}` | Administrator | Read, edit, or delete a question and its answer choices |
| POST | `/api/polls/{pollId}/votes` | Signed-in user | Submit one response to an active poll |
| GET | `/api/polls/{pollId}/results` | Voter or administrator | Read vote totals and percentages |

Poll creation accepts nested questions and answer choices. Example:

```json
{
  "title": "Preferred work arrangement",
  "summary": "Choose the arrangement that works best for you.",
  "isPublished": true,
  "startsAt": "2026-11-12",
  "endsAt": "2026-12-31",
  "questions": [
    {
      "content": "Where do you prefer to work?",
      "answers": ["Office", "Remote", "Hybrid"]
    }
  ]
}
```

To submit a vote, send the selected answer IDs, exactly one per question:

```json
{
  "answerIds": [1]
}
```

Users can see results after voting. Administrators can see results at any time. Questions cannot be changed after voting has started, which preserves the meaning of existing responses.

## Scope

The API does not include a web or MVC client, refresh tokens, or multi-select questions. Email-based account confirmation, password recovery, and poll notifications depend on SMTP configuration. Poll results are calculated from stored votes when requested.

## Contact

For project questions, contact [mohamed00147mansour@gmail.com](mailto:mohamed00147mansour@gmail.com).
