# RaceDay Part 3: MVC Web Application

Author: Sizwe Sigubudu
Module: PROG6212, Part 3 (Automation of Web Application)

RaceDay Part 3 is an ASP.NET Core MVC web application that provides the front end for the RaceDay running and walking event platform. It contains no database code and never talks to Azure directly. Every operation, including image uploads, goes through the RaceDay Part 2 REST API.

Demonstration video (unlisted YouTube): https://youtu.be/ygnLYm0dyn8
Latest successful GitHub Actions run: ADD SCREENSHOT TO docs/ci-green.png AND LINK IT HERE

## Features

Authentication and navigation: register and sign in through the API, with the JWT, user id, role and display name held in server-side session. Organisers and participants see different menus. Unauthenticated users are redirected to the sign-in page, wrong-role access is redirected to the user's own area, an expired token signs the user out cleanly, and Sign out clears the session.

Organiser interface: dashboard with events created, enrolments per event and upcoming dates; event management with create, edit and delete and a banner image upload; category management for adding age or distance categories to an event; enrolment viewing per event with status; results entry that opens only after the event has concluded and rejects duplicate positions and badly formatted times.

Participant interface: home page with search, type, category and distance filters; event detail with category selection and Enter Event, recorded immediately; My Events with colour-coded status (green Confirmed, amber Pending, red Cancelled) and cancellation; My Results showing event, date, category, finish time and position such as 47th out of 312 finishers; profile page with personal details and a profile picture upload.

Images: banner and profile files are posted to the Part 2 API, which stores them in Azure Blob Storage and saves the URL. The MVC pages display the returned URL.

## Project structure

    RaceDay_Part3/
      RaceDay.Part3.slnx            Solution
      Dockerfile                    Multi-stage build for the MVC app
      docker-compose.yml            One-command run
      RaceDay.Web/
        Controllers/                Account, Home, Events, Participant, Profile, Organiser, ManageEvents,
                                    ManageCategories, ManageEnrolments, ManageResults
        Services/                   RaceDayApi (typed API client), ApiRoutes, ApiMapper, UserSession, JwtReader
        Filters/                    SessionAuthorize (role guard), ApiExceptionFilter
        Models/                     Domain items, form models with validation, view models
        Helpers/                    Inline SVG icons (Lucide), formatting helpers
        Views/                      Razor views per controller plus shared layout and partials
        wwwroot/                    site.css and site.js
      RaceDay.Web.Tests/            xUnit tests
      DemoApi/                      Stand-in API for demonstrations (not part of the submission)
      docs/                         Commit plan and video outline
      Part2_API_Additions/          Upload controller, blob service and the API contract checklist
      .github/workflows/            CI workflow (copy to the repository root .github/workflows)

## Requirements

.NET 10 SDK and the RaceDay Part 2 API running. For Docker, Docker Desktop or Docker Engine with Compose.

## Part 2 API changes needed

The brief says the MVC project must not talk to Azure directly, so the API needs two upload endpoints. Copy BlobStorageService.cs and ImagesController.cs from Part2_API_Additions into RaceDay.API, add the two nullable image URL columns with a migration, and set the AzureBlob connection string in user secrets. Part2_API_Additions/API_CONTRACT_CHECKLIST.md lists every route this application calls, so you can compare it with your Swagger page.

## Run with Docker (single command)

Start the Part 2 API first, then from the RaceDay_Part3 folder run:

    docker compose up --build

Open http://localhost:8082. The host port is 8082 to avoid conflicts with services using port 8080. The container reaches the API through host.docker.internal. If the API uses a different address, set it for the run:

    API_BASE_URL=http://host.docker.internal:5080/ docker compose up --build

On Windows PowerShell:

    $env:API_BASE_URL="http://host.docker.internal:5080/"; docker compose up --build

Without Compose, the equivalent commands are:

    docker build -t raceday-web .
    docker run --rm -p 8082:8080 -e Api__BaseUrl=http://host.docker.internal:5080/ raceday-web

## Run the demo (site and demo API together)

When the Part 2 API is not available, the DemoApi folder holds a small stand-in API that provides every route this site calls. It is not the Part 2 submission, and images it receives are stored locally instead of in Azure Blob Storage. One command starts both the API and the site:

    docker compose -f docker-compose.demo.yml up --build

On Windows you can also double-click run-demo.bat. Open http://localhost:8081. Demo accounts: organiser@raceday.test with Organiser123! and thandi@raceday.test with Runner1234!, or register a new participant.

## Run locally without Docker

Set Api:BaseUrl in RaceDay.Web/appsettings.Development.json to the address of the Part 2 API, then run:

    dotnet run --project RaceDay.Web

## Configuration

Api:BaseUrl is the Part 2 API address. Api:AllowUntrustedCertificate accepts a self-signed development certificate and is true only in Development and in the compose file. Set it to false against a real HTTPS API. In Docker the same settings are supplied as Api__BaseUrl and Api__AllowUntrustedCertificate.

## Creating an organiser account

Organiser accounts are not created through the registration page. Use the administrator console command that came with Part 2, then sign in with that account to reach the organiser pages.

## Tests

    dotnet test RaceDay.Part3.slnx

The tests cover token reading, tolerant response mapping, the API client (bearer token handling, friendly failures, expired sessions), the role guard that redirects unauthenticated and wrong-role users, finish time and image validation, and the position and status helpers.

## Continuous integration

The workflow in .github/workflows/dotnet-part3.yml runs on pushes and pull requests to main and part-3. It validates the repository structure, checks that the MVC project has no database or Azure references, restores, builds, runs the tests, and finally builds the Docker image. Copy the file to the repository root .github/workflows folder. It assumes this folder is named RaceDay_Part3 at the repository root; change the paths if you use another name.

## Interface design

Fonts are Barlow Condensed and Barlow from Google Fonts on a Bootstrap 5 base. The theme uses asphalt navy, signal orange and chalk white, with lane markings on the home hero and race-bib styling for dates and finishing positions. Icons are inline SVG. Layouts are responsive, focus states are visible, text contrast meets WCAG AA, and motion respects the reduced-motion setting. Invalid input is handled with field messages, and failures from the API are shown as plain messages instead of error pages.

## Known assumptions

The Part 2 DTO field names are not visible from the documentation, so request bodies use common camelCase names (see RaceDayApi.cs) and responses are read by trying several likely names. If a page shows blank values, compare the JSON in Swagger with ApiMapper.cs and ApiRoutes.cs and adjust the one place that differs.
