import 'dart:math';

import 'package:crash_car/src/game/impact_run_game.dart';
import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:crash_car/src/screens/result_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:flame/game.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class ImpactRunScreen extends ConsumerStatefulWidget {
  const ImpactRunScreen({super.key, required this.arena, required this.car});

  final ArenaSpec arena;
  final CarSpec car;

  @override
  ConsumerState<ImpactRunScreen> createState() => _ImpactRunScreenState();
}

class _ImpactRunScreenState extends ConsumerState<ImpactRunScreen> {
  late ImpactRunGame _game;
  bool _navigating = false;

  @override
  void initState() {
    super.initState();
    final progress = ref.read(progressProvider);
    _game = ImpactRunGame(
      car: widget.car,
      arena: widget.arena,
      upgradeLevel: progress.upgradeLevel(widget.car.id),
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
          nextRunLabel: 'Impact Again',
          nextRunBuilder: (_) =>
              ImpactRunScreen(arena: widget.arena, car: widget.car),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        fit: StackFit.expand,
        children: [
          GameWidget(game: _game),
          _ImpactHud(game: _game),
        ],
      ),
    );
  }
}

class _ImpactHud extends StatelessWidget {
  const _ImpactHud({required this.game});

  final ImpactRunGame game;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Stack(
        children: [
          Positioned(
            top: 12,
            left: 12,
            right: 12,
            child: DecoratedBox(
              decoration: BoxDecoration(
                color: const Color(0xFF071016).withValues(alpha: 0.54),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.white.withValues(alpha: 0.13)),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.28),
                    blurRadius: 18,
                    offset: const Offset(0, 8),
                  ),
                ],
              ),
              child: Padding(
                padding: const EdgeInsets.all(8),
                child: Column(
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: _MetricColumn(
                            label: 'Score',
                            value: game.score,
                            accent: const Color(0xFFFFD84A),
                          ),
                        ),
                        const SizedBox(width: 6),
                        Expanded(
                          child: _MetricColumn(
                            label: 'Damage',
                            value: game.damage,
                            accent: const Color(0xFFFF7A30),
                          ),
                        ),
                        const SizedBox(width: 6),
                        Expanded(
                          child: _MetricColumn(
                            label: 'Hits',
                            value: game.objectsHit,
                            accent: const Color(0xFF7CD957),
                          ),
                        ),
                        const SizedBox(width: 8),
                        _HudCircleButton(
                          tooltip: 'Pause',
                          icon: Icons.pause_rounded,
                          onPressed: game.pauseOrResume,
                        ),
                        const SizedBox(width: 6),
                        _HudCircleButton(
                          tooltip: 'End run',
                          icon: Icons.flag_rounded,
                          onPressed: game.forceFinish,
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    ValueListenableBuilder<double>(
                      valueListenable: game.levelProgress,
                      builder: (context, value, _) {
                        return ClipRRect(
                          borderRadius: BorderRadius.circular(999),
                          child: LinearProgressIndicator(
                            minHeight: 5,
                            value: value,
                            backgroundColor:
                                Colors.white.withValues(alpha: 0.08),
                            color: const Color(0xFFFFC533),
                          ),
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
          ),
          Positioned(
            top: kIsWeb ? 78 : 84,
            left: 0,
            right: 0,
            child: ValueListenableBuilder<int>(
              valueListenable: game.remainingSeconds,
              builder: (context, seconds, _) => IgnorePointer(
                child: Text(
                  '$seconds',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 42,
                    fontWeight: FontWeight.w900,
                    shadows: [
                      Shadow(
                        color: Colors.black,
                        blurRadius: 12,
                        offset: Offset(0, 3),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            top: 136,
            left: 18,
            child: ValueListenableBuilder<int>(
              valueListenable: game.combo,
              builder: (context, combo, _) => AnimatedScale(
                scale: combo > 1 ? 1.08 : 1,
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
            left: 18,
            right: 18,
            top: 172,
            child: ValueListenableBuilder<String>(
              valueListenable: game.stuntText,
              builder: (context, text, _) => IgnorePointer(
                child: AnimatedOpacity(
                  opacity: text.isEmpty ? 0 : 1,
                  duration: const Duration(milliseconds: 110),
                  child: Text(
                    text,
                    textAlign: TextAlign.center,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Color(0xFFFFC533),
                      fontSize: 24,
                      fontWeight: FontWeight.w900,
                      shadows: [
                        Shadow(color: Colors.black, blurRadius: 10),
                        Shadow(color: Color(0xFF42C7FF), blurRadius: 22),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            left: 18,
            right: 18,
            top: 214,
            child: ValueListenableBuilder<bool>(
              valueListenable: game.slowMotion,
              builder: (context, active, _) {
                return ValueListenableBuilder<String>(
                  valueListenable: game.impactText,
                  builder: (context, text, _) => IgnorePointer(
                    child: AnimatedOpacity(
                      opacity: active ? 1 : 0,
                      duration: const Duration(milliseconds: 120),
                      child: Center(
                        child: DecoratedBox(
                          decoration: BoxDecoration(
                            color: Colors.black.withValues(alpha: 0.56),
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: game.arena.primary.withValues(alpha: 0.7),
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
                  ),
                );
              },
            ),
          ),
          Positioned(
            left: 20,
            right: 20,
            top: 298,
            child: ValueListenableBuilder<String>(
              valueListenable: game.scorePopupText,
              builder: (context, text, _) => IgnorePointer(
                child: AnimatedOpacity(
                  opacity: text.isEmpty ? 0 : 1,
                  duration: const Duration(milliseconds: 110),
                  child: Text(
                    text,
                    textAlign: TextAlign.center,
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
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            left: 18,
            bottom: 106,
            child: _BoostMeter(value: game.boost),
          ),
          Positioned(
            right: 16,
            bottom: 84,
            child: _Speedometer(speed: game.speedKmh),
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
        ],
      ),
    );
  }
}

class _HudCircleButton extends StatelessWidget {
  const _HudCircleButton({
    required this.tooltip,
    required this.icon,
    required this.onPressed,
  });

  final String tooltip;
  final IconData icon;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkResponse(
        onTap: onPressed,
        radius: 25,
        child: DecoratedBox(
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: const Color(0xFF18252E).withValues(alpha: 0.88),
            border: Border.all(color: Colors.white.withValues(alpha: 0.18)),
          ),
          child: SizedBox(
            width: 42,
            height: 42,
            child: Icon(icon, color: Colors.white, size: 24),
          ),
        ),
      ),
    );
  }
}

class _MetricColumn extends StatelessWidget {
  const _MetricColumn({
    required this.label,
    required this.value,
    required this.accent,
  });

  final String label;
  final ValueListenable<int> value;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<int>(
      valueListenable: value,
      builder: (context, current, _) => DecoratedBox(
        decoration: BoxDecoration(
          color: Colors.black.withValues(alpha: 0.34),
          borderRadius: BorderRadius.circular(7),
          border: Border.all(color: Colors.white.withValues(alpha: 0.09)),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontSize: 10,
                  color: Colors.white70,
                  fontWeight: FontWeight.w800,
                ),
              ),
              Text(
                '$current',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: accent,
                  fontSize: 20,
                  fontWeight: FontWeight.w900,
                  height: 1.05,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
class _BoostMeter extends StatelessWidget {
  const _BoostMeter({required this.value});

  final ValueListenable<double> value;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 150,
      child: ValueListenableBuilder<double>(
        valueListenable: value,
        builder: (context, current, _) => Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'BOOST',
              style: TextStyle(
                color: Colors.white70,
                fontSize: 11,
                fontWeight: FontWeight.w900,
              ),
            ),
            const SizedBox(height: 5),
            ClipRRect(
              borderRadius: BorderRadius.circular(999),
              child: LinearProgressIndicator(
                minHeight: 12,
                value: current,
                color: const Color(0xFF42C7FF),
                backgroundColor: Colors.black.withValues(alpha: 0.44),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Speedometer extends StatelessWidget {
  const _Speedometer({required this.speed});

  final ValueListenable<int> speed;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<int>(
      valueListenable: speed,
      builder: (context, current, _) => SizedBox(
        width: 112,
        height: 112,
        child: CustomPaint(
          painter: _SpeedometerPainter(current),
          child: Center(
            child: Padding(
              padding: const EdgeInsets.only(top: 18),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '$current',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 26,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  const Text(
                    'KM/H',
                    style: TextStyle(
                      color: Colors.white70,
                      fontSize: 10,
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
  }
}

class _SpeedometerPainter extends CustomPainter {
  const _SpeedometerPainter(this.speed);

  final int speed;

  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final radius = size.shortestSide / 2 - 6;
    final rect = Rect.fromCircle(center: center, radius: radius);
    final base = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 7
      ..strokeCap = StrokeCap.round
      ..color = Colors.black.withValues(alpha: 0.58);
    final track = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 7
      ..strokeCap = StrokeCap.round
      ..color = Colors.white.withValues(alpha: 0.2);
    final accent = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 7
      ..strokeCap = StrokeCap.round
      ..color = const Color(0xFFFFC533);
    canvas.drawCircle(center, radius + 4, base);
    canvas.drawArc(rect, 2.4, 4.9, false, track);
    canvas.drawArc(
      rect,
      2.4,
      4.9 * (speed / 330).clamp(0.0, 1.0),
      false,
      accent,
    );
    final needleAngle = 2.4 + 4.9 * (speed / 330).clamp(0.0, 1.0);
    final needleEnd = Offset(
      center.dx + cos(needleAngle) * (radius - 13),
      center.dy + sin(needleAngle) * (radius - 13),
    );
    canvas.drawLine(
      center,
      needleEnd,
      Paint()
        ..color = const Color(0xFFE64655)
        ..strokeWidth = 3
        ..strokeCap = StrokeCap.round,
    );
    canvas.drawCircle(center, 5, Paint()..color = Colors.white);
  }

  @override
  bool shouldRepaint(covariant _SpeedometerPainter oldDelegate) =>
      oldDelegate.speed != speed;
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
    final size = large ? 76.0 : 64.0;
    return GestureDetector(
      onTapDown: (_) => onDown(),
      onTapUp: (_) => onUp(),
      onTapCancel: onUp,
      child: DecoratedBox(
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: large
              ? const Color(0xFF143647).withValues(alpha: 0.86)
              : Colors.black.withValues(alpha: 0.5),
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
