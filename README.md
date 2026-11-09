# RaceDay

RaceDay is an event registration and race-day management platform for runners, walkers and cyclists.
**Organisers** create events and race categories, upload event banners, see who has entered and publish results.
**Participants** browse upcoming events, enter a category, pay their entry fee, follow their enrolment status and
look back at their own race history.

Built for PROG6212 (Programming 2B) as one Portfolio of Evidence in three parts:

| Part | Folder | What it contains |
|---|---|---|
| Part 1 - Planning and Database | `/docs` | ERD, API endpoint plan, SQL script |
| Part 2 - RESTful API | `/Part2-API` | ASP.NET Core Web API, EF Core (Code-First), session authentication, Swagger, unit tests |
| Part 3 - Web application | `/Part3-Web` | ASP.NET Core MVC front end, Azure Blob Storage image uploads (through the API), Docker |

## How the pieces fit together

```
  Browser
     |
     v
  RaceDay.Web  (MVC, port 5080)        No database access. No business logic.
     |  HTTP + JSON, session cookie forwarded
     v
  RaceDay.Api  (Web API, port 5081)    All data rules, authentication and file handling live here.
     |                    |
     v                    v
  SQL Server        Azure Blob Storage  (event banners and profile pictures)
```

The MVC application never touches the database and never talks to Azure. Everything goes through the Part 2 API.

## The two roles

| Role | What they can do |
|---|---|
| **Organiser** | Dashboard of their events; create, edit and delete events (with banner upload); add and remove categories; see every enrolment for their events; capture and publish finish times and positions. |
| **Participant** | Browse and filter upcoming events; see full event details; enter an event by choosing a category; pay the entry fee; see colour-coded enrolment statuses; view personal results; edit their profile and upload a profile picture. |

Each role gets its own navigation menu and its own pages. Anyone who is not logged in is sent to the login page when
they try to open a protected page, and a logged-in user who opens a page for the other role sees an "access denied" page.

## Features by page

**Everyone:** Events home page (search, type and distance filters, include past events), event details with categories,
public results page for each event, register, log in.

**Organiser:** Dashboard (events created, total enrolments, upcoming dates, enrolments per event) - Manage events
(create / edit / delete, banner upload) - Categories (add / remove age or distance categories) - Enrolments (every
participant, category, bib number and status) - Results entry (finish time, position, result status) - Profile.

**Participant:** Home with filters - Event detail with "Enter" per category - My events (status badges, pay, cancel) -
My results (event, date, category, finish time and position such as "47th out of 312 finishers") - Profile with picture upload.

Statuses are colour coded everywhere: **green** = Confirmed / Open / Finished, **amber** = Pending / DNF,
**red** = Cancelled / DSQ, blue = Planned, grey = Closed / Not started.

## Authentication and session management

- Registration and login are done by calling the API. A user chooses Participant or Organiser when registering.
- The API hashes passwords with PBKDF2 (SHA-256, 100,000 iterations, random salt); the original password is never stored.
- On login the web app stores the user's id, name and role in its own server-side session, and keeps the API's session
  cookie inside that session so every later call to the API is made as the same user.
- A `[SessionAuth]` filter protects every non-public page. Logging out ends the API session, clears the web session and
  redirects to the login page.

## Azure Blob Storage

Images (event banners and profile pictures) are stored in Azure Blob Storage, and only the API does this:

1. The organiser or participant chooses an image in the web app.
2. The web app sends the file to the API (`POST /api/events/{id}/banner` or `POST /api/users/me/picture`).
3. The API checks it is an image under 5 MB, uploads it to the Blob container and saves the blob URL in the database
   (`Events.BannerUrl`, `Users.ProfilePictureUrl`).
4. The web app displays the image straight from its Azure URL.

### Setting up Azure Storage
1. In the Azure portal create a **Storage account** (Standard, LRS is fine).
2. Open it, go to **Settings > Configuration** and set **Allow Blob anonymous access** to **Enabled** (so browsers can display the images). Save.
3. Go to **Security + networking > Access keys**, click **Show** and copy a **Connection string**.
4. Give the connection string to the API (never commit it):
   - **Visual Studio:** right-click `RaceDay.Api` > **Manage User Secrets** and paste:
     ```json
     { "AzureBlob": { "ConnectionString": "PASTE_CONNECTION_STRING_HERE" } }
     ```
   - **Docker:** copy `.env.example` to `.env` and paste the connection string into it.

The blob container (`raceday-images`) is created automatically on the first upload.

If no connection string is configured, the API falls back to saving images in a local `uploads` folder so the application
still runs. The marked submission uses Azure.

## Running the application

### Option 1 - Docker (one command)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/). From the folder that contains `docker-compose.yml`:

```
docker compose up --build
```

This builds and starts three containers: SQL Server, the API and the MVC web app. The first run takes a few minutes.
(Optional: copy `.env.example` to `.env` first so uploads go to Azure.)

| What | Address |
|---|---|
| Web application | http://localhost:5080 |
| API Swagger | http://localhost:5081/swagger |

Stop everything with `docker compose down` (add `-v` to also delete the database and uploaded files).

The containerised application behaves the same as running locally: the API creates the database from its migrations and
seeds the same sample data on start-up.

**How the Docker setup works**
- Each project has its own multi-stage `Dockerfile`: the .NET 10 SDK image restores and publishes the app, then only the
  small ASP.NET runtime image is kept. The project file is copied first so the package-restore layer is cached, and the
  app runs as a non-root user.
- `docker-compose.yml` starts SQL Server first (with a health check), then the API (which waits for the database and
  retries until it is ready), then the web app. The web app reaches the API by its service name (`http://api:8080`).

### Option 2 - Visual Studio

1. Open `Part2-API/RaceDay.sln`. Add the web project if it is not already there: right-click the solution > **Add > Existing Project** > `Part3-Web/RaceDay.Web/RaceDay.Web.csproj`.
2. Check the connection string in `Part2-API/RaceDay.Api/appsettings.json` (default: LocalDB).
3. Open **Tools > NuGet Package Manager > Package Manager Console**, set **Default project** to `RaceDay.Api`, and run: `Update-Database`
4. Right-click the solution > **Configure Startup Projects** > **Multiple startup projects**: set `RaceDay.Api` and `RaceDay.Web` to **Start**. Press **F5**.

| What | Address |
|---|---|
| Web application | https://localhost:7200 |
| API Swagger | https://localhost:7105/swagger |

### Demo accounts (all use the password `Password123!`)

| Account | Role |
|---|---|
| thabo.mokoena@raceday.co.za | Organiser (owns the Marathon and Fun Run) |
| lindiwe.nkosi@raceday.co.za | Organiser (owns the Trail Challenge) |
| sipho.dlamini@gmail.com | Participant (has finished two races) |
| amahle.zulu@gmail.com | Participant |

## Project structure

```
Part2-API/
  RaceDay.Api/        Web API: controllers, EF Core models and migrations, Azure Blob service, Dockerfile
  RaceDay.Tests/      94 xUnit tests (authentication, roles, events, enrolments, results, uploads)
Part3-Web/
  RaceDay.Web/
    Controllers/      Account, Events, Organiser, Participant, Profile, Home
    Services/         RaceDayApi (the only code that talks to the API), session keys, image URL resolver
    Filters/          SessionAuth (login and role protection)
    Models/           API data shapes and form view models
    Views/            Razor pages for each role + shared layout and partials
    wwwroot/css/      RaceDay theme
    Dockerfile
docs/                 Part 1 planning documents and the video script
docker-compose.yml    Runs SQL Server + API + web app
.github/workflows/    CI pipeline
```

## Unit tests

```
dotnet test Part2-API/RaceDay.sln
```

94 tests start the real API in memory (with an in-memory database and a fake image store), covering success and failure
cases for registration, login, role enforcement, events, categories, enrolments, payments, results, image uploads, the
Organiser dashboard data and the Participant race history.


## CI/CD

`.github/workflows/ci.yml` runs on every push and has three jobs:

1. **Validate repository structure** - checks the README, `docker-compose.yml`, both Dockerfiles and all three projects exist.
2. **Build and run unit tests** - builds the API, the test project and the MVC web app, then runs every unit test.
3. **Build Docker images** - builds the API and web images to prove the Dockerfiles work.

**Latest successful run:**

![CI passing]
<img width="1917" height="1072" alt="Screenshot 2026-11-09 211943" src="https://github.com/user-attachments/assets/e56aec2b-a066-4564-bd73-deacce6eb0da" />




## API additions made for Part 3

The Part 2 API gained four endpoints so the web app never needs its own data logic:

| Method | Route | Purpose |
|---|---|---|
| GET | /api/events/mine | Organiser dashboard: own events with enrolment counts |
| POST | /api/events/{id}/banner | Upload an event banner (stored in Azure Blob Storage) |
| POST | /api/users/me/picture | Upload a profile picture (stored in Azure Blob Storage) |
| GET | /api/users/me/results | Participant race history, including finishers per category |

`Events.BannerUrl` and `Users.ProfilePictureUrl` were added through the `AddImageUrls` migration, and enrolment responses now
include the event date and entry fee. (Payments are simulated: no real payment gateway is called.)

## Video walkthrough

Unlisted YouTube video demonstrating the full running application, the MVC structure, Blob Storage integration and Docker setup:

**[Insert YouTube link here]**


