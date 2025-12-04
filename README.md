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
