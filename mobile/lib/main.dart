import 'package:flutter/material.dart';

import 'auth_screens.dart';
import 'widgets.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await api.loadToken();
  runApp(const GoSchoolApp());
}

class GoSchoolApp extends StatelessWidget {
  const GoSchoolApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
        title: 'GoSchool',
        theme: ThemeData(colorSchemeSeed: Colors.blue),
        home: const StartScreen(),
      );
}

/// Sends a remembered session straight to its home screen, anyone else to login.
class StartScreen extends StatefulWidget {
  const StartScreen({super.key});

  @override
  State<StartScreen> createState() => _StartScreenState();
}

class _StartScreenState extends State<StartScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _start());
  }

  Future<void> _start() async {
    if (api.token != null) {
      try {
        final me = await api.get('/auth/me');
        if (mounted) await enterApp(context, me['user']);
        return;
      } catch (_) {
        await api.saveToken(null);
      }
    }
    if (mounted) {
      Navigator.of(context).pushAndRemoveUntil(MaterialPageRoute(builder: (_) => const LoginScreen()), (_) => false);
    }
  }

  @override
  Widget build(BuildContext context) => const Scaffold(body: Center(child: CircularProgressIndicator()));
}
