# Part 2 API contract used by the Part 3 MVC application

The MVC project calls only the routes below. All of them are listed in one file,
RaceDay.Web/Services/ApiRoutes.cs, so any difference in your Part 2 API is a one-line change.
Responses are read tolerantly (see RaceDay.Web/Services/ApiMapper.cs), so small naming
differences such as eventDate versus date do not break the pages.

Check each route against your Swagger page before recording the video.

| Purpose | Route |
| --- | --- |
| Register | POST api/auth/register (firstName, lastName, email, phoneNumber, password) |
| Login | POST api/auth/login (email, password), returns token |
| Profile | GET and PUT api/users/me |
| Profile picture (new) | POST api/users/me/picture, multipart field named file |
| List and view events | GET api/events and GET api/events/{id} |
| Create, edit, delete event | POST api/events, PUT and DELETE api/events/{id} |
| Event banner (new) | POST api/events/{id}/banner, multipart field named file |
| Categories | GET and POST api/categories |
| Event categories | GET and POST api/events/{id}/categories (POST body: categoryId) |
| Enrol | POST api/enrolments (eventId, eventCategoryId) |
| My enrolments | GET api/enrolments/my |
| Cancel enrolment | DELETE api/enrolments/{id} |
| Event enrolments (organiser) | GET api/events/{id}/enrolments |
| Record results (organiser) | POST api/results and PUT api/results/{id} (enrolmentId, finishTime, position) |
| Event results (organiser) | GET api/events/{id}/results |
| My results | GET api/results/my, should include eventId, eventName, eventDate, categoryName, finishTime, position and totalFinishers |

Routes marked new need ImagesController.cs and BlobStorageService.cs from this folder.
The totalFinishers value lets the My Results page show a line such as 47th out of 312 finishers.
If the API cannot supply it, the page falls back to counting the event results, and shows only the position when that is not allowed for participants.
