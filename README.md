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
# Система задач с интеграцией RabbitMQ (4Lab)

## Обзор
Система реализует приложение управления задачами, которое использует RabbitMQ для обмена сообщениями между клиентом и сервером, с PostgreSQL в качестве бэкенда базы данных.

## Архитектура
- **Клиентское приложение**: Консольное приложение, которое отправляет стандартизованные сообщения в RabbitMQ
- **Сервер API**: ASP.NET Core Web API, который потребляет сообщения из RabbitMQ и выполняет операции с базой данных
- **Сообщения**: RabbitMQ для асинхронной коммуникации через очереди `api.requests` и `api.responses`
- **База данных**: PostgreSQL для хранения данных

## Схема обмена сообщениями

```
Клиент -> [api.requests] -> Сервер -> [api.responses] -> Клиент
```

Сервер подписывается на очередь `api.requests`, обрабатывает сообщения и отправляет ответы в `api.responses`.

## Структура очередей и маршрутов

- **api.requests** - очередь для входящих запросов от клиентов
- **api.responses** - очередь для исходящих ответов клиентам
- **dead_letter_queue** - очередь для сообщений, которые не удалось обработать

## Формат сообщений

### Запрос:
```json
{
  "id": "uuid",
  "version": "v1",
  "action": "create_job",
  "data": { ... },
  "auth": "api-key"
}
```

### Ответ:
```json
{
  "correlation_id": "uuid",
  "status": "ok",
  "data": { ... },
  "error": null
}
```

## Запуск системы

### 1. С использованием Docker Compose (Рекомендуется)
```bash
# Запустить всю инфраструктуру
docker-compose up --build

# В отдельном терминале запустить клиентское приложение
cd ClientApp/TaskSystemClient
dotnet run
```

### 2. Запуск компонентов по отдельности
Сначала необходимо запустить PostgreSQL и RabbitMQ. Можно использовать:
- Docker для запуска инфраструктуры:
```bash
docker run -d --name postgres -e POSTGRES_DB=task_system_db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=qweapril -p 5432:5432 postgres:15
docker run -d --name rabbitmq -e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest -p 5672:5672 -p 15672:15672 rabbitmq:3.12-management
```

Затем запустить API:
```bash
cd TaskSystem
dotnet run
```

И наконец, запустить клиента:
```bash
cd ClientApp/TaskSystemClient
dotnet run
```

### 3. Полный процесс запуска пошагово

1. **Запустите инфраструктуру (один из вариантов):**
   - Используйте Docker Compose
     ```bash
     docker-compose up --build
     ```

2. **Откройте ещё один терминал и запустите клиентское приложение:**
     ```bash
     cd /Users/mtrx/Desktop/УНИВЕР/не мое/task-system/lab1/ClientApp/TaskSystemClient
     dotnet run
     ```

3. **После запуска клиентского приложения вы увидите подсказку с командами, которые можно использовать. Введите нужную команду и нажмите Enter.**

4. **Для завершения работы приложения нажмите Ctrl+C в каждом терминале или используйте команду `exit` в клиентском приложении.**

## Использование клиента

Клиентское приложение поддерживает следующие команды:

### 1. Создание задачи
```
create "Моя новая задача" 11111111-1111-1111-1111-111111111111
```

### 2. Обновление задачи
```
update 22222222-2222-2222-2222-222222222222 "Обновленный заголовок" "Обновленное описание" 2025-12-31 11111111-1111-1111-1111-111111111111
```

### 3. Удаление задачи
```
delete 22222222-2222-2222-2222-222222222222
```

### 4. Получение конкретной задачи
```
get 22222222-2222-2222-2222-222222222222
```

### 5. Список всех задач
```
list 1 10
# или просто:
list
```

### 6. Выход из клиента
```
exit
```

## API точки

API также поддерживает прямые HTTP вызовы по следующим точкам:
- `GET /api/v1/job-mq/GetAll?pageNumber=1&pageSize=10` - Получить все задачи
- `POST /api/v1/job-mq/Create` - Создать задачу (отправляет сообщение в очередь)
- `PUT /api/v1/job-mq/Update/{id}` - Обновить задачу (отправляет сообщение в очередь)
- `DELETE /api/v1/job-mq/Delete/{id}` - Удалить задачу (отправляет сообщение в очередь)

## Механизмы безопасности

### Аутентификация
Система использует JWT-токены для аутентификации пользователей. Токены передаются в поле `auth` сообщений в формате:
```
"auth": "Bearer <jwt_token>"
```

JWT-токены обеспечивают безопасное и защищенное взаимодействие между клиентом и сервером, позволяя подтверждать личность пользователя и обеспечивать целостность данных сообщений.

## Механизмы идемпотентности

Система реализует идемпотентность через хранение обработанных ID сообщений в памяти. Каждое сообщение имеет уникальный ID, и если сервер получает сообщение с уже обработанным ID, он возвращает кэшированный результат, предотвращая дублирование операций.

## Важные замечания

1. Система использует асинхронные сообщения, поэтому операции могут не сразу отразиться в базе данных
2. Убедитесь, что ваша база данных PostgreSQL правильно настроена с правильными строками подключения
3. RabbitMQ должен быть запущен перед запуском сервера API
4. Система включает в себя потребителя сообщений, который обрабатывает сообщения в фоновом режиме
5. Оригинальные прямые точки API сервера базы данных все еще доступны по адресу `/api/v1/job/`

## Структура проекта
```
TaskSystem/                     # Основной проект сервера API
├── RabbitMq/                  # Стандартизованные сообщения RabbitMQ
├── Services/Jobs/             # Сервисы задач с обработкой сообщений
├── Controllers/v1/            # Контроллеры API включая JobMessagingController
├── BackgroundServices/        # Сервис хостинга для потребления сообщений
├── Services/                  # Службы общего назначения (IdempotencyService)
ClientApp/TaskSystemClient/    # Консольное клиентское приложение
docker-compose.yml             # Конфигурация Docker для инфраструктуры
```

## Особенности реализации

### Dead Letter Queue (DLQ)
Система поддерживает полнофункциональную очередь "мертвых писем" (DLQ) для обработки сообщений, которые не удалось обработать. Реализация включает:

- **Автоматическое перемещение ошибочных сообщений**: Сообщения, которые не могут быть обработаны после 3 попыток, автоматически перемещаются в DLQ
- **Отдельная очередь для сбоя**: Все неудачные сообщения направляются в очередь `dead_letter_queue` с обменом `dead_letter_exchange`
- **Метаданные сбоя**: Каждое сообщение в DLQ содержит информацию об ошибке и времени сбоя
- **Настройка TTL**: Сообщения в основной очереди имеют TTL 5 минут, после чего перемещаются в DLQ при необходимости
- **Обработка неизвестных операций**: Сообщения с неизвестным типом операции сразу направляются в DLQ

### Система повторных попыток (Retry)
Система реализует механизм повторных попыток обработки сообщений:
- **Количество попыток**: 3 попытки повторной обработки при временных ошибках
- **Автоматическое повторение**: Сообщения с временными ошибками автоматически возвращаются в очередь для повторной обработки
- **Перемещение в DLQ**: Если сообщение не может быть обработано после 3 попыток, оно перемещается в DLQ

### Логгирование
Система использует продвинутое логгирование с помощью Serilog, которое обеспечивает:

- **Логгирование в файлы**: Все события логируются в файлы в директории `logs/` с ежедневной ротацией
- **Консольный вывод**: Логи также выводятся в консоль для мониторинга в реальном времени
- **Структурированные логи**: Используется формат структурированного логирования для лучшего анализа
- **Подробное логгирование ошибок**: Все ошибки обрабатываются с детальной информацией, включая трассировку стека
- **Логгирование DLQ**: События перемещения сообщений в DLQ также логируются

Файлы логов будут создаваться в директории `logs/` с именами в формате `task-system-YYYYMMDD.txt`.

## Сравнение подходов: RabbitMQ vs REST API

### Преимущества архитектуры с использованием RabbitMQ:

1. **Асинхронная обработка**: Запросы обрабатываются асинхронно, что позволяет системе продолжать работу даже при длительных операциях
2. **Масштабируемость**: Возможность масштабировать потребителей сообщений независимо от производителей
3. **Отказоустойчивость**: Брокер сообщений гарантирует доставку даже при кратковременных сбоях системы
4. **Декуплинг**: Производители и потребители не зависят друг от друга, что упрощает разработку и развертывание
5. **Надежность**: Возможность повторной обработки сообщений и системы Dead Letter Queue для обработки ошибок
6. **Производительность**: Более эффективная обработка большого количества запросов за счет асинхронности

### Преимущества традиционной REST API архитектуры:

1. **Простота разработки**: Прямые синхронные вызовы проще в реализации и отладке
2. **Однозначность ответа**: Клиент сразу получает результат операции
3. **Поддержка инструментов**: Больше инструментов для тестирования, документирования и мониторинга
4. **Интерактивность**: Лучше подходит для интерактивных сценариев с немедленной реакцией

### Когда использовать каждый подход:

- **RabbitMQ** рекомендуется для систем, где важна масштабируемость, отказоустойчивость и возможность обрабатывать большие объемы асинхронных операций
- **REST API** лучше подходит для интерактивных веб-приложений, где требуется немедленный ответ от сервера
- **Гибридный подход**, как в данной системе, позволяет получить преимущества обоих решений

## Управление миграциями базы данных

В системе используются Entity Framework Core миграции для управления схемой базы данных. Ниже приведены основные команды и рекомендации по работе с миграциями.

### Требования

Для выполнения команд миграции необходимо:
- Установленный .NET SDK (версия 8.0 или выше)
- Доступ к серверу PostgreSQL, к которому будет выполнено подключение

### Основные команды миграции

#### 1. Просмотр состояния миграций
```bash
cd TaskSystem
dotnet ef migrations list
```

#### 2. Добавление новой миграции
**Для контекста UserDbContext:**
```bash
cd TaskSystem
dotnet ef migrations add <Название_миграции> --context UserDbContext
```

**Для контекста JobProjectDbContext:**
```bash
cd TaskSystem
dotnet ef migrations add <Название_миграции> --context JobProjectDbContext
```

#### 3. Применение миграций к базе данных
**Для контекста UserDbContext:**
```bash
cd TaskSystem
dotnet ef database update --context UserDbContext
```

**Для контекста JobProjectDbContext:**
```bash
cd TaskSystem
dotnet ef database update --context JobProjectDbContext
```

#### 4. Откат последней миграции
**Для контекста UserDbContext:**
```bash
cd TaskSystem
dotnet ef migrations remove --context UserDbContext
```

**Для контекста JobProjectDbContext:**
```bash
cd TaskSystem
dotnet ef migrations remove --context JobProjectDbContext
```

### Рекомендации по работе с миграциями

1. **Создание миграций в разработке**
   При разработке новых функций, которые требуют изменений в схеме базы данных, создавайте миграции с говорящими именами:
   ```bash
   dotnet ef migrations add AddDescriptionFieldToProjects --context JobProjectDbContext
   ```

2. **Работа с несколькими контекстами**
   В системе используются два контекста базы данных (UserDbContext и JobProjectDbContext), подключающихся к одной базе данных. При работе с миграциями всегда указывайте конкретный контекст с помощью флага `--context`.

3. **Проверка строки подключения**
   Перед применением миграций убедитесь, что строка подключения в файле `appsettings.Development.json` указывает на правильную базу данных:
   ```json
   {
     "ConnectionStrings": {
       "UserDbContext": "User ID=postgres;Password=qweapril;Host=localhost;Port=5432;Database=task_system_db;",
       "JobProjectDbContext": "User ID=postgres;Password=qweapril;Host=localhost;Port=5432;Database=task_system_db;"
     }
   }
   ```

4. **Использование в Docker-окружении**
   При запуске через Docker Compose, убедитесь, что PostgreSQL контейнер запущен перед применением миграций. В production-окружении миграции обычно применяются автоматически при запуске приложения через скрипты инициализации.

5. **Резервное копирование**
   Перед применением миграций на production-базе данных обязательно создайте резервную копию.

6. **Проверка миграций**
   Всегда проверяйте сгенерированные файлы миграций перед их применением, чтобы убедиться, что они содержат ожидаемые изменения схемы.

### Управление миграциями через Docker

Если вы хотите выполнить миграции, когда PostgreSQL запущен в Docker-контейнере, сначала убедитесь, что контейнер запущен:
```bash
docker-compose up -d postgres
```

Затем выполните команды миграции из корневой директории проекта.

### Структура миграций

Миграции хранятся в следующих директориях:
- `TaskSystem/Migrations/` - Миграции для UserDbContext
- `TaskSystem/Migrations/JobProjectDb/` - Миграции для JobProjectDbContext

Каждая миграция содержит как SQL-скрипт для обновления схемы, так и метод для отката изменений (Down-метод).