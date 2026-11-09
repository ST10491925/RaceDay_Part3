# Demonstration video outline (unlisted YouTube, about 6 to 8 minutes)

Record your screen with your voice. Keep Docker Desktop and the API running before you start.

1. Introduce yourself and the project: an MVC front end for the RaceDay API, with no database or Azure code in the MVC project.
2. Show the repository on GitHub: folder structure, the part-3 branch, the commit history and the green GitHub Actions run.
3. Show the code structure in the editor: Controllers, Services (RaceDayApi), Filters, Views, and the Dockerfile.
4. Run `docker compose up --build` and open http://localhost:8080 in the browser.
5. Show the home page filters, then try to open My Events while signed out to show the redirect to sign in.
6. Register a participant, sign in, enter an event, show My Events with the green Confirmed badge, then cancel an entry.
7. Show My Results with the position and finish time, then the profile page and a profile picture upload.
8. Sign out, then sign in as the organiser. Show the different menu and the dashboard.
9. Create an event with a banner image, add categories, view enrolments, and enter results for a finished event.
10. Explain Blob Storage: the MVC app posts the file to the API, the API stores it in Azure Blob Storage and saves the URL, and the page displays that URL.
11. Explain Docker and CI briefly, then show the unit tests passing with `dotnet test`.
12. Close by summarising what was built and how the Research, Plan, Design, Build, Evaluate cycle was followed.
