import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import 'auth_screens.dart';
import 'widgets.dart';

class ParentHome extends StatefulWidget {
  const ParentHome({super.key});

  @override
  State<ParentHome> createState() => _ParentHomeState();
}

class _ParentHomeState extends State<ParentHome> {
  int _tab = 0;
  static const _titles = ['My children', 'Find a van', 'Seat requests', 'Payments', 'Profile'];

  @override
  Widget build(BuildContext context) {
    final tabs = [const _StudentsTab(), const _FindTab(), const _ApplicationsTab(), const _PaymentsTab(), const _ProfileTab()];
    return Scaffold(
      appBar: AppBar(title: Text(_titles[_tab]), actions: [notificationsButton(context)]),
      body: tabs[_tab],
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _tab,
        type: BottomNavigationBarType.fixed,
        onTap: (i) => setState(() => _tab = i),
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.child_care), label: 'Children'),
          BottomNavigationBarItem(icon: Icon(Icons.search), label: 'Find'),
          BottomNavigationBarItem(icon: Icon(Icons.event_seat), label: 'Requests'),
          BottomNavigationBarItem(icon: Icon(Icons.payments), label: 'Payments'),
          BottomNavigationBarItem(icon: Icon(Icons.person), label: 'Profile'),
        ],
      ),
    );
  }
}

class _StudentsTab extends StatelessWidget {
  const _StudentsTab();

  @override
  Widget build(BuildContext context) => Loader<dynamic>(
        load: () => api.get('/parent/students'),
        builder: (context, data, reload) {
          final students = data as List;
          Future<void> open(Widget form) async {
            if (await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => form)) == true) reload();
          }

          return Scaffold(
            floatingActionButton: FloatingActionButton(onPressed: () => open(const StudentForm()), child: const Icon(Icons.add)),
            body: students.isEmpty
                ? const Center(child: Text('Add your child with + to start looking for a van.'))
                : ListView(
                    children: [
                      for (final s in students)
                        ListTile(
                          title: Text(s['name']),
                          subtitle: Text('${s['school']} · ${s['schoolAddress']}'),
                          onTap: () => open(StudentForm(student: s)),
                          trailing: IconButton(
                            icon: const Icon(Icons.delete_outline),
                            onPressed: () async {
                              if (!await confirm(context, 'Remove ${s['name']}?') || !context.mounted) return;
                              if (await run(context, () => api.delete('/parent/students/${s['id']}')) != null) reload();
                            },
                          ),
                        ),
                    ],
                  ),
          );
        },
      );
}

class StudentForm extends StatefulWidget {
  const StudentForm({super.key, this.student});
  final Map? student;

  @override
  State<StudentForm> createState() => _StudentFormState();
}

class _StudentFormState extends State<StudentForm> {
  late final _name = TextEditingController(text: widget.student?['name'] ?? '');
  late final _school = TextEditingController(text: widget.student?['school'] ?? '');
  late final _address = TextEditingController(text: widget.student?['schoolAddress'] ?? '');
  late final _latitude = TextEditingController(text: widget.student?['schoolLatitude']?.toString() ?? '');
  late final _longitude = TextEditingController(text: widget.student?['schoolLongitude']?.toString() ?? '');
  late final _primary = TextEditingController(text: widget.student?['primaryContactNumber'] ?? '');
  late final _emergency = TextEditingController(text: widget.student?['emergencyContactNumber'] ?? '');

  Future<void> _save() async {
    Map<String, dynamic> body() => {
          'name': _name.text,
          'school': _school.text,
          'schoolAddress': _address.text,
          'schoolLatitude': parseNum('School latitude', _latitude.text),
          'schoolLongitude': parseNum('School longitude', _longitude.text),
          'primaryContactNumber': _primary.text,
          'emergencyContactNumber': _emergency.text,
        };
    final saved = await run(
      context,
      () => widget.student == null ? api.post('/parent/students', body()) : api.put('/parent/students/${widget.student!['id']}', body()),
    );
    if (saved != null && mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: Text(widget.student == null ? 'Add child' : 'Edit child')),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            field(_name, 'Child\'s name'),
            field(_school, 'School'),
            field(_address, 'School address'),
            field(_latitude, 'School latitude', keyboard: coordinateKeyboard),
            field(_longitude, 'School longitude', keyboard: coordinateKeyboard),
            field(_primary, 'Primary contact number', keyboard: TextInputType.phone),
            field(_emergency, 'Emergency contact number', keyboard: TextInputType.phone),
            const SizedBox(height: 8),
            FilledButton(onPressed: _save, child: const Text('Save')),
          ],
        ),
      );
}

class _FindTab extends StatefulWidget {
  const _FindTab();

  @override
  State<_FindTab> createState() => _FindTabState();
}

class _FindTabState extends State<_FindTab> {
  final _school = TextEditingController();
  final _radius = TextEditingController(text: '10');
  String _query = '?radiusKm=10';

  void _search() => setState(() {
        _query = '?radiusKm=${Uri.encodeQueryComponent(_radius.text)}&school=${Uri.encodeQueryComponent(_school.text)}';
      });

  Future<void> _apply(BuildContext context, Map card) async {
    final sent = await showDialog<bool>(context: context, builder: (_) => _ApplyDialog(card: card));
    if (sent == true && context.mounted) showMessage(context, 'Seat request sent. The driver will accept or decline it.');
  }

  Widget _card(BuildContext context, Map c) {
    final stops = c['route']['stops'] as List;
    final photos = c['vehicle']['photoUrls'] as List;
    return Card(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (photos.isNotEmpty)
            Image.network(photos.first, height: 140, width: double.infinity, fit: BoxFit.cover, errorBuilder: (_, _, _) => const SizedBox.shrink()),
          ListTile(
            title: Text('${c['driver']['fullName']} · ${c['route']['name']}'),
            subtitle: Text('${c['distanceKm']} km from home · ${c['seatsAvailable']} seats free\n'
                '${money(c['route']['pricePerMonth'])}/month · ${money(c['route']['pricePerDay'])}/day\n'
                'Van ${c['vehicle']['registrationNumber']} · ${c['driver']['phoneNumber']}'),
            isThreeLine: true,
          ),
          ExpansionTile(
            title: Text('Stops (${stops.length})'),
            children: [
              for (final s in stops)
                ListTile(dense: true, leading: Text('${s['sequence']}'), title: Text('${s['school']} · ${hhmm(s['arrivalTime'])}'), subtitle: Text(s['pickupAddress'])),
            ],
          ),
          OverflowBar(
            children: [
              FilledButton(onPressed: c['seatsAvailable'] > 0 ? () => _apply(context, c) : null, child: const Text('Apply for seat')),
            ],
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) => Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              children: [
                Expanded(child: field(_school, 'School (optional)')),
                const SizedBox(width: 8),
                SizedBox(width: 90, child: field(_radius, 'Within km', keyboard: TextInputType.number)),
                IconButton(icon: const Icon(Icons.search), onPressed: _search),
              ],
            ),
          ),
          Expanded(
            child: Loader<dynamic>(
              key: ValueKey(_query),
              load: () => api.get('/parent/drivers$_query'),
              builder: (context, data, reload) {
                final cards = data as List;
                if (cards.isEmpty) return const Center(child: Text('No vans found. Try a wider radius.'));
                return ListView(children: [for (final c in cards) _card(context, c)]);
              },
            ),
          ),
        ],
      );
}

class _ApplyDialog extends StatefulWidget {
  const _ApplyDialog({required this.card});
  final Map card;

  @override
  State<_ApplyDialog> createState() => _ApplyDialogState();
}

class _ApplyDialogState extends State<_ApplyDialog> {
  int? _studentId;
  int? _stopId;

  Future<void> _submit() async {
    if (_studentId == null || _stopId == null) {
      showMessage(context, 'Choose a child and a pickup stop');
      return;
    }
    final body = {'studentId': _studentId, 'routeId': widget.card['route']['id'], 'routeStopId': _stopId};
    if (await run(context, () => api.post('/parent/applications', body)) != null && mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    final stops = widget.card['route']['stops'] as List;
    return AlertDialog(
      title: Text('Apply to ${widget.card['driver']['fullName']}'),
      content: SizedBox(
        width: double.maxFinite,
        child: Loader<dynamic>(
          load: () => api.get('/parent/students'),
          builder: (context, data, reload) {
            final students = data as List;
            if (students.isEmpty) return const Text('Add a child first.');
            return Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                DropdownButton<int>(
                  isExpanded: true,
                  hint: const Text('Child'),
                  value: _studentId,
                  items: [for (final s in students) DropdownMenuItem(value: s['id'] as int, child: Text(s['name']))],
                  onChanged: (v) => setState(() => _studentId = v),
                ),
                DropdownButton<int>(
                  isExpanded: true,
                  hint: const Text('Pickup stop'),
                  value: _stopId,
                  items: [
                    for (final s in stops) DropdownMenuItem(value: s['id'] as int, child: Text('${s['school']} · ${hhmm(s['arrivalTime'])}')),
                  ],
                  onChanged: (v) => setState(() => _stopId = v),
                ),
              ],
            );
          },
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
        FilledButton(onPressed: _submit, child: const Text('Apply')),
      ],
    );
  }
}

class _ApplicationsTab extends StatelessWidget {
  const _ApplicationsTab();

  Future<void> _pay(BuildContext context, Map application) async {
    final sent = await showDialog<bool>(context: context, builder: (_) => _PaymentDialog(application: application));
    if (sent == true && context.mounted) showMessage(context, 'Payment submitted. The driver will confirm it.');
  }

  @override
  Widget build(BuildContext context) => Loader<dynamic>(
        load: () => api.get('/parent/applications'),
        builder: (context, data, reload) {
          final apps = data as List;
          if (apps.isEmpty) return const Center(child: Text('No seat requests yet. Find a van to apply.'));
          return ListView(
            children: [
              for (final a in apps)
                Card(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      ListTile(
                        title: Text('${a['student']['name']} · ${a['route']['name']}'),
                        subtitle: Text('Status: ${a['status']}\n'
                            'Pickup: ${a['stop']['school']} at ${hhmm(a['stop']['arrivalTime'])}\n'
                            'Driver: ${a['driver']['fullName']} · ${a['driver']['phoneNumber']}'),
                        isThreeLine: true,
                      ),
                      if (a['driver']['bank'] != null)
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          child: Text('${money(a['route']['pricePerMonth'])}/month · Bank transfer to: '
                              '${a['driver']['bank']['bankName'] ?? '-'} ${a['driver']['bank']['bankBranch'] ?? ''} '
                              '${a['driver']['bank']['bankAccountNumber'] ?? ''}'),
                        ),
                      if (a['status'] == 'Accepted')
                        OverflowBar(children: [FilledButton(onPressed: () => _pay(context, a), child: const Text('Pay this month'))]),
                    ],
                  ),
                ),
            ],
          );
        },
      );
}

class _PaymentDialog extends StatefulWidget {
  const _PaymentDialog({required this.application});
  final Map application;

  @override
  State<_PaymentDialog> createState() => _PaymentDialogState();
}

class _PaymentDialogState extends State<_PaymentDialog> {
  DateTime _period = DateTime(DateTime.now().year, DateTime.now().month);
  String _method = 'Bank';
  String? _slipUrl;
  bool _uploading = false;

  Future<void> _pickSlip() async {
    final picked = await ImagePicker().pickImage(source: ImageSource.gallery);
    if (picked == null || !mounted) return;
    setState(() => _uploading = true);
    final url = await run(context, () => api.upload(File(picked.path)));
    if (mounted) {
      setState(() {
        _slipUrl = url;
        _uploading = false;
      });
    }
  }

  Future<void> _submit() async {
    final body = {
      'applicationId': widget.application['id'],
      'billingYear': _period.year,
      'billingMonth': _period.month,
      'method': _method,
      'slipImageUrl': _slipUrl,
    };
    if (await run(context, () => api.post('/parent/payments', body)) != null && mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
        title: Text('Pay ${money(widget.application['route']['pricePerMonth'])}'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                IconButton(icon: const Icon(Icons.chevron_left), onPressed: () => setState(() => _period = DateTime(_period.year, _period.month - 1))),
                Text(ym(_period.year, _period.month)),
                IconButton(icon: const Icon(Icons.chevron_right), onPressed: () => setState(() => _period = DateTime(_period.year, _period.month + 1))),
              ],
            ),
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'Bank', label: Text('Bank transfer')),
                ButtonSegment(value: 'Cash', label: Text('Paid cash')),
              ],
              selected: {_method},
              onSelectionChanged: (s) => setState(() => _method = s.first),
            ),
            if (_method == 'Bank')
              TextButton.icon(
                onPressed: _uploading ? null : _pickSlip,
                icon: _uploading ? const SizedBox.square(dimension: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.upload_file),
                label: Text(_slipUrl == null ? 'Upload transfer slip' : 'Slip uploaded'),
              ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          FilledButton(onPressed: _submit, child: const Text('Submit')),
        ],
      );
}

class _PaymentsTab extends StatelessWidget {
  const _PaymentsTab();

  @override
  Widget build(BuildContext context) => Loader<dynamic>(
        load: () => api.get('/parent/payments'),
        builder: (context, data, reload) {
          final rows = data as List;
          if (rows.isEmpty) return const Center(child: Text('No payments yet'));
          return ListView(
            children: [
              for (final r in rows)
                ListTile(
                  leading: Icon(r['payment']['status'] == 'Completed' ? Icons.check_circle : Icons.pending,
                      color: r['payment']['status'] == 'Completed' ? Colors.green : Colors.orange),
                  title: Text('${ym(r['payment']['billingYear'], r['payment']['billingMonth'])} · ${r['student']}'),
                  subtitle: Text('${r['driver']} · ${r['route']}\n${money(r['payment']['amount'])} · ${r['payment']['method']} · ${r['payment']['status']}'),
                  isThreeLine: true,
                ),
            ],
          );
        },
      );
}

class _ProfileTab extends StatelessWidget {
  const _ProfileTab();

  @override
  Widget build(BuildContext context) => Loader<dynamic>(
        load: () => api.get('/auth/me'),
        builder: (context, me, reload) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(me['user']['fullName'], style: Theme.of(context).textTheme.headlineSmall),
            Text('${me['user']['email']} · ${me['user']['phoneNumber']}'),
            const SizedBox(height: 24),
            OutlinedButton(onPressed: () => logout(context), child: const Text('Log out')),
          ],
        ),
      );
}
