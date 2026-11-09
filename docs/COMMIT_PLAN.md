# Commit plan for Part 3 (20 or more meaningful commits)

Work on a branch named part-3, and commit as each piece is added, built and checked. Run `dotnet build` before each commit so every commit leaves the project in a working state. The order below follows the Research, Plan, Design, Build, Evaluate cycle.

    git checkout -b part-3

1. Add Part 3 folder and solution file
2. Add RaceDay.Web project with Program.cs, session and HttpClient setup
3. Add appsettings with the API base address
4. Add domain models and form models with validation
5. Add ApiRoutes and the typed RaceDayApi client
6. Add JWT reader, tolerant JSON mapper and UserSession
7. Add SessionAuthorize role guard and API exception filter
8. Add shared layout with role-based navigation and flash messages
9. Add RaceDay theme (site.css, Google Fonts, SVG icons)
10. Add account controller with login, register and logout views
11. Add home page with event cards and filters
12. Add event detail page with category selection and Enter Event
13. Add My Events page with status badges and cancellation
14. Add My Results page with finishing position display
15. Add profile page with picture upload
16. Add organiser dashboard
17. Add organiser event management with banner upload
18. Add organiser category management
19. Add organiser enrolment viewing
20. Add organiser results entry with validation
21. Add xUnit test project with service and helper tests
22. Add Dockerfile, docker-compose and README run instructions
23. Add GitHub Actions workflow for Part 3
24. Fix issues found during testing and update README with CI screenshot and video link

After pushing, open the Actions tab, wait for the green run, take a screenshot and save it as docs/ci-green.png.
