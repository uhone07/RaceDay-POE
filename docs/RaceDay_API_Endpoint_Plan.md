# RaceDay — API Endpoint Plan (Part 1, Section B)

Role legend: **None** = public, **Any** = any logged-in user, **Organiser** / **Participant** = specific role only.

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None | { fullName, email, password, role } | 201 Created – user created (no password returned) 400 Bad Request – validation failed 409 Conflict – email already in use |
| POST | /api/auth/login | Authenticates a user and starts a session storing their user ID and role. | None | { email, password } | 200 OK – session started, returns userId and role 401 Unauthorized – invalid credentials |
| POST | /api/auth/logout | Ends the current user's session. | Any | None | 200 OK – session cleared |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/profile | Returns the logged-in user's own profile information. | Any | None | 200 OK – profile object 401 Unauthorized – no active session |
| PUT | /api/profile | Updates the logged-in user's own profile information. | Any | { fullName, phoneNumber } | 200 OK – updated profile 400 Bad Request – validation failed |
| POST | /api/profile/picture | Uploads/replaces the logged-in user's profile picture (stored in Azure Blob Storage in Part 3). | Any | multipart/form-data file | 200 OK – returns picture URL 400 Bad Request – invalid file |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Returns all events, with optional filtering (e.g. by date or type). | None | None | 200 OK – list of events |
| GET | /api/events/{id} | Returns full details for a single event. | None | None | 200 OK – event object 404 Not Found – event does not exist |
| POST | /api/events | Creates a new event owned by the logged-in Organiser. | Organiser | { name, description, date, location, distance, eventType } | 201 Created – event created 400 Bad Request – validation failed |
| PUT | /api/events/{id} | Updates an event owned by the logged-in Organiser. | Organiser | { name, description, date, location, distance, eventType } | 200 OK – event updated 403 Forbidden – not the owning Organiser 404 Not Found – event does not exist |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser | None | 200 OK – event deleted 403 Forbidden – not the owning Organiser 404 Not Found – event does not exist |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Returns all categories available for a specific event. | None | None | 200 OK – list of categories 404 Not Found – event does not exist |
| POST | /api/events/{eventId}/categories | Adds a new age/distance category to an event owned by the Organiser. | Organiser | { name, minAge, maxAge, distance } | 201 Created – category created 403 Forbidden – not the owning Organiser |
| PUT | /api/categories/{id} | Updates an existing category. | Organiser | { name, minAge, maxAge, distance } | 200 OK – category updated 403 Forbidden – not the owning Organiser 404 Not Found – category does not exist |
| DELETE | /api/categories/{id} | Deletes a category. | Organiser | None | 200 OK – category deleted 403 Forbidden – not the owning Organiser 404 Not Found – category does not exist |

## 5. Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/enrolments | Enrols the logged-in Participant into an event under a chosen category. | Participant | { categoryId } | 201 Created – enrolment recorded 400 Bad Request – category does not belong to event 409 Conflict – already enrolled |
| GET | /api/enrolments/my | Returns all enrolments belonging to the logged-in Participant. | Participant | None | 200 OK – list of enrolments |
| GET | /api/events/{eventId}/enrolments | Returns all Participants enrolled in a specific event, owned by the Organiser. | Organiser | None | 200 OK – list of enrolments 403 Forbidden – not the owning Organiser |
| PUT | /api/enrolments/{id}/status | Updates an enrolment's status (e.g. Pending to Confirmed). | Organiser | { status } | 200 OK – status updated 403 Forbidden – not the owning Organiser 404 Not Found – enrolment does not exist |

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures the finish time and position for a Participant's enrolment, once the event has concluded. | Organiser | { finishTime, finishPosition, totalFinishers } | 201 Created – result captured 403 Forbidden – not the owning Organiser 409 Conflict – result already captured |
| PUT | /api/results/{id} | Updates a previously captured result. | Organiser | { finishTime, finishPosition, totalFinishers } | 200 OK – result updated 403 Forbidden – not the owning Organiser 404 Not Found – result does not exist |
| GET | /api/results/my | Returns the logged-in Participant's personal race history across all completed events. | Participant | None | 200 OK – list of results with event name, date, category, time, position |
| GET | /api/events/{eventId}/results | Returns all published results for a specific event. | Organiser | None | 200 OK – list of results 403 Forbidden – not the owning Organiser |
