import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:crash_car/src/screens/garage_screen.dart';
import 'package:crash_car/src/screens/home_screen.dart';
import 'package:crash_car/src/screens/play_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:crash_car/src/widgets/chrome.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class ResultScreen extends ConsumerWidget {
  const ResultScreen({super.key, required this.result, required this.arena});

  final GameResult result;
  final ArenaSpec arena;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final progress = ref.watch(progressProvider);
    final stars = result.score >= 9500
        ? 3
        : result.score >= 5200
        ? 2
        : result.score >= 1800
        ? 1
        : 0;
    return Scaffold(
      body: CrashBackground(
        image: 'assets/images/key_art.png',
        dim: 0.72,
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: GlassPanel(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        for (var i = 0; i < 3; i++)
                          Icon(
                            i < stars
                                ? Icons.star_rounded
                                : Icons.star_border_rounded,
                            color: const Color(0xFFFFC533),
                            size: 46,
                          ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Text(
                      result.levelComplete ? 'LEVEL COMPLETE' : 'RUN COMPLETE',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.headlineMedium
                          ?.copyWith(color: const Color(0xFFFFC533)),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      result.arenaName,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: arena.primary,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 18),
                    _ScoreLine(
                      label: 'Total score',
                      value: result.score.toString(),
                    ),
                    _ScoreLine(
                      label: 'Coins earned',
                      value: '+${result.coins}',
                    ),
                    _ScoreLine(
                      label: 'Objects hit',
                      value: result.objectsHit.toString(),
                    ),
                    _ScoreLine(
                      label: 'Damage',
                      value: '${result.damagePercent}%',
                    ),
                    _ScoreLine(
                      label: 'Max speed',
                      value: '${result.maxSpeed} km/h',
                    ),
                    _ScoreLine(
                      label: 'Best combo',
                      value: 'x${result.bestCombo}',
                    ),
                    const Divider(height: 28),
                    _ScoreLine(label: 'Bank', value: progress.coins.toString()),
                    const SizedBox(height: 18),
                    CrashButton(
                      label: 'Next Run',
                      icon: Icons.play_arrow_rounded,
                      onPressed: () => Navigator.of(context).pushReplacement(
                        MaterialPageRoute(
                          builder: (_) => PlayScreen(arena: arena),
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                    CrashButton(
                      label: 'Garage',
                      icon: Icons.garage_rounded,
                      primary: false,
                      onPressed: () => Navigator.of(context).pushReplacement(
                        MaterialPageRoute(builder: (_) => const GarageScreen()),
                      ),
                    ),
                    const SizedBox(height: 10),
                    CrashButton(
                      label: 'Home',
                      icon: Icons.home_rounded,
                      primary: false,
                      onPressed: () => Navigator.of(context).pushAndRemoveUntil(
                        MaterialPageRoute(builder: (_) => const HomeScreen()),
                        (route) => false,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _ScoreLine extends StatelessWidget {
  const _ScoreLine({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        children: [
          Expanded(
            child: Text(label, style: const TextStyle(color: Colors.white70)),
          ),
          Text(
            value,
            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 18),
          ),
        ],
      ),
    );
  }
}
