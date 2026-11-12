import 'package:flutter/material.dart';

import 'auth_screens.dart';
import 'widgets.dart';

class DriverHome extends StatefulWidget {
  const DriverHome({super.key});

  @override
  State<DriverHome> createState() => _DriverHomeState();
}

class _DriverHomeState extends State<DriverHome> {
  int _tab = 0;
  static const _titles = ['Home', 'Routes', 'Requests', 'Payments', 'Profile'];

  @override
  Widget build(BuildContext context) {
    final tabs = [const _SummaryTab(), const _RoutesTab(), const _RequestsTab(), const _PaymentsTab(), const _ProfileTab()];
    return Scaffold(
      appBar: AppBar(title: Text(_titles[_tab]), actions: [notificationsButton(context)]),
      body: tabs[_tab],
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _tab,
        type: BottomNavigationBarType.fixed,
        onTap: (i) => setState(() => _tab = i),
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.home), label: 'Home'),
          BottomNavigationBarItem(icon: Icon(Icons.alt_route), label: 'Routes'),
          BottomNavigationBarItem(icon: Icon(Icons.inbox), label: 'Requests'),
          BottomNavigationBarItem(icon: Icon(Icons.payments), label: 'Payments'),
          BottomNavigationBarItem(icon: Icon(Icons.person), label: 'Profile'),
        ],
      ),
    );
  }
}

class _SummaryTab extends StatelessWidget {
  const _SummaryTab();

  @override
  Widget build(BuildContext context) => Loader<dynamic>(
        load: () => api.get('/driver/me'),
        builder: (context, me, reload) {
          final vehicle = me['vehicle'];
          final sub = me['subscription'];
          return ListView(
            children: [
              ListTile(leading: const Icon(Icons.person), title: Text(me['user']['fullName']), subtitle: Text(me['user']['email'])),
              ListTile(
                leading: const Icon(Icons.directions_bus),
                title: Text(vehicle == null ? 'No vehicle' : '${vehicle['registrationNumber']} · ${vehicle['capacity']} seats'),
              ),
              ListTile(leading: const Icon(Icons.event_seat), title: Text('Seats taken: ${me['acceptedSeats']} / ${vehicle?['capacity'] ?? '?'}')),
              ListTile(leading: const Icon(Icons.inbox), title: Text('Pending seat requests: ${me['pendingRequests']}')),
              ListTile(
                leading: const Icon(Icons.card_membership),
                title: const Text('GoSchool subscription (this month)'),
                subtitle: Text(sub == null ? 'Not billed yet' : '${ym(sub['billingYear'], sub['billingMonth'])}: ${money(sub['amount'])} — ${sub['status']}'),
              ),
            ],
          );
        },
      );
}

class _RoutesTab extends StatelessWidget {
  const _RoutesTab();

  @override
  Widget build(BuildContext context) => Loader<dynamic>(
        load: () => api.get('/driver/routes'),
        builder: (context, data, reload) {
          final routes = data as List;
          Future<void> open(Widget form) async {
            if (await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => form)) == true) reload();
          }

          return Scaffold(
            floatingActionButton: FloatingActionButton(onPressed: () => open(const RouteForm()), child: const Icon(Icons.add)),
            body: routes.isEmpty
                ? const Center(child: Text('No routes yet. Add one with +.'))
                : ListView(
                    children: [
                      for (final r in routes)
                        ExpansionTile(
                          title: Text(r['name']),
                          subtitle: Text('${money(r['pricePerMonth'])}/month · ${money(r['pricePerDay'])}/day'),
                          children: [
                            for (final s in r['stops'])
                              ListTile(
                                dense: true,
                                leading: Text('${s['sequence']}'),
                                title: Text('${s['school']} · ${hhmm(s['arrivalTime'])}'),
                                subtitle: Text(s['pickupAddress']),
                                onTap: () => open(StopForm(routeId: r['id'], stop: s)),
                                trailing: IconButton(
                                  icon: const Icon(Icons.delete_outline),
                                  onPressed: () async {
                                    if (!await confirm(context, 'Delete this stop?') || !context.mounted) return;
                                    if (await run(context, () => api.delete('/driver/stops/${s['id']}')) != null) reload();
                                  },
                                ),
                              ),
                            OverflowBar(
                              children: [
                                TextButton(onPressed: () => open(StopForm(routeId: r['id'])), child: const Text('Add stop')),
                                TextButton(onPressed: () => open(RouteForm(route: r)), child: const Text('Edit route')),
                                TextButton(
                                  onPressed: () async {
                                    if (!await confirm(context, 'Delete route ${r['name']}?') || !context.mounted) return;
                                    if (await run(context, () => api.delete('/driver/routes/${r['id']}')) != null) reload();
                                  },
                                  child: const Text('Delete route'),
                                ),
                              ],
                            ),
                          ],
                        ),
                    ],
                  ),
          );
        },
      );
}

class RouteForm extends StatefulWidget {
  const RouteForm({super.key, this.route});
  final Map? route;

  @override
  State<RouteForm> createState() => _RouteFormState();
}

class _RouteFormState extends State<RouteForm> {
  late final _name = TextEditingController(text: widget.route?['name'] ?? '');
  late final _day = TextEditingController(text: widget.route?['pricePerDay']?.toString() ?? '');
  late final _month = TextEditingController(text: widget.route?['pricePerMonth']?.toString() ?? '');

  Future<void> _save() async {
    Map<String, dynamic> body() => {
          'name': _name.text,
          'pricePerDay': parseNum('Price per day', _day.text),
          'pricePerMonth': parseNum('Price per month', _month.text),
        };
    final saved = await run(
      context,
      () => widget.route == null ? api.post('/driver/routes', body()) : api.put('/driver/routes/${widget.route!['id']}', body()),
    );
    if (saved != null && mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: Text(widget.route == null ? 'New route' : 'Edit route')),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            field(_name, 'Route name'),
            field(_day, 'Price per day (LKR)', keyboard: TextInputType.number),
            field(_month, 'Price per month (LKR)', keyboard: TextInputType.number),
            const SizedBox(height: 8),
            FilledButton(onPressed: _save, child: const Text('Save')),
          ],
        ),
      );
}

class StopForm extends StatefulWidget {
  const StopForm({super.key, required this.routeId, this.stop});
  final int routeId;
  final Map? stop;

  @override
  State<StopForm> createState() => _StopFormState();
}

class _StopFormState extends State<StopForm> {
  late final _sequence = TextEditingController(text: widget.stop?['sequence']?.toString() ?? '');
  late final _school = TextEditingController(text: widget.stop?['school'] ?? '');
  late final _address = TextEditingController(text: widget.stop?['pickupAddress'] ?? '');
  late final _latitude = TextEditingController(text: widget.stop?['latitude']?.toString() ?? '');
  late final _longitude = TextEditingController(text: widget.stop?['longitude']?.toString() ?? '');
  late TimeOfDay _time = _parse(widget.stop?['arrivalTime']);

  static TimeOfDay _parse(String? time) {
    if (time == null) return const TimeOfDay(hour: 7, minute: 0);
    final parts = time.split(':');
    return TimeOfDay(hour: int.parse(parts[0]), minute: int.parse(parts[1]));
  }

  String get _timeText => '${_time.hour.toString().padLeft(2, '0')}:${_time.minute.toString().padLeft(2, '0')}';

  Future<void> _save() async {
    Map<String, dynamic> body() => {
          'sequence': parseNum('Stop number', _sequence.text).toInt(),
          'school': _school.text,
          'pickupAddress': _address.text,
          'latitude': parseNum('Latitude', _latitude.text),
          'longitude': parseNum('Longitude', _longitude.text),
          'arrivalTime': _timeText,
        };
    final saved = await run(
      context,
      () => widget.stop == null
          ? api.post('/driver/routes/${widget.routeId}/stops', body())
          : api.put('/driver/stops/${widget.stop!['id']}', body()),
    );
    if (saved != null && mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: Text(widget.stop == null ? 'New stop' : 'Edit stop')),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            field(_sequence, 'Stop number (order on the route)', keyboard: TextInputType.number),
            field(_school, 'School'),
            field(_address, 'Pickup address'),
            field(_latitude, 'Latitude', keyboard: coordinateKeyboard),
            field(_longitude, 'Longitude', keyboard: coordinateKeyboard),
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text('Arrival time: $_timeText'),
              trailing: const Icon(Icons.schedule),
              onTap: () async {
                final picked = await showTimePicker(context: context, initialTime: _time);
                if (picked != null) setState(() => _time = picked);
              },
            ),
            const SizedBox(height: 8),
            FilledButton(onPressed: _save, child: const Text('Save')),
          ],
        ),
      );
}

class _RequestsTab extends StatefulWidget {
  const _RequestsTab();

  @override
  State<_RequestsTab> createState() => _RequestsTabState();
}

class _RequestsTabState extends State<_RequestsTab> {
  String _status = 'Pending';

  Future<void> _decide(BuildContext context, Object id, String action, VoidCallback reload) async {
    if (await run(context, () => api.post('/driver/applications/$id/$action')) != null) reload();
  }

  @override
  Widget build(BuildContext context) => Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: DropdownButton<String>(
              value: _status,
              isExpanded: true,
              items: [for (final s in ['Pending', 'Accepted', 'Declined']) DropdownMenuItem(value: s, child: Text(s))],
              onChanged: (v) => setState(() => _status = v!),
            ),
          ),
          Expanded(
            child: Loader<dynamic>(
              key: ValueKey(_status),
              load: () => api.get('/driver/applications?status=$_status'),
              builder: (context, data, reload) {
                final apps = data as List;
                if (apps.isEmpty) return Center(child: Text('No $_status requests'));
                return ListView(
                  children: [
                    for (final a in apps)
                      Card(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            ListTile(
                              title: Text('${a['student']['name']} — ${a['student']['school']}'),
                              subtitle: Text('Parent: ${a['parent']['fullName']} · ${a['parent']['phoneNumber']}\n'
                                  'Stop: ${a['stop']['school']} at ${hhmm(a['stop']['arrivalTime'])}\n'
                                  'Pickup: ${a['stop']['pickupAddress']}'),
                              isThreeLine: true,
                            ),
                            if (a['status'] == 'Pending')
                              OverflowBar(
                                children: [
                                  TextButton(onPressed: () => _decide(context, a['id'], 'decline', reload), child: const Text('Decline')),
                                  FilledButton(onPressed: () => _decide(context, a['id'], 'accept', reload), child: const Text('Accept')),
                                ],
                              ),
                          ],
                        ),
                      ),
                  ],
                );
              },
            ),
          ),
        ],
      );
}

class _PaymentsTab extends StatefulWidget {
  const _PaymentsTab();

  @override
  State<_PaymentsTab> createState() => _PaymentsTabState();
}

class _PaymentsTabState extends State<_PaymentsTab> {
  DateTime _period = DateTime(DateTime.now().year, DateTime.now().month);

  void _shift(int months) => setState(() => _period = DateTime(_period.year, _period.month + months));

  Future<void> _remind() async {
    final result = await run(context, () => api.post('/driver/reminders', {'billingYear': _period.year, 'billingMonth': _period.month}));
    if (result != null && mounted) showMessage(context, 'Reminders sent: ${result['sent']}');
  }

  Widget _tile(BuildContext context, dynamic item, VoidCallback reload) {
    final p = item['payment'];
    final Widget trailing;
    if (p == null) {
      trailing = TextButton(
        onPressed: () async {
          final body = {'applicationId': item['applicationId'], 'billingYear': _period.year, 'billingMonth': _period.month};
          if (await run(context, () => api.post('/driver/payments', body)) != null) reload();
        },
        child: const Text('Log cash'),
      );
    } else if (p['status'] == 'Pending') {
      trailing = FilledButton(
        onPressed: () async {
          if (await run(context, () => api.post('/driver/payments/${p['id']}/confirm')) != null) reload();
        },
        child: const Text('Confirm'),
      );
    } else {
      trailing = const Icon(Icons.check_circle, color: Colors.green);
    }
    return Card(
      child: Column(
        children: [
          ListTile(
            title: Text('${item['student']} · ${item['parent']['fullName']}'),
            subtitle: Text('${item['route']} · ${money(item['amountDue'])}\n${p == null ? 'Unpaid' : '${p['status']} via ${p['method']}'}'),
            isThreeLine: true,
            trailing: trailing,
          ),
          if (p?['slipImageUrl'] != null)
            Padding(
              padding: const EdgeInsets.all(8),
              child: Image.network(p['slipImageUrl'], height: 140, errorBuilder: (_, _, _) => const Text('Slip image unavailable')),
            ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) => Column(
        children: [
          Row(
            children: [
              IconButton(icon: const Icon(Icons.chevron_left), onPressed: () => _shift(-1)),
              Text(ym(_period.year, _period.month), style: Theme.of(context).textTheme.titleMedium),
              IconButton(icon: const Icon(Icons.chevron_right), onPressed: () => _shift(1)),
              const Spacer(),
              TextButton.icon(onPressed: _remind, icon: const Icon(Icons.notifications_active), label: const Text('Remind unpaid')),
            ],
          ),
          Expanded(
            child: Loader<dynamic>(
              key: ValueKey(_period),
              load: () => api.get('/driver/payments?year=${_period.year}&month=${_period.month}'),
              builder: (context, data, reload) {
                final items = data['items'] as List;
                if (items.isEmpty) return const Center(child: Text('No accepted students yet'));
                return ListView(children: [for (final item in items) _tile(context, item, reload)]);
              },
            ),
          ),
        ],
      );
}

class _ProfileTab extends StatelessWidget {
  const _ProfileTab();

  @override
  Widget build(BuildContext context) => Loader<List<dynamic>>(
        load: () => Future.wait([api.get('/driver/me'), api.get('/driver/subscriptions')]),
        builder: (context, data, reload) => _ProfileBody(me: data[0], subscriptions: data[1] as List),
      );
}

class _ProfileBody extends StatefulWidget {
  const _ProfileBody({required this.me, required this.subscriptions});
  final Map me;
  final List subscriptions;

  @override
  State<_ProfileBody> createState() => _ProfileBodyState();
}

class _ProfileBodyState extends State<_ProfileBody> {
  late final _bankName = TextEditingController(text: widget.me['bank']['bankName'] ?? '');
  late final _bankBranch = TextEditingController(text: widget.me['bank']['bankBranch'] ?? '');
  late final _bankAccount = TextEditingController(text: widget.me['bank']['bankAccountNumber'] ?? '');

  Future<void> _saveBank() async {
    final body = {'bankName': _bankName.text, 'bankBranch': _bankBranch.text, 'bankAccountNumber': _bankAccount.text};
    if (await run(context, () => api.put('/driver/bank', body)) != null && mounted) showMessage(context, 'Bank details saved');
  }

  @override
  Widget build(BuildContext context) {
    final user = widget.me['user'];
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(user['fullName'], style: Theme.of(context).textTheme.headlineSmall),
        Text('${user['email']} · ${user['phoneNumber']}'),
        const SizedBox(height: 16),
        Text('Bank details (shown to parents for transfers)', style: Theme.of(context).textTheme.titleMedium),
        field(_bankName, 'Bank name'),
        field(_bankBranch, 'Branch'),
        field(_bankAccount, 'Account number'),
        FilledButton(onPressed: _saveBank, child: const Text('Save bank details')),
        const SizedBox(height: 24),
        Text('GoSchool subscription', style: Theme.of(context).textTheme.titleMedium),
        if (widget.subscriptions.isEmpty) const Text('Nothing billed yet.'),
        for (final s in widget.subscriptions)
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(ym(s['billingYear'], s['billingMonth'])),
            subtitle: Text('${money(s['amount'])} — ${s['status']}'),
            trailing: Icon(s['status'] == 'Completed' ? Icons.check_circle : Icons.pending,
                color: s['status'] == 'Completed' ? Colors.green : Colors.orange),
          ),
        const SizedBox(height: 24),
        OutlinedButton(onPressed: () => logout(context), child: const Text('Log out')),
      ],
    );
  }
}
