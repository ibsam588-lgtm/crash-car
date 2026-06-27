import 'package:crash_car/src/game/ramp_launch_game.dart';
import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:crash_car/src/screens/result_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:flame/game.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class RampPlayScreen extends ConsumerStatefulWidget {
  const RampPlayScreen({
    super.key,
    required this.arena,
    required this.car,
    required this.chargeSeconds,
  });

  final ArenaSpec arena;
  final CarSpec car;
  final int chargeSeconds;

  @override
  ConsumerState<RampPlayScreen> createState() => _RampPlayScreenState();
}

class _RampPlayScreenState extends ConsumerState<RampPlayScreen> {
  late RampLaunchGame _game;
  bool _navigating = false;

  @override
  void initState() {
    super.initState();
    final progress = ref.read(progressProvider);
    _game = RampLaunchGame(
      car: widget.car,
      arena: widget.arena,
      upgradeLevel: progress.upgradeLevel(widget.car.id),
      chargeSeconds: widget.chargeSeconds,
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
      MaterialPageRoute(
        builder: (_) => ResultScreen(
          result: result,
          arena: widget.arena,
          nextRunLabel: 'Ramp Again',
          nextRunBuilder: (_) => RampPlayScreen(
            arena: widget.arena,
            car: widget.car,
            chargeSeconds: widget.chargeSeconds,
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          GameWidget(game: _game),
          _RampHud(game: _game),
        ],
      ),
    );
  }
}

class _RampHud extends StatelessWidget {
  const _RampHud({required this.game});

  final RampLaunchGame game;

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
                ValueListenableBuilder<String>(
                  valueListenable: game.phaseText,
                  builder: (context, text, _) => Text(
                    '${game.arena.name} / $text',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: game.arena.primary,
                      fontSize: 12,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                const SizedBox(height: 4),
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
                ValueListenableBuilder<double>(
                  valueListenable: game.chargeProgress,
                  builder: (context, value, _) {
                    return ClipRRect(
                      borderRadius: BorderRadius.circular(999),
                      child: LinearProgressIndicator(
                        minHeight: 9,
                        value: value,
                        backgroundColor: Colors.white.withValues(alpha: 0.12),
                        color: const Color(0xFF42C7FF),
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
                const SizedBox(height: 6),
                ValueListenableBuilder<int>(
                  valueListenable: game.damage,
                  builder: (context, value, _) => Align(
                    alignment: Alignment.centerLeft,
                    child: SizedBox(
                      width: 150,
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(999),
                        child: LinearProgressIndicator(
                          minHeight: 5,
                          value: value / 100,
                          backgroundColor: Colors.white.withValues(alpha: 0.08),
                          color: Color.lerp(
                            const Color(0xFF7CD957),
                            const Color(0xFFE64655),
                            value / 100,
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          Positioned(
            right: 18,
            top: 130,
            child: ValueListenableBuilder<int>(
              valueListenable: game.combo,
              builder: (context, combo, _) => AnimatedScale(
                scale: combo > 1 ? 1.1 : 1,
                duration: const Duration(milliseconds: 140),
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.5),
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
                        color: Color(0xFFFFC533),
                        fontSize: 22,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            left: 20,
            right: 20,
            top: 158,
            child: ValueListenableBuilder<bool>(
              valueListenable: game.slowMotion,
              builder: (context, active, _) {
                return ValueListenableBuilder<String>(
                  valueListenable: game.impactText,
                  builder: (context, text, _) {
                    return IgnorePointer(
                      child: AnimatedOpacity(
                        opacity: active ? 1 : 0,
                        duration: const Duration(milliseconds: 120),
                        child: Center(
                          child: DecoratedBox(
                            decoration: BoxDecoration(
                              color: Colors.black.withValues(alpha: 0.58),
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(
                                color: game.arena.primary.withValues(
                                  alpha: 0.72,
                                ),
                              ),
                            ),
                            child: Padding(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 18,
                                vertical: 12,
                              ),
                              child: Column(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  const Text(
                                    'SLOW MOTION',
                                    style: TextStyle(
                                      color: Colors.white,
                                      fontSize: 12,
                                      fontWeight: FontWeight.w900,
                                    ),
                                  ),
                                  if (text.isNotEmpty)
                                    Text(
                                      text,
                                      textAlign: TextAlign.center,
                                      style: TextStyle(
                                        color: game.arena.primary,
                                        fontSize: 24,
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ),
                    );
                  },
                );
              },
            ),
          ),
          Positioned(
            left: 20,
            right: 20,
            top: 254,
            child: ValueListenableBuilder<String>(
              valueListenable: game.scorePopupText,
              builder: (context, text, _) {
                return IgnorePointer(
                  child: AnimatedOpacity(
                    opacity: text.isEmpty ? 0 : 1,
                    duration: const Duration(milliseconds: 110),
                    child: Center(
                      child: Text(
                        text,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Color(0xFFFFC533),
                          fontSize: 34,
                          fontWeight: FontWeight.w900,
                          shadows: [
                            Shadow(
                              color: Colors.black,
                              blurRadius: 12,
                              offset: Offset(0, 3),
                            ),
                            Shadow(color: Color(0xFF42C7FF), blurRadius: 22),
                          ],
                        ),
                      ),
                    ),
                  ),
                );
              },
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
                  icon: Icons.local_gas_station_rounded,
                  large: true,
                  onDown: () => game.setAccelerating(true),
                  onUp: () => game.setAccelerating(false),
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
                color: accent,
                fontSize: 21,
                fontWeight: FontWeight.w900,
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
