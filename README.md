# RaceDay

RaceDay is an event registration and race-day management system built for the PROG6212 (Programming 2B) Portfolio of Evidence. It allows race Organisers to create events and race categories, and allows Participants to browse events, enrol in categories, pay their entry fees, and view their results once a race is complete.

This repository currently contains the **Part 1 - System Planning and Database** deliverables. The RESTful API implementation (Part 2) is planned according to the endpoint plan below and will be added to this repository as it is built.

## System Description

RaceDay solves a common problem for community and city race organisers: managing entries, categories, and results across multiple events without relying on spreadsheets or paper sign-up sheets. An Organiser can list an event (e.g. a marathon), break it into categories (e.g. 5km, 10km, 21km), and track who has enrolled in each. A Participant can register once, then enrol in any open category, pay their entry fee, and check their finishing position after the race.

The system is planned around six core entities - **Users, Events, Categories, Enrolments, Results,** and **Payments** - fully documented in the ERD in `/docs/erd.png`.

## User Roles

| Role | Description | Typical actions |
|---|---|---|
| **Organiser** | Creates and manages events on behalf of a race organisation. | Create/update/cancel events, add and manage race categories, view enrolments for their events, capture results. |
| **Participant** | A member of the public who wants to take part in a race. | Register/log in, browse events, enrol in a category, make payment, view their own results. |

Both roles share the same `Users` table and are distinguished by a `Role` column, since both share the same core profile attributes (name, email, password, contact number).

## Repository Structure

```
RaceDay/
├── docs/
│   ├── erd.png                      # Entity Relationship Diagram (Section A)
│   ├── API_Endpoint_Plan.md         # API endpoint plan (Section B)
│   ├── RaceDay_Database_Script.sql  # Full SQL schema + seed data (Section C)
│   └── ci-success-screenshot.png    # Screenshot of a passing GitHub Actions run
├── .github/
│   └── workflows/
│       └── validate-structure.yml   # CI workflow validating /docs contents
└── README.md
```

## Database

The full schema is defined in [`docs/RaceDay_Database_Script.sql`](docs/RaceDay_Database_Script.sql) and matches the ERD in [`docs/erd.png`](docs/erd.png) exactly. It creates all six tables with primary keys, foreign keys, and constraints, and seeds the database with realistic sample data (2 Organisers, 2 Participants, 3 Events, categories per event, sample enrolments, results, and payments).

**To run it:**
1. Open SQL Server Management Studio (SSMS).
2. Create a new database, e.g. `CREATE DATABASE RaceDayDB;`
3. Open `RaceDay_Database_Script.sql` and run it against that database.
4. The script drops and recreates all tables, so it can be re-run safely at any time.

## API Endpoint Plan

The full list of planned endpoints is documented in [`docs/API_Endpoint_Plan.md`](docs/API_Endpoint_Plan.md), covering Authentication, User Profile, Events, Categories, Event Enrolments, Results, and Payments (22 endpoints in total). The Part 2 implementation will follow this plan; any deviations will be explained here.

## CI/CD

A GitHub Actions workflow ([`.github/workflows/validate-structure.yml`](.github/workflows/validate-structure.yml)) runs on every push and validates that the `/docs` folder exists and contains the required planning files.

**Latest successful run:**

![CI passing](docs/ci-success-screenshot.png)

*(Screenshot to be added once the workflow has been run against this repository.)*

## Video Walkthrough

An unlisted YouTube video walking through the planning documents, ERD decisions, endpoint plan choices, and a live run of the SQL script in SSMS:

**[Insert YouTube link here]**

## Author

Tsatsawane - Diploma in Software Development, Second Year
Module: PROG6212 - Programming 2B
