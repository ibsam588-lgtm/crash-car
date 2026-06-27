import 'package:crash_car/src/game/crash_car_game.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:crash_car/src/screens/result_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:flame/game.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class PlayScreen extends ConsumerStatefulWidget {
  const PlayScreen({super.key});

  @override
  ConsumerState<PlayScreen> createState() => _PlayScreenState();
}

class _PlayScreenState extends ConsumerState<PlayScreen> {
  late CrashCarGame _game;
  bool _navigating = false;

  @override
  void initState() {
    super.initState();
    final progress = ref.read(progressProvider);
    _game = CrashCarGame(
      car: progress.selectedCar,
      upgradeLevel: progress.upgradeLevel(progress.selectedCarId),
      onFinished: _handleFinished,
    );
  }

  Future<void> _handleFinished(GameResult result) async {
    if (_navigating || !mounted) {
      return;
    }
    _navigating = true;
    await ref.read(progressProvider.notifier).recordResult(result);
    if (!mounted) {
      return;
    }
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (_) => ResultScreen(result: result)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          GameWidget(game: _game),
          _Hud(game: _game),
        ],
      ),
    );
  }
}

class _Hud extends StatelessWidget {
  const _Hud({required this.game});

  final CrashCarGame game;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Stack(
        children: [
          Positioned(
            top: 12,
            left: 12,
            right: 12,
            child: Column(
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: _MetricColumn(
                        label: 'Score',
                        value: game.score,
                        accent: Theme.of(context).colorScheme.primary,
                      ),
                    ),
                    Expanded(
                      child: _MetricColumn(
                        label: 'Speed',
                        value: game.speedKmh,
                        suffix: ' km/h',
                        accent: const Color(0xFF42C7FF),
                      ),
                    ),
                    Expanded(
                      child: _MetricColumn(
                        label: 'Hits',
                        value: game.objectsHit,
                        accent: const Color(0xFF7CD957),
                      ),
                    ),
                    IconButton.filledTonal(
                      tooltip: 'Pause',
                      onPressed: game.pauseOrResume,
                      icon: const Icon(Icons.pause_rounded),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                ValueListenableBuilder<int>(
                  valueListenable: game.damage,
                  builder: (context, damage, _) {
                    return ClipRRect(
                      borderRadius: BorderRadius.circular(999),
                      child: LinearProgressIndicator(
                        minHeight: 9,
                        value: damage / 100,
                        backgroundColor: Colors.white.withValues(alpha: 0.12),
                        color: Color.lerp(
                          const Color(0xFF7CD957),
                          const Color(0xFFE64655),
                          damage / 100,
                        ),
                      ),
                    );
                  },
                ),
                const SizedBox(height: 6),
                ValueListenableBuilder<double>(
                  valueListenable: game.levelProgress,
                  builder: (context, value, _) {
                    return ClipRRect(
                      borderRadius: BorderRadius.circular(999),
                      child: LinearProgressIndicator(
                        minHeight: 5,
                        value: value,
                        backgroundColor: Colors.white.withValues(alpha: 0.08),
                        color: const Color(0xFFFFC533),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),
          Positioned(
            right: 18,
            top: 128,
            child: ValueListenableBuilder<int>(
              valueListenable: game.combo,
              builder: (context, combo, _) => AnimatedScale(
                scale: combo > 1 ? 1.08 : 1,
                duration: const Duration(milliseconds: 150),
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.48),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(
                      color: const Color(0xFFFFC533).withValues(alpha: 0.55),
                    ),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 8,
                    ),
                    child: Text(
                      'x$combo',
                      style: const TextStyle(
                        fontWeight: FontWeight.w900,
                        fontSize: 22,
                        color: Color(0xFFFFC533),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            left: 18,
            right: 18,
            bottom: 18,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _HoldButton(
                  icon: Icons.chevron_left_rounded,
                  onDown: () => game.setSteering(-1),
                  onUp: () => game.setSteering(0),
                ),
                _HoldButton(
                  icon: Icons.bolt_rounded,
                  large: true,
                  onDown: () => game.setBoosting(true),
                  onUp: () => game.setBoosting(false),
                ),
                _HoldButton(
                  icon: Icons.chevron_right_rounded,
                  onDown: () => game.setSteering(1),
                  onUp: () => game.setSteering(0),
                ),
              ],
            ),
          ),
          Positioned(
            top: 12,
            right: 68,
            child: IconButton.filledTonal(
              tooltip: 'End run',
              onPressed: game.forceFinish,
              icon: const Icon(Icons.flag_rounded),
            ),
          ),
        ],
      ),
    );
  }
}

class _MetricColumn extends StatelessWidget {
  const _MetricColumn({
    required this.label,
    required this.value,
    required this.accent,
    this.suffix = '',
  });

  final String label;
  final ValueListenable<int> value;
  final Color accent;
  final String suffix;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<int>(
      valueListenable: value,
      builder: (context, current, _) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: const TextStyle(
                fontSize: 11,
                color: Colors.white70,
                fontWeight: FontWeight.w800,
              ),
            ),
            Text(
              '$current$suffix',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontWeight: FontWeight.w900,
                fontSize: 21,
                color: accent,
              ),
            ),
          ],
        );
      },
    );
  }
}

class _HoldButton extends StatelessWidget {
  const _HoldButton({
    required this.icon,
    required this.onDown,
    required this.onUp,
    this.large = false,
  });

  final IconData icon;
  final VoidCallback onDown;
  final VoidCallback onUp;
  final bool large;

  @override
  Widget build(BuildContext context) {
    final size = large ? 74.0 : 64.0;
    return GestureDetector(
      onTapDown: (_) => onDown(),
      onTapUp: (_) => onUp(),
      onTapCancel: onUp,
      child: DecoratedBox(
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: large
              ? const Color(0xFF143647).withValues(alpha: 0.84)
              : Colors.black.withValues(alpha: 0.48),
          border: Border.all(
            color: large ? const Color(0xFF42C7FF) : Colors.white30,
            width: 2,
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.35),
              blurRadius: 18,
              offset: const Offset(0, 8),
            ),
          ],
        ),
        child: SizedBox(
          width: size,
          height: size,
          child: Icon(
            icon,
            size: large ? 38 : 34,
            color: large ? const Color(0xFF42C7FF) : Colors.white,
          ),
        ),
      ),
    );
  }
}
