import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/screens/play_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:crash_car/src/widgets/chrome.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class ArenaSelectScreen extends ConsumerWidget {
  const ArenaSelectScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final progress = ref.watch(progressProvider);
    final wide = MediaQuery.sizeOf(context).width > 820;
    return Scaffold(
      body: CrashBackground(
        image: 'assets/images/key_art.png',
        dim: 0.62,
        child: Column(
          children: [
            AppTopBar(
              title: 'Select Arena',
              coins: progress.coins,
              onBack: () => Navigator.of(context).pop(),
            ),
            Expanded(
              child: GridView.count(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                crossAxisCount: wide ? 2 : 1,
                childAspectRatio: wide ? 2.05 : 1.55,
                crossAxisSpacing: 14,
                mainAxisSpacing: 14,
                children: [
                  for (final arena in arenaSpecs)
                    _ArenaCard(
                      arena: arena,
                      onPressed: () => Navigator.of(context).pushReplacement(
                        MaterialPageRoute(
                          builder: (_) => PlayScreen(arena: arena),
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ArenaCard extends StatelessWidget {
  const _ArenaCard({required this.arena, required this.onPressed});

  final ArenaSpec arena;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      borderColor: arena.primary.withValues(alpha: 0.52),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              DecoratedBox(
                decoration: BoxDecoration(
                  color: arena.primary.withValues(alpha: 0.16),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: arena.primary.withValues(alpha: 0.52),
                  ),
                ),
                child: SizedBox(
                  width: 48,
                  height: 48,
                  child: Icon(arena.icon, color: arena.primary),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      arena.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    Text(
                      arena.subtitle,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(color: Colors.white70),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Expanded(
            child: Text(
              arena.description,
              style: const TextStyle(color: Colors.white70, height: 1.25),
            ),
          ),
          const SizedBox(height: 12),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _ArenaPill(label: 'Traffic', value: arena.trafficDensity),
              _ArenaPill(label: 'Scenery', value: arena.sceneryDensity),
              _ArenaPill(label: 'Points', value: arena.scoreBonus),
            ],
          ),
          const SizedBox(height: 14),
          CrashButton(
            label: 'Start Arena',
            icon: Icons.sports_motorsports_rounded,
            onPressed: onPressed,
          ),
        ],
      ),
    );
  }
}

class _ArenaPill extends StatelessWidget {
  const _ArenaPill({required this.label, required this.value});

  final String label;
  final double value;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.white.withValues(alpha: 0.1)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
        child: Text(
          '$label ${(value * 100).round()}%',
          style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800),
        ),
      ),
    );
  }
}
