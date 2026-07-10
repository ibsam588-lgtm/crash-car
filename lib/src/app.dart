import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/screens/home_screen.dart';
import 'package:crash_car/src/screens/impact_run_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class CrashCarApp extends ConsumerStatefulWidget {
  const CrashCarApp({super.key});

  @override
  ConsumerState<CrashCarApp> createState() => _CrashCarAppState();
}

class _CrashCarAppState extends ConsumerState<CrashCarApp> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(progressProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    const amber = Color(0xFFFFC533);
    const cyan = Color(0xFF42C7FF);
    final initialScreen = Uri.base.queryParameters['mode'] == 'impact-run'
        ? ImpactRunScreen(
            arena: arenaById('downtown_strip'),
            car: carById('muscle_orange'),
          )
        : const HomeScreen();

    return MaterialApp(
      title: 'Crash Car',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        brightness: Brightness.dark,
        scaffoldBackgroundColor: const Color(0xFF071014),
        colorScheme: ColorScheme.fromSeed(
          seedColor: amber,
          brightness: Brightness.dark,
          primary: amber,
          secondary: cyan,
          surface: const Color(0xFF0D171D),
        ),
        fontFamily: 'Arial',
        textTheme: const TextTheme(
          headlineLarge: TextStyle(
            fontWeight: FontWeight.w900,
            letterSpacing: 0,
          ),
          headlineMedium: TextStyle(
            fontWeight: FontWeight.w900,
            letterSpacing: 0,
          ),
          titleLarge: TextStyle(fontWeight: FontWeight.w800, letterSpacing: 0),
          titleMedium: TextStyle(fontWeight: FontWeight.w800, letterSpacing: 0),
          labelLarge: TextStyle(fontWeight: FontWeight.w800, letterSpacing: 0),
        ),
      ),
      home: initialScreen,
    );
  }
}
