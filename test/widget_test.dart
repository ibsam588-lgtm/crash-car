import 'package:crash_car/src/app.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('home screen renders selected impact home', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(const ProviderScope(child: CrashCarApp()));
    await tester.pump(const Duration(milliseconds: 120));

    expect(find.text('IMPACT RUN'), findsOneWidget);
    expect(find.text('12,750'), findsOneWidget);
    expect(find.text('183'), findsOneWidget);
    expect(find.text('KM/H'), findsOneWidget);

    await tester.tap(find.byTooltip('Menu'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 450));

    expect(find.text('Classic'), findsOneWidget);
    expect(find.text('Ramp'), findsOneWidget);
    expect(find.text('Garage'), findsOneWidget);
    expect(find.text('Store'), findsOneWidget);

    await tester.ensureVisible(find.text('Classic'));
    await tester.pump(const Duration(milliseconds: 120));
    await tester.tap(find.text('Classic'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 450));

    expect(find.text('Select Arena'), findsOneWidget);
    expect(find.text('Construction Yard'), findsOneWidget);
  });
}
