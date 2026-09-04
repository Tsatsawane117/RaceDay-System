# RaceDay - API Endpoint Plan

This table lists every endpoint the RaceDay API will expose. It covers Authentication, User Profile, Events, Categories, Event Enrolments, Results and Payments. This plan was completed before any Part 2 API code was written.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None (public) | `{ fullName, email, password, role, contactNumber }` | 201 Created - new user profile (password excluded). 400 Bad Request - missing/invalid fields. 409 Conflict - email already registered. |
| POST | /api/auth/login | Authenticates a user and returns an access token. | None (public) | `{ email, password }` | 200 OK - `{ token, userId, role }`. 401 Unauthorized - invalid credentials. |
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any (logged in) | None | 200 OK - user profile object. 401 Unauthorized. |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any (logged in) | `{ fullName, contactNumber }` | 200 OK - updated profile. 400 Bad Request. 401 Unauthorized. |
| POST | /api/events | Creates a new event. Links the event to the logged-in Organiser. | Organiser | `{ eventName, eventDate, location, description }` | 201 Created - new event object. 400 Bad Request. 403 Forbidden. |
| GET | /api/events | Lists all events, with optional status filter. | None (public) | None | 200 OK - array of events. |
| GET | /api/events/{id} | Returns full details of one event, including its categories. | None (public) | None | 200 OK - event with nested categories. 404 Not Found. |
| PUT | /api/events/{id} | Updates an existing event's details. | Organiser (must own the event) | `{ eventName, eventDate, location, description, status }` | 200 OK - updated event. 403 Forbidden - not the owner. 404 Not Found. |
| DELETE | /api/events/{id} | Cancels/removes an event. | Organiser (must own the event) | None | 200 OK - confirmation message. 403 Forbidden. 404 Not Found. |
| POST | /api/events/{id}/categories | Adds a race category (e.g. 10km) to an event. | Organiser (must own the event) | `{ categoryName, distanceKm, maxParticipants, entryFee }` | 201 Created - new category. 400 Bad Request. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/categories | Lists all categories for a specific event. | None (public) | None | 200 OK - array of categories. 404 Not Found. |
| PUT | /api/categories/{id} | Updates a category's details. | Organiser (must own the parent event) | `{ categoryName, distanceKm, maxParticipants, entryFee }` | 200 OK - updated category. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (must own the parent event) | None | 200 OK - confirmation message. 403 Forbidden. 404 Not Found. |
| POST | /api/categories/{id}/enrol | Enrols the logged-in Participant into a category and issues a bib number. | Participant | None | 201 Created - enrolment record with bib number. 400 Bad Request - category full. 404 Not Found. 409 Conflict - already enrolled. |
| GET | /api/users/me/enrolments | Lists all enrolments belonging to the logged-in Participant. | Participant | None | 200 OK - array of enrolments. 401 Unauthorized. |
| GET | /api/events/{id}/enrolments | Lists all enrolments for an event, for the organiser managing it. | Organiser (must own the event) | None | 200 OK - array of enrolments. 403 Forbidden. 404 Not Found. |
| DELETE | /api/enrolments/{id} | Cancels an enrolment. | Participant (owner) or Organiser | None | 200 OK - confirmation message. 403 Forbidden. 404 Not Found. |
| POST | /api/results | Captures a finish result against an enrolment. | Organiser | `{ enrolmentId, finishTime, position, status }` | 201 Created - new result. 400 Bad Request. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/results | Returns the leaderboard/results for an event, ordered by position. | None (public) | None | 200 OK - array of results. 404 Not Found. |
| GET | /api/enrolments/{id}/results | Returns the result for one specific enrolment. | Participant (owner) or Organiser | None | 200 OK - result object. 404 Not Found. |
| POST | /api/enrolments/{id}/payments | Records a payment made against an enrolment. | Participant (owner) | `{ amount, paymentMethod }` | 201 Created - payment record. 400 Bad Request. 404 Not Found. |
| GET | /api/enrolments/{id}/payments | Returns the payment history for an enrolment. | Participant (owner) or Organiser | None | 200 OK - array of payments. 403 Forbidden. 404 Not Found. |

**Total: 22 endpoints**, covering the six required functional areas (Authentication, User Profile, Events, Categories, Event Enrolments, Results) plus Category management and Payments, which were identified as necessary to support the enrolment and results workflow.
