import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:goschool_mobile/auth_screens.dart';
import 'package:goschool_mobile/widgets.dart';

void main() {
  testWidgets('login screen offers email, password, login and sign-up', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: LoginScreen()));
    expect(find.text('Email'), findsOneWidget);
    expect(find.text('Password'), findsOneWidget);
    expect(find.text('Log in'), findsOneWidget);
    expect(find.text('Create account'), findsOneWidget);
  });

  testWidgets('register screen switches between parent and driver fields', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: RegisterScreen()));
    expect(find.text('Home address'), findsOneWidget);
    expect(find.text('Vehicle registration number'), findsNothing);
    await tester.tap(find.text('Driver'));
    await tester.pumpAndSettle();
    expect(find.text('Vehicle registration number'), findsOneWidget);
    expect(find.text('Home address'), findsNothing);
  });

  test('formatting helpers', () {
    expect(ym(2026, 3), '2026-03');
    expect(hhmm('07:15:00'), '07:15');
    expect(() => parseNum('Capacity', 'abc'), throwsA(isA<Object>()));
    expect(parseNum('Capacity', ' 12 '), 12);
  });
}
