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
    expect(find.byIcon(Icons.play_arrow_rounded), findsOneWidget);
  });
}
