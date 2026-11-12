import 'package:flutter/material.dart';

import 'api.dart';

final api = Api.instance;

const coordinateKeyboard = TextInputType.numberWithOptions(decimal: true, signed: true);

String ym(Object year, Object month) => '$year-${month.toString().padLeft(2, '0')}';
String hhmm(String time) => time.length >= 5 ? time.substring(0, 5) : time;
String money(Object? amount) => 'LKR $amount';

num parseNum(String label, String text) {
  final value = num.tryParse(text.trim());
  if (value == null) throw ApiException(0, 'Enter a number for $label');
  return value;
}

void showMessage(BuildContext context, String message) {
  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
}

/// Runs [action]; on failure shows the error as a snackbar and returns null.
Future<T?> run<T>(BuildContext context, Future<T> Function() action) async {
  try {
    return await action();
  } catch (e) {
    if (context.mounted) showMessage(context, e.toString());
    return null;
  }
}

Future<bool> confirm(BuildContext context, String message) async =>
    await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        content: Text(message),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('OK')),
        ],
      ),
    ) ??
    false;

Widget field(TextEditingController controller, String label, {TextInputType? keyboard, bool obscure = false}) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: TextField(
        controller: controller,
        obscureText: obscure,
        keyboardType: keyboard,
        decoration: InputDecoration(labelText: label, border: const OutlineInputBorder()),
      ),
    );

Widget notificationsButton(BuildContext context) => IconButton(
      icon: const Icon(Icons.notifications),
      tooltip: 'Notifications',
      onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const NotificationsScreen())),
    );

/// Loads data once and renders it; shows a spinner, or the error with a Retry button.
class Loader<T> extends StatefulWidget {
  const Loader({super.key, required this.load, required this.builder});
  final Future<T> Function() load;
  final Widget Function(BuildContext context, T data, VoidCallback reload) builder;

  @override
  State<Loader<T>> createState() => _LoaderState<T>();
}

class _LoaderState<T> extends State<Loader<T>> {
  late Future<T> _future = widget.load();

  void _reload() {
    if (mounted) setState(() => _future = widget.load());
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<T>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Padding(padding: const EdgeInsets.all(16), child: Text('${snapshot.error}', textAlign: TextAlign.center)),
                  TextButton(onPressed: _reload, child: const Text('Retry')),
                ],
              ),
            );
          }
          if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
          return widget.builder(context, snapshot.data as T, _reload);
        },
      );
}

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Notifications')),
        body: Loader<dynamic>(
          load: () => api.get('/notifications'),
          builder: (context, data, reload) {
            final items = data as List;
            if (items.isEmpty) return const Center(child: Text('No notifications'));
            return ListView(
              children: [
                for (final n in items)
                  ListTile(
                    leading: Icon(n['isRead'] == true ? Icons.drafts : Icons.mark_email_unread),
                    title: Text(n['message']),
                    subtitle: Text(n['createdAt'].toString().substring(0, 16).replaceFirst('T', ' ')),
                    onTap: n['isRead'] == true
                        ? null
                        : () async {
                            if (await run(context, () => api.post('/notifications/${n['id']}/read')) != null) reload();
                          },
                  ),
              ],
            );
          },
        ),
      );
}
