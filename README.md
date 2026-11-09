# GoSchool Sri Lanka — School Van Management System

Monorepo for the platform described in [docs/School_Van_Management_System_Requirements.md](docs/School_Van_Management_System_Requirements.md).

## Structure

```
backend/      ASP.NET Core Web API (C#) + EF Core / PostgreSQL
web-admin/    React + TypeScript admin portal (Vite)
mobile/       Flutter app — Driver & Parent interfaces (not yet scaffolded, see mobile/README.md)
docs/         Requirements and other project docs
```

## Getting started

### Backend (`backend/`)

```bash
cd backend
dotnet restore
# create/update the local Postgres schema (requires Postgres running + connection string in appsettings.json)
dotnet ef database update --project src/GoSchool.Api
dotnet run --project src/GoSchool.Api
```
API runs at `http://localhost:5133`, Swagger/OpenAPI at `/openapi/v1.json` in dev.

### Web Admin (`web-admin/`)

```bash
cd web-admin
cp .env.example .env
npm install
npm run dev
```
Runs at `http://localhost:5173`.

### Mobile (`mobile/`)

Not yet scaffolded — see [mobile/README.md](mobile/README.md) for the Flutter
SDK fix needed on this machine and the `flutter create` command to run.

### Run everything at once

Open this folder in VS Code and run the **Run All (Backend + Web Admin + Mobile)**
task (`Cmd+Shift+P` → "Run Task"), defined in `.vscode/tasks.json`.
