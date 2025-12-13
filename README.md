# TaskSystem

## Introduction
TaskSystem is a service for managing projects and tasks.
The system provides operations for user registration, authentication, retrieving the list of projects, and creating tasks.

## Installation
Follow the steps below to install and run the application.

1. Clone the repo
   ```
   git clone https://github.com/ob1tower/task-system.git
   ```
3. Navigate into project folder
    ```
    cd task-system/TaskSystem
    ```
4. Open Swagger UI
    ```
    http://localhost:5072/swagger
    ```
## REST API
The REST API to the example app is described below.
### [1] Register
#### Request
`POST /api/v2/auth/Register`
```
curl -X 'POST' \
  'http://localhost:5072/api/v2/auth/Register' \
  -H 'accept: */*' \
  -H 'IdempotencyKey: 00000000-0000-0000-0000-000000000000' \
  -H 'Content-Type: application/json' \
  -d '{
  "userName": "user",
  "email": "user@mail.ru",
  "password": "user123"
}'
```
#### Response
```
{
  "id": "<USER_ID>",
  "userName": "user",
  "email": "user@mail.ru"
}
```

### [2] Login (JWT authentication)
#### Request
`POST /api/v2/auth/Login`
```
curl -X 'POST' \
  'http://localhost:5072/api/v2/auth/Login' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "email": "user@mail.ru",
  "password": "user123"
}'
```
#### Response body
```
{
  "message": "Пользователь успешно авторизован.",
  "tokens": {
    "accessToken": "<JWT_ACCESS_TOKEN>"
  }
}
```

### [3] Get list of Projects
#### Request
`GET /api/v2/project`
```
curl -X 'GET' \
  'http://localhost:5072/api/v2/project/GetAll?pageNumber=1&pageSize=10' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```
#### Response body
```
{
  "message": "Список проектов.",
  "projects": [
    {
      "projectId": "<PROJECT_ID>",
      "name": "My first project",
      "description": Description,
      "createdAt": "<CREATION_TIME>"
    }
  ]
}

```

### [4] Create Job
#### Request
`POST /api/v2/job`
```
curl -X 'POST' \
  'http://localhost:5072/api/v2/job/Create' \
  -H 'accept: */*' \
  -H 'IdempotencyKey: 00000000-0000-0000-0000-000000000001' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "My first task",
  "projectId": "<PROJECT_ID>"
}'
```
#### Response body
```
{
  "message": "Задача с Id <TASK_ID> успешно создана."
}
```

### [5] Get Job by ID
#### Request
`GET /api/v2/job/{id}`
```
curl -X 'GET' \
  'http://localhost:5072/api/v2/job/Get/f6d961da-4d9c-4d82-8f99-64f02acdf347' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```
#### Response body
```
{
  "statusCode": 429,
  "description": "Вы превысили лимит запросов. Попробуйте позже."
}
```
#### Response headers
```
retry-after: 30
x-limit-remaining: 0
```
# Client Application (Lab3)

## Overview
The lab3 folder contains a client-side web application developed for interacting with an external REST API provided by another student as part of Laboratory Work №3.
The client supports authentication, CRUD operations for teachers and courses, pagination, request-limit handling, and idempotent POST requests.
### [1] What the Client Does
1. Handles user authentication (login/register if provided).
2. Displays, edits and deletes teachers and courses.
3. Supports pagination and page-size switching.
4. Implements idempotent POST operations via IdempotencyKey.
5. Shows detailed error messages, including rate-limit errors (HTTP 429).
### [2] The API that the client is working with
The client interacts with an external API hosted at: http://localhost:8080/api/v2
### [3] Fetching Teachers List
When the Teachers page is opened, the client sends:
#### Request
`GET /api/v2/teachers?pageNumber=1&pageSize=10`
#### Example response
```
[
  {
    "id": "019b172a-32ae-75f8-a0e9-97b8f65325cf",
    "login": "teacher01",
    "lastName": "Ivanov",
    "firstName": "Ivan",
    "middleName": "Sergeevich"
  }
]
```
### [4] Creating a Course
The client uses an IdempotencyKey header to prevent duplicate course creation if the request is sent twice.
#### Request
```
POST /api/v2/courses
IdempotencyKey: <uuid>
Authorization: Bearer <token>
Content-Type: application/json
```
#### Body
```
{
  "title": "Frontend Basics",
  "description": "Introduction to frontend development",
  "teacherId": "019b172a-32ae-75f8-a0e9-97b8f65325cf"
}
```
#### Example response
```
{
  "id": "<COURSE_ID>",
  "title": "Frontend Basics",
  "description": "Introduction to frontend development"
}
```
