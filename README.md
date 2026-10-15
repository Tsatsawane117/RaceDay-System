# RaceDay

**Organisers** create events and race categories and capture results. **Participants** browse events,
enrol in a category, pay the entry fee and view their own results.

| Part | Folder | What it contains |
|---|---|---|
| Part 1 - Planning and Database | `/docs` | ERD, API endpoint plan, SQL script |
| Part 2 - RESTful API | `/Part2-API` | ASP.NET Core Web API, EF Core, session authentication, Swagger, unit tests |

## The two roles

| Role | What they can do |
|---|---|
| **Organiser** | Create, update and delete their own events; define categories for them; view all enrolments for their events; capture finish times and positions. |
| **Participant** | Browse events and categories; enrol in a category; pay the entry fee; cancel their own enrolment; view their own results. |

Both roles can register, log in and view or update their own profile. Role rules are enforced on the server:
an Organiser cannot use Participant-only endpoints and vice versa, and an Organiser can only change
events that they own.

## Technology

ASP.NET Core Web API (.NET 10) - Entity Framework Core (Code-First) - SQL Server - ASP.NET Core sessions -
Swagger (Swashbuckle) - xUnit - GitHub Actions

## Setup and run instructions (Part 2)

### Prerequisites
- Visual Studio with the **ASP.NET and web development** workload
- .NET 10 SDK (installed with the latest Visual Studio)
- SQL Server (LocalDB, which comes with Visual Studio, or SQL Server Express) and SSMS

### 1. Open the solution
Open `Part2-API/RaceDay.sln` in Visual Studio and wait for the NuGet packages to restore.

### 2. Check the connection string
Open `Part2-API/RaceDay.Api/appsettings.json`. The default uses LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True
```

If you use SQL Server Express, change the server to `.\SQLEXPRESS` (use the same server name you type into
SSMS when connecting).

### 3. Create the database (EF Core Code-First migration)
The migration is already in the repository (`RaceDay.Api/Migrations`). In Visual Studio open
**Tools > NuGet Package Manager > Package Manager Console**, set **Default project** to `RaceDay.Api`, then run:

```
Update-Database
```

Open SSMS and refresh: the `RaceDayDB` database now contains the Users, Events, Categories, Enrolments,
Results and Payments tables. When the API starts it seeds the same sample data as the Part 1 SQL script.

### 4. Run the API
Right-click `RaceDay.Api`, choose **Set as Startup Project**, and press **F5**. Swagger opens at
`https://localhost:7105/swagger`.

### 5. Try it
Log in through Swagger with `POST /api/auth/login`. The browser keeps the session cookie, so every other
endpoint then works as that user.

| Seeded account | Role | Password |
|---|---|---|
| thabo.mokoena@raceday.co.za | Organiser (owns events 1 and 2) | `Password123!` |
| lindiwe.nkosi@raceday.co.za | Organiser (owns event 3) | `Password123!` |
| sipho.dlamini@gmail.com | Participant | `Password123!` |
| amahle.zulu@gmail.com | Participant | `Password123!` |

### 6. Run the unit tests
In Visual Studio: **Test > Run All Tests** (results appear in Test Explorer). Or from a terminal in the
repository root:

```
dotnet test Part2-API/RaceDay.sln
```

There are 74 tests. They start the real API in memory with an in-memory database, so they need no SQL Server.
<img width="1916" height="1012" alt="Screenshot 2026-10-14 215450" src="https://github.com/user-attachments/assets/70b3fed4-b5a3-44ff-8766-65a7ba7cd4ef" />

## Authentication and security

- **Registration:** a user supplies their details and chooses a role (Organiser or Participant). Emails are unique.
- **Password hashing:** passwords are hashed with PBKDF2 (SHA-256, 100,000 iterations, unique random salt per
  password). The original password is never stored or returned.
- **Session management:** on login the server creates a server-side session that stores the user's id and role.
  The browser only receives an HttpOnly session cookie. The `[SessionAuthorize]` attribute reads the session on every
  protected request: no session returns **401 Unauthorized**, and the wrong role returns **403 Forbidden**.
- **Ownership checks:** Organisers can only change events, categories, enrolments and results that belong to their own
  events. Participants can only see and pay for their own enrolments.

## API endpoints

Every endpoint is documented live in Swagger, including its purpose, the data it expects and each response it can return.

| Method | Route | Access |
|---|---|---|
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/logout | Logged in |
| GET | /api/users/me | Logged in |
| PUT | /api/users/me | Logged in |
| GET | /api/users/me/enrolments | Participant |
| POST | /api/events | Organiser |
| GET | /api/events | Public |
| GET | /api/events/{id} | Public |
| PUT | /api/events/{id} | Organiser (owner) |
| DELETE | /api/events/{id} | Organiser (owner) |
| POST | /api/events/{id}/categories | Organiser (owner) |
| GET | /api/events/{id}/categories | Public |
| GET | /api/events/{id}/enrolments | Organiser (owner) |
| GET | /api/events/{id}/results | Public |
| PUT | /api/categories/{id} | Organiser (owner) |
| DELETE | /api/categories/{id} | Organiser (owner) |
| POST | /api/categories/{id}/enrol | Participant |
| DELETE | /api/enrolments/{id} | Participant (owner) or event Organiser |
| GET | /api/enrolments/{id}/results | Participant (owner) or event Organiser |
| POST | /api/enrolments/{id}/payments | Participant (owner) |
| GET | /api/enrolments/{id}/payments | Participant (owner) or event Organiser |
| POST | /api/results | Organiser (owner) |

## Differences from the Part 1 plan

The Part 2 API follows the Part 1 endpoint plan (22 endpoints). These changes were made deliberately:

1. **Events table: two new columns.** The Part 2 brief requires each event to capture a *distance* and an
   *event type* (run, walk or cycle), which the Part 1 ERD did not include. `DistanceKM` and `EventType` were added to
   Events (see `Part2-API/Database/Part2_Schema_Update.sql`). All other tables and columns match the Part 1 ERD and SQL script.
2. **One extra endpoint:** `POST /api/auth/logout`, needed so a session can be ended.
3. **Seeded passwords:** the Part 1 SQL script used placeholder text for password hashes. The API seeds real PBKDF2
   hashes of `Password123!` so the sample accounts can log in.
4. **Extra response codes:** deleting an event or category that already has enrolments returns `409 Conflict`, and
   `POST /api/results` returns `200` (instead of `201`) when it updates an existing result.
5. **Cancelling an enrolment** sets its status to "Cancelled" instead of deleting the row, so results and payments
   stay traceable.

## CI/CD

The workflow in `.github/workflows/main.yml` runs on every push. It installs .NET 10, restores packages, builds the
solution in Release mode, and runs all unit tests. A failing build or test turns the run red.

**Latest successful run:**

![CI passing]<img width="1911" height="1012" alt="Screenshot 2026-10-14 214501" src="https://github.com/user-attachments/assets/b98612f7-62b5-4b48-98b1-09655117e90e" />




## Video walkthrough

Unlisted YouTube video demonstrating the running API, code structure, endpoints, authentication and unit tests:
https://youtu.be/lHZ6dlrIsxk
