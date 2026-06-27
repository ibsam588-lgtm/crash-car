import 'package:crash_car/src/app.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('home screen renders Crash Car title', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(const ProviderScope(child: CrashCarApp()));
    await tester.pumpAndSettle();

    expect(find.byType(RichText), findsWidgets);
    expect(find.text('Classic Arena'), findsOneWidget);
    expect(find.text('Ramp Launch'), findsOneWidget);

    await tester.tap(find.text('Classic Arena'));
    await tester.pumpAndSettle();

    expect(find.text('Select Arena'), findsOneWidget);
    expect(find.text('Construction Yard'), findsOneWidget);
  });
}
