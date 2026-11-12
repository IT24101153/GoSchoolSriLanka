# GoSchool Mobile (Flutter)

One app for both Driver and Parent; the role is chosen at sign-up. Admin uses the web portal.

## Run

1. Start the API (`backend/src/GoSchool.Api`, listens on `http://localhost:5133`).
2. `flutter run` from this folder.

The API URL is picked automatically: Android emulator → `http://10.0.2.2:5133/api`, iOS simulator → `http://localhost:5133/api`.
For a real phone or another host:

```bash
flutter run --dart-define=API_BASE_URL=http://<your-computer-ip>:5133/api
```

## Layout

- `lib/api.dart` — HTTP client, token storage, file upload
- `lib/auth_screens.dart` — login, sign-up (driver / parent), pending-approval screen
- `lib/driver_screens.dart` — routes & stops, seat requests, payments & reminders, bank details, subscription
- `lib/parent_screens.dart` — children, find a van, seat requests, payments
- `lib/widgets.dart` — shared helpers (`Loader`, form field, notifications)

`flutter test` runs the widget tests without a device.
