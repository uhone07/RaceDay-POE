# RaceDay-POE

RaceDay is a full-stack web-based event management system built for the South African road running, walking, and cycling community. It allows Event Organisers to create and manage events, categories, and participant results, while Participants can browse upcoming events, enter events, and track their personal performance history.

This repository contains the Portfolio of Evidence (POE) for PROG6212, submitted in three parts:

- **Part 1:** System planning: ERD, API endpoint plan, and SQL database script
- **Part 2:** RESTful API built in C# with unit tests and CI/CD (this submission)
- **Part 3:** MVC web application with Azure Blob Storage and Docker

## User Roles

- **Organiser**: can create, edit, and delete events, manage event categories, capture participant results, and view all enrolments for their own events.
- **Participant**: can create an account, browse events, enter an event by selecting a category, view their own enrolments, and view their own results.

Roles are chosen at registration and stored in a server-side session on login. Role checks are enforced on every protected endpoint (401 if not logged in, 403 if the role is wrong). Organisers can only change events, categories, enrolment lists and results that belong to events they created.

## Part 2: RESTful API

### Setup and run
1. Install the .NET 10 SDK, Visual Studio, and SQL Server (Express is fine).
2. Clone the repository and open `RaceDay.Api/RaceDay.Api.slnx` in Visual Studio.
3. In `appsettings.json`, set `ConnectionStrings:DefaultConnection` to your own SQL Server instance.
4. In the Package Manager Console, run `Update-Database` to create the database.
5. Press the green **https** button to run the API.
6. Open `https://localhost:<port>/swagger` (the port is shown in the console output).

To test protected endpoints in Swagger: call **POST /api/auth/register**, then **POST /api/auth/login**.

### Unit tests
Run them in Visual Studio with **Test > Run All Tests**. They cover registration and login, event management by Organisers, role rejection, and event enrolments.

### AI use disclosure
I used an AI assistant (Claude) for step-by-step guidance while building Part 2, including project setup, code structure, debugging, and drafting this README. I reviewed, ran and tested the code and can explain it.
### Deviations from the Part 1 plan
- `POST /api/profile/picture` is not implemented in Part 2; picture upload needs Azure Blob Storage, which the plan assigns to Part 3.
- Viewing events and categories requires a logged-in user (any role), because the brief says both roles can view them. The plan listed these as public.
- JSON field names are `eventDate` and `distanceKm` instead of `date` and `distance`.
- Deleting an event or category returns 204 No Content (not 200), and returns 409 Conflict if it has enrolments.
- `POST /api/auth/logout` is open to everyone; with no session it simply does nothing.
- Extra endpoint added: `PUT /api/enrolments/{id}/cancel` lets a Participant cancel their own enrolment.
- The database is created by EF Core migrations, so column types differ slightly from the SQL script (nvarchar/datetime2 instead of varchar/datetime). Tables, keys and relationships are the same.
## CI/CD

A GitHub Actions workflow validates the repository structure, builds the API, and runs all unit tests on every push.

![CI Build Success](docs/ci-build-success.png)

![CI Build Success Part 2](docs/ci-build-success-part2.png)

## Video Presentations

- [Part 1 walkthrough](https://youtu.be/Pi1sjD3rQZ4)
- Part 2 walkthrough: PASTE YOUR UNLISTED YOUTUBE LINK HERE
