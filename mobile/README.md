# Mobile App (Flutter)

Not yet scaffolded — the Flutter SDK on this machine isn't runnable yet.

`~/.zshrc` adds `/Users/lelum/Development/Flutter/bin` to `PATH`, but that
directory is an incomplete checkout of the `flutter/flutter` repo (no
`bin/flutter` executable present). Fix options:

1. Re-clone/reset the SDK:
   ```bash
   rm -rf ~/Development/Flutter
   git clone https://github.com/flutter/flutter.git -b stable ~/Development/Flutter
   flutter doctor
   ```
2. Or install via a version manager (e.g. [fvm](https://fvm.app/)) and point
   `PATH` at its shim instead.

Once `flutter doctor` runs cleanly, scaffold the app from the repo root:

```bash
cd mobile
flutter create --org com.goschool --project-name goschool_mobile .
```

This app will hold **both** the Driver and Parent interfaces (role selected
at login), per the requirements doc. Suggested structure once created:

```
mobile/
  lib/
    main.dart
    core/            # API client, theming, shared config
    features/
      auth/
      driver/         # Route & pricing config, applications, payments
      parent/          # Student profiles, search, seat applications, payments
    shared/
      widgets/
      models/
```

Recommended packages: `dio` or `http` (API client), `go_router` (navigation),
`flutter_riverpod` or `provider` (state management), `flutter_secure_storage`
(auth tokens), `image_picker` (vehicle photos / payment slips).
