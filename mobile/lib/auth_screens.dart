import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import 'driver_screens.dart';
import 'parent_screens.dart';
import 'widgets.dart';

/// Replaces the whole stack with the home screen for [user]'s role and status.
Future<void> enterApp(BuildContext context, Map user) async {
  Widget home;
  if (user['role'] == 'Driver') {
    home = user['status'] == 'Active' ? const DriverHome() : const PendingScreen();
  } else if (user['role'] == 'Parent') {
    home = const ParentHome();
  } else {
    await api.saveToken(null);
    home = const LoginScreen(message: 'Admin accounts use the web portal.');
  }
  if (!context.mounted) return;
  Navigator.of(context).pushAndRemoveUntil(MaterialPageRoute(builder: (_) => home), (_) => false);
}

Future<void> logout(BuildContext context) async {
  try {
    await api.post('/auth/logout');
  } catch (_) {}
  await api.saveToken(null);
  if (!context.mounted) return;
  Navigator.of(context).pushAndRemoveUntil(MaterialPageRoute(builder: (_) => const LoginScreen()), (_) => false);
}

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, this.message});
  final String? message;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _email = TextEditingController();
  final _password = TextEditingController();

  Future<void> _login() async {
    final session = await run(context, () => api.post('/auth/login', {'email': _email.text, 'password': _password.text}));
    if (session == null) return;
    await api.saveToken(session['token']);
    if (mounted) await enterApp(context, session['user']);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('GoSchool')),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (widget.message != null) Padding(padding: const EdgeInsets.only(bottom: 12), child: Text(widget.message!)),
            field(_email, 'Email', keyboard: TextInputType.emailAddress),
            field(_password, 'Password', obscure: true),
            const SizedBox(height: 8),
            FilledButton(onPressed: _login, child: const Text('Log in')),
            TextButton(
              onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const RegisterScreen())),
              child: const Text('Create account'),
            ),
          ],
        ),
      );
}

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  String _role = 'Parent';
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _phone = TextEditingController();
  final _address = TextEditingController();
  final _latitude = TextEditingController();
  final _longitude = TextEditingController();
  final _registration = TextEditingController();
  final _capacity = TextEditingController();
  final _bankName = TextEditingController();
  final _bankBranch = TextEditingController();
  final _bankAccount = TextEditingController();
  List<XFile> _photos = [];

  Map<String, dynamic> get _common => {
        'fullName': _name.text,
        'email': _email.text,
        'password': _password.text,
        'phoneNumber': _phone.text,
      };

  Future<void> _submit() async {
    final session = _role == 'Parent'
        ? await run(context, () => api.post('/auth/register/parent', {
              ..._common,
              'homeAddress': _address.text,
              'homeLatitude': parseNum('Home latitude', _latitude.text),
              'homeLongitude': parseNum('Home longitude', _longitude.text),
            }))
        : await run(context, () => api.post('/auth/register/driver', {
              ..._common,
              'registrationNumber': _registration.text,
              'capacity': parseNum('Passenger capacity', _capacity.text).toInt(),
              'bankName': _bankName.text.isEmpty ? null : _bankName.text,
              'bankBranch': _bankBranch.text.isEmpty ? null : _bankBranch.text,
              'bankAccountNumber': _bankAccount.text.isEmpty ? null : _bankAccount.text,
            }));
    if (session == null) return;
    await api.saveToken(session['token']);

    // Uploads need a token, so van photos go up right after the account exists.
    if (_photos.isNotEmpty && mounted) {
      final urls = await run(context, () async => [for (final photo in _photos) await api.upload(File(photo.path))]);
      if (urls != null && mounted) await run(context, () => api.put('/auth/vehicle-photos', {'photoUrls': urls}));
    }
    if (mounted) await enterApp(context, session['user']);
  }

  Future<void> _pickPhotos() async {
    final picked = await ImagePicker().pickMultiImage();
    if (picked.isNotEmpty) setState(() => _photos = picked);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Create account')),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'Parent', label: Text('Parent'), icon: Icon(Icons.family_restroom)),
                ButtonSegment(value: 'Driver', label: Text('Driver'), icon: Icon(Icons.directions_bus)),
              ],
              selected: {_role},
              onSelectionChanged: (s) => setState(() => _role = s.first),
            ),
            const SizedBox(height: 12),
            field(_name, 'Full name'),
            field(_email, 'Email', keyboard: TextInputType.emailAddress),
            field(_password, 'Password (6+ characters)', obscure: true),
            field(_phone, 'Phone number', keyboard: TextInputType.phone),
            if (_role == 'Parent') ...[
              field(_address, 'Home address'),
              field(_latitude, 'Home latitude', keyboard: coordinateKeyboard),
              field(_longitude, 'Home longitude', keyboard: coordinateKeyboard),
            ] else ...[
              field(_registration, 'Vehicle registration number'),
              field(_capacity, 'Passenger capacity', keyboard: TextInputType.number),
              TextButton.icon(
                onPressed: _pickPhotos,
                icon: const Icon(Icons.add_a_photo),
                label: Text(_photos.isEmpty ? 'Add van photos' : '${_photos.length} photo(s) selected'),
              ),
              field(_bankName, 'Bank name (optional)'),
              field(_bankBranch, 'Bank branch (optional)'),
              field(_bankAccount, 'Bank account number (optional)'),
            ],
            const SizedBox(height: 8),
            FilledButton(onPressed: _submit, child: const Text('Register')),
          ],
        ),
      );
}

class PendingScreen extends StatelessWidget {
  const PendingScreen({super.key});

  Future<void> _check(BuildContext context) async {
    final me = await run(context, () => api.get('/auth/me'));
    if (me == null || !context.mounted) return;
    if (me['user']['status'] == 'Pending') {
      showMessage(context, 'Still waiting for admin approval.');
    } else {
      await enterApp(context, me['user']);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Account under review'), actions: [notificationsButton(context)]),
        body: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.hourglass_top, size: 64),
              const SizedBox(height: 16),
              const Text(
                'Your driver account is waiting for GoSchool admin approval. '
                'You will be able to set up routes once it is approved.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              FilledButton(onPressed: () => _check(context), child: const Text('Check status')),
              TextButton(onPressed: () => logout(context), child: const Text('Log out')),
            ],
          ),
        ),
      );
}
