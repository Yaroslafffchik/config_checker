# Практическая работа 3 — настройки и базовая защита

## Описание
Проект демонстрирует чтение настроек из трех источников с явным приоритетом, раннюю проверку корректности конфигурации и базовую защиту веб-сервиса.

Реализовано:
- приоритет источников настроек (файл -> переменные окружения -> аргументы запуска);
- ранняя валидация конфигурации с запретом запуска при ошибках;
- CORS только для доверенных источников;
- ограничение частоты запросов (разные лимиты для чтения и записи);
- защитные заголовки ответа;
- два режима работы: учебный и боевой.

## Приоритет настроек
1. `appsettings.json`
2. Переменные окружения
3. Аргументы командной строки

Последний источник имеет наивысший приоритет и переопределяет предыдущие.

## Критичные настройки
- `App:AllowedOrigins` — список доверенных источников. Ошибка или пустой список блокируют запуск.
- `App:RateLimits` — лимиты запросов; нулевые/отрицательные значения запрещены.
- `App:Mode` — режим работы, влияет на строгость сообщений и логирование.

Ранняя проверка позволяет не запускать сервис с опасной или ошибочной конфигурацией, снижая риск неконтролируемого доступа.

## Запуск
### Учебный режим
```powershell
# через Taskfile
task run:training

# напрямую
dotnet run --project src/Task3.Web -- --App:Mode=Training
```

### Боевой режим
```powershell
# через Taskfile
task run:production

# напрямую
$env:APP__MODE = "Production"
dotnet run --project src/Task3.Web
```

## Примеры запросов
```powershell
# получить список
Invoke-RestMethod -Method Get -Uri http://localhost:5000/items

# создать элемент
Invoke-RestMethod -Method Post -Uri http://localhost:5000/items -ContentType "application/json" -Body '{"name":"demo"}'
```

## Настройка CORS (доверенные источники)
Пример через переменные окружения:
```powershell
$env:APP__ALLOWEDORIGINS__0 = "http://localhost:5173"
$env:APP__ALLOWEDORIGINS__1 = "http://localhost:3000"
```

## Лимиты запросов
Разные лимиты для чтения и записи задаются в `appsettings.json`:
```json
"RateLimits": {
  "Read": { "PermitLimit": 30, "WindowSeconds": 60, "QueueLimit": 0 },
  "Write": { "PermitLimit": 10, "WindowSeconds": 60, "QueueLimit": 0 }
}
```

## Тесты
```powershell
# все тесты
task test

# тесты с учебным режимом
task test:training

# тесты с боевым режимом
task test:production
```

## Пояснение к режимам
- **Учебный режим**: подробные сообщения об ошибках, мягкие лимиты, больше информации для диагностики.
- **Боевой режим**: короткие сообщения, строгие лимиты и минимум деталей в ответах.

Переключение режимов выполняется только через настройки.

## Тестирование CORS из браузера

### Запуск HTML-страницы
1. Запусти API (см. раздел "Запуск").
2. Запусти тестовый фронт:

```powershell
task web:serve
```

3. Открой в браузере:

```text
http://localhost:5173/test-cors.html
```

На странице есть кнопки для проверки:
- доступ к `GET /items`
- создание `POST /items`
- проверка security headers
- спам-запросы для rate limiting

### Примеры fetch в консоли браузера

Доверенный origin:
```javascript
fetch('http://localhost:5000/items', {
  method: 'GET',
  headers: {
    'Origin': 'http://localhost:5173'
  }
})
.then(r => r.json())
.then(d => console.log('✓ localhost:5173 РАЗРЕШЕН:', d))
.catch(e => console.error('✗ Ошибка:', e));
```

Недоверенный origin:
```javascript
fetch('http://localhost:5000/items', {
  method: 'GET',
  headers: {
    'Origin': 'http://evil.test'
  }
})
.then(r => r.json())
.then(d => console.log('✗ ДОЛЖНО БЫТЬ БЛОКИРОВАНО, но вы видите:', d))
.catch(e => console.log('✓ Браузер заблокировал CORS:', e.message));
```

Проверка защитных заголовков:
```javascript
fetch('http://localhost:5000/items', {
  method: 'GET',
  headers: {
    'Origin': 'http://localhost:5173'
  }
})
.then(r => {
  console.log('X-Content-Type-Options:', r.headers.get('X-Content-Type-Options'));
  console.log('X-Frame-Options:', r.headers.get('X-Frame-Options'));
  console.log('Cache-Control:', r.headers.get('Cache-Control'));
  console.log('Access-Control-Allow-Origin:', r.headers.get('Access-Control-Allow-Origin'));
  return r.json();
})
.then(d => console.log('Ответ:', d));
```

Проверка rate limiting:
```javascript
for (let i = 0; i < 50; i++) {
  fetch('http://localhost:5000/items', {
    method: 'GET',
    headers: {
      'Origin': 'http://localhost:5173'
    }
  })
  .then(r => console.log(`Запрос ${i + 1}: статус ${r.status}`));
}
```

Репозиторий с проектом: https://github.com/Yaroslafffchik/config_checker/tree/main