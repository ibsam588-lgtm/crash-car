import 'dart:math';
import 'dart:ui' as ui;

import 'package:crash_car/src/game/procedural_vehicle.dart';
import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/screens/impact_run_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:crash_car/src/widgets/chrome.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class ImpactSetupScreen extends ConsumerStatefulWidget {
  const ImpactSetupScreen({super.key});

  @override
  ConsumerState<ImpactSetupScreen> createState() => _ImpactSetupScreenState();
}

class _ImpactSetupScreenState extends ConsumerState<ImpactSetupScreen> {
  ArenaSpec _arena = arenaSpecs[1];
  String? _carId;

  @override
  Widget build(BuildContext context) {
    final progress = ref.watch(progressProvider);
    final selectedCar = carById(_carId ?? progress.selectedCarId);
    final unlockedCars = carSpecs
        .where((car) => progress.unlockedCarIds.contains(car.id))
        .toList();
    final wide = MediaQuery.sizeOf(context).width > 860;

    return Scaffold(
      body: CrashBackground(
        child: Column(
          children: [
            AppTopBar(
              title: 'Impact Run',
              coins: progress.coins,
              onBack: () => Navigator.of(context).pop(),
            ),
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                child: wide
                    ? Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            flex: 5,
                            child: _ImpactPreview(
                              arena: _arena,
                              car: selectedCar,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            flex: 4,
                            child: _ImpactControls(
                              arena: _arena,
                              selectedCar: selectedCar,
                              unlockedCars: unlockedCars,
                              onArenaChanged: (arena) =>
                                  setState(() => _arena = arena),
                              onCarChanged: (car) =>
                                  setState(() => _carId = car.id),
                              onStart: () => _start(selectedCar),
                            ),
                          ),
                        ],
                      )
                    : Column(
                        children: [
                          _ImpactPreview(arena: _arena, car: selectedCar),
                          const SizedBox(height: 14),
                          _ImpactControls(
                            arena: _arena,
                            selectedCar: selectedCar,
                            unlockedCars: unlockedCars,
                            onArenaChanged: (arena) =>
                                setState(() => _arena = arena),
                            onCarChanged: (car) =>
                                setState(() => _carId = car.id),
                            onStart: () => _start(selectedCar),
                          ),
                        ],
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _start(CarSpec selectedCar) {
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) => ImpactRunScreen(arena: _arena, car: selectedCar),
      ),
    );
  }
}

class _ImpactPreview extends StatefulWidget {
  const _ImpactPreview({required this.arena, required this.car});

  final ArenaSpec arena;
  final CarSpec car;

  @override
  State<_ImpactPreview> createState() => _ImpactPreviewState();
}

class _ImpactPreviewState extends State<_ImpactPreview>
    with SingleTickerProviderStateMixin {
  late final AnimationController _motion;

  @override
  void initState() {
    super.initState();
    _motion = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2200),
    )..repeat();
  }

  @override
  void dispose() {
    _motion.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final arena = widget.arena;
    final car = widget.car;
    return GlassPanel(
      borderColor: arena.primary.withValues(alpha: 0.45),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.speed_rounded, color: arena.primary, size: 34),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${arena.name} Chase',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context)
                          .textTheme
                          .headlineSmall
                          ?.copyWith(fontWeight: FontWeight.w900),
                    ),
                    Text(
                      '${car.name} / near misses, drift, impact chains',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: arena.primary),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 18),
          AspectRatio(
            aspectRatio: 1.62,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: AnimatedBuilder(
                animation: _motion,
                builder: (context, _) {
                  final t = _motion.value;
                  return Stack(
                    fit: StackFit.expand,
                    children: [
                      Positioned.fill(
                        child: CustomPaint(
                          painter: _PreviewRoadPainter(arena, t),
                        ),
                      ),
                      Positioned.fill(
                        child: _SetupChaseActors(car: car, motion: t),
                      ),
                      const Positioned(
                        top: 12,
                        left: 12,
                        child: _PreviewHud(label: 'SCORE', value: '26,641'),
                      ),
                      const Positioned(
                        top: 12,
                        right: 12,
                        child: _PreviewHud(label: 'SPEED', value: '132'),
                      ),
                    ],
                  );
                },
              ),
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: StatBar(label: 'Speed', value: car.speed),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: StatBar(label: 'Handling', value: car.handling),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: StatBar(label: 'Impact', value: car.durability),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ImpactControls extends StatelessWidget {
  const _ImpactControls({
    required this.arena,
    required this.selectedCar,
    required this.unlockedCars,
    required this.onArenaChanged,
    required this.onCarChanged,
    required this.onStart,
  });

  final ArenaSpec arena;
  final CarSpec selectedCar;
  final List<CarSpec> unlockedCars;
  final ValueChanged<ArenaSpec> onArenaChanged;
  final ValueChanged<CarSpec> onCarChanged;
  final VoidCallback onStart;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text('Arena', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final item in arenaSpecs)
                ChoiceChip(
                  selected: item.id == arena.id,
                  avatar: Icon(item.icon, size: 18),
                  label: Text(item.name),
                  selectedColor: item.primary.withValues(alpha: 0.28),
                  onSelected: (_) => onArenaChanged(item),
                ),
            ],
          ),
          const SizedBox(height: 18),
          Text('Car', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 10),
          SizedBox(
            height: 148,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: unlockedCars.length,
              separatorBuilder: (context, index) => const SizedBox(width: 10),
              itemBuilder: (context, index) {
                final car = unlockedCars[index];
                final selected = car.id == selectedCar.id;
                return _ImpactCarPickCard(
                  car: car,
                  selected: selected,
                  onPressed: () => onCarChanged(car),
                );
              },
            ),
          ),
          const SizedBox(height: 18),
          const _RuleRow(
            icon: Icons.local_gas_station_rounded,
            label: 'Hold gas to build speed and boost.',
          ),
          const _RuleRow(
            icon: Icons.compare_arrows_rounded,
            label: 'Steer through traffic for near-miss points.',
          ),
          const _RuleRow(
            icon: Icons.car_crash_rounded,
            label: 'Crashes trigger slow motion, debris, and recovery.',
          ),
          const SizedBox(height: 20),
          CrashButton(
            label: 'Start Impact Run',
            icon: Icons.speed_rounded,
            onPressed: onStart,
          ),
        ],
      ),
    );
  }
}

class _SetupChaseActors extends StatelessWidget {
  const _SetupChaseActors({required this.car, required this.motion});

  final CarSpec car;
  final double motion;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final height = constraints.maxHeight;
        final wheelSpin = motion * pi * 18;
        final bounce = sin(motion * pi * 2) * height * 0.008;
        return Stack(
          fit: StackFit.expand,
          children: [
            _PreviewVehicle(
              left: width * (0.63 + sin(motion * pi * 2) * 0.015),
              top: height * (0.42 + sin(motion * pi * 2) * 0.035),
              width: width * 0.19,
              height: height * 0.18,
              angle: -0.02,
              style: ProceduralVehicleStyle.truck,
              color: const Color(0xFFEAECE8),
              wheelSpin: wheelSpin * 0.6,
            ),
            _PreviewVehicle(
              left: width * (0.27 + cos(motion * pi * 2) * 0.02),
              top: height * (0.55 + cos(motion * pi * 2) * 0.04),
              width: width * 0.15,
              height: height * 0.15,
              angle: 0.04,
              style: ProceduralVehicleStyle.sport,
              color: const Color(0xFF28A8E8),
              wheelSpin: wheelSpin,
            ),
            _PreviewVehicle(
              left: width * 0.5,
              top: height * (0.31 + sin(motion * pi * 2 + 1.2) * 0.025),
              width: width * 0.1,
              height: height * 0.1,
              angle: -0.06,
              style: ProceduralVehicleStyle.sedan,
              color: const Color(0xFFE64655),
              wheelSpin: wheelSpin * 0.8,
              opacity: 0.72,
            ),
            Positioned(
              left: width * 0.28,
              bottom: height * 0.11,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(999),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.48),
                      blurRadius: 20,
                      spreadRadius: 2,
                    ),
                  ],
                ),
                child: SizedBox(
                  width: width * 0.44,
                  height: max(12, height * 0.035),
                ),
              ),
            ),
            Positioned(
              left: width * 0.5 - width * 0.25,
              bottom: height * 0.01 + bounce,
              child: SizedBox(
                width: width * 0.5,
                height: height * 0.42,
                child: CustomPaint(
                  painter: ProceduralVehiclePainter(
                    style: proceduralStyleForCarId(car.id),
                    color: car.color,
                    wheelSpin: wheelSpin,
                    damage: 0.04,
                    angle: -0.03 + sin(motion * pi * 2) * 0.012,
                    brakeLights: true,
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _PreviewVehicle extends StatelessWidget {
  const _PreviewVehicle({
    required this.left,
    required this.top,
    required this.width,
    required this.height,
    required this.angle,
    required this.style,
    required this.color,
    required this.wheelSpin,
    this.opacity = 1,
  });

  final double left;
  final double top;
  final double width;
  final double height;
  final double angle;
  final ProceduralVehicleStyle style;
  final Color color;
  final double wheelSpin;
  final double opacity;

  @override
  Widget build(BuildContext context) {
    return Positioned(
      left: left,
      top: top,
      child: SizedBox(
        width: width,
        height: height,
        child: CustomPaint(
          painter: ProceduralVehiclePainter(
            style: style,
            color: color,
            wheelSpin: wheelSpin,
            angle: angle,
            opacity: opacity,
          ),
        ),
      ),
    );
  }
}

class _ImpactCarPickCard extends StatelessWidget {
  const _ImpactCarPickCard({
    required this.car,
    required this.selected,
    required this.onPressed,
  });

  final CarSpec car;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(8),
      onTap: onPressed,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: selected
              ? car.color.withValues(alpha: 0.18)
              : Colors.white.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: selected ? car.color : Colors.white.withValues(alpha: 0.14),
            width: selected ? 2 : 1,
          ),
        ),
        child: SizedBox(
          width: 148,
          child: Padding(
            padding: const EdgeInsets.all(10),
            child: Column(
              children: [
                Expanded(
                  child: SizedBox.expand(
                    child: CustomPaint(
                      painter: ProceduralVehiclePainter(
                        style: proceduralStyleForCarId(car.id),
                        color: car.color,
                        wheelSpin: selected ? 1.6 : 0.4,
                        damage: selected ? 0.05 : 0,
                        brakeLights: selected,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  car.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _RuleRow extends StatelessWidget {
  const _RuleRow({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 9),
      child: Row(
        children: [
          Icon(icon, color: const Color(0xFFFFC533), size: 20),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              label,
              style: const TextStyle(color: Colors.white70, height: 1.2),
            ),
          ),
        ],
      ),
    );
  }
}

class _PreviewHud extends StatelessWidget {
  const _PreviewHud({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 7),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: const TextStyle(
                color: Colors.white70,
                fontSize: 9,
                fontWeight: FontWeight.w800,
              ),
            ),
            Text(
              value,
              style: const TextStyle(
                color: Colors.white,
                fontSize: 17,
                fontWeight: FontWeight.w900,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PreviewRoadPainter extends CustomPainter {
  const _PreviewRoadPainter(this.arena, this.motion);

  final ArenaSpec arena;
  final double motion;

  @override
  void paint(Canvas canvas, Size size) {
    final horizon = size.height * 0.54;
    final center = size.width * 0.5;
    canvas.drawRect(
      Offset.zero & size,
      Paint()
        ..shader = ui.Gradient.linear(
          Offset(center, 0),
          Offset(center, size.height),
          [
            const Color(0xFF3BB9F0),
            const Color(0xFFCAF6FF),
            const Color(0xFFE7E7DF),
            arena.sideTint,
          ],
          const [0, 0.4, 0.58, 1],
        ),
    );
    final sunCenter = Offset(size.width * 0.08, size.height * 0.12);
    canvas.drawCircle(
      sunCenter,
      size.shortestSide * 0.28,
      Paint()
        ..shader = ui.Gradient.radial(
          sunCenter,
          size.shortestSide * 0.28,
          [
            Colors.white.withValues(alpha: 0.85),
            const Color(0xFFFFE8A5).withValues(alpha: 0.22),
            Colors.white.withValues(alpha: 0),
          ],
        ),
    );

    for (var layer = 0; layer < 2; layer++) {
      final baseY = horizon + 18 + layer * 26;
      final height = size.height * (0.18 - layer * 0.05);
      final offset = (motion * (36 + layer * 18)) % 76;
      final hills = Path()..moveTo(-90, baseY);
      for (double x = -90; x <= size.width + 100; x += 44) {
        hills.lineTo(
          x,
          baseY -
              height *
                  (0.42 +
                      sin((x + offset) * 0.03 + layer) * 0.18 +
                      sin((x + offset) * 0.07) * 0.08),
        );
      }
      hills
        ..lineTo(size.width + 100, baseY + height)
        ..lineTo(-90, baseY + height)
        ..close();
      canvas.drawPath(
        hills,
        Paint()
          ..color =
              (layer == 0 ? const Color(0xFF9DA87B) : const Color(0xFF68705A))
                  .withValues(alpha: layer == 0 ? 0.46 : 0.42),
      );
    }

    final skylineShift = motion * 78;
    for (var i = 0; i < 13; i++) {
      final x = ((i * 68 - skylineShift) % (size.width + 120)) - 56;
      final width = 28.0 + (i * 11 % 34).toDouble();
      final height = 45.0 + (i * 17 % 76).toDouble();
      final rect = Rect.fromLTWH(x, horizon + 50 - height, width, height);
      canvas.drawRRect(
        RRect.fromRectAndRadius(rect, const Radius.circular(3)),
        Paint()
          ..shader = ui.Gradient.linear(
            rect.topLeft,
            rect.bottomRight,
            [
              const Color(0xFFDDE8E8).withValues(alpha: 0.32),
              const Color(0xFF263942).withValues(alpha: 0.58),
            ],
          ),
      );
      final windowPaint = Paint()..color = Colors.white.withValues(alpha: 0.1);
      for (double wx = rect.left + 6; wx < rect.right - 4; wx += 11) {
        for (double wy = rect.top + 8; wy < rect.bottom - 7; wy += 15) {
          canvas.drawRect(Rect.fromLTWH(wx, wy, 3, 5), windowPaint);
        }
      }
    }

    for (final side in [-1.0, 1.0]) {
      for (var i = 0; i < 5; i++) {
        final depth = 0.18 + ((i / 5 + motion * 0.35) % 1) * 0.62;
        final y = horizon + pow(depth, 1.5) * (size.height - horizon);
        final x = center + side * (52 + depth * size.width * 0.42);
        final w = 38 + depth * 98;
        final h = 38 + depth * 108;
        final rect = Rect.fromLTWH(x - w / 2, y - h, w, h);
        canvas.drawRRect(
          RRect.fromRectAndRadius(rect, Radius.circular(3 + depth * 3)),
          Paint()
            ..color =
                Color.lerp(arena.sideTint, const Color(0xFF6A777D), depth)!
                    .withValues(alpha: 0.34 + depth * 0.36),
        );
        canvas.drawRect(
          Rect.fromLTWH(rect.left, rect.top + h * 0.2, w * 0.8, h * 0.1),
          Paint()..color = arena.primary.withValues(alpha: 0.24 + depth * 0.25),
        );
      }
    }

    final bridgeY = horizon + size.height * 0.09 + sin(motion * pi * 2) * 4;
    final bridge = Path()
      ..moveTo(-12, bridgeY - 10)
      ..lineTo(size.width + 12, bridgeY - 15)
      ..lineTo(size.width + 18, bridgeY + 15)
      ..lineTo(-18, bridgeY + 19)
      ..close();
    canvas.drawPath(
      bridge,
      Paint()..color = const Color(0xFF647177).withValues(alpha: 0.42),
    );

    final road = Path()
      ..moveTo(center - 38, horizon)
      ..lineTo(center + 38, horizon)
      ..lineTo(size.width * 0.88, size.height)
      ..lineTo(size.width * 0.12, size.height)
      ..close();
    canvas.drawPath(
      road,
      Paint()
        ..shader = ui.Gradient.linear(
          Offset(center, horizon),
          Offset(center, size.height),
          [
            Color.lerp(arena.roadTint, Colors.white, 0.18)!
                .withValues(alpha: 0.82),
            Color.lerp(arena.roadTint, Colors.black, 0.2)!
                .withValues(alpha: 0.95),
            const Color(0xFF101519),
          ],
          const [0, 0.54, 1],
        ),
    );
    canvas.save();
    canvas.clipPath(road);
    for (var i = 0; i < 12; i++) {
      final d = ((i / 12) + motion * 1.45) % 1;
      final y0 = horizon + pow(d, 1.65) * (size.height - horizon);
      final y1 = min(size.height, y0 + 8 + d * 26);
      final half0 = 38 + d * size.width * 0.34;
      final half1 = 38 + min(1.0, d + 0.08) * size.width * 0.34;
      final band = Path()
        ..moveTo(center - half0, y0)
        ..lineTo(center + half0, y0)
        ..lineTo(center + half1, y1)
        ..lineTo(center - half1, y1)
        ..close();
      canvas.drawPath(
        band,
        Paint()
          ..color = (i.isEven ? Colors.black : Colors.white).withValues(
            alpha: i.isEven ? 0.055 : 0.035,
          ),
      );
    }

    final streakPaint = Paint()
      ..strokeCap = StrokeCap.round
      ..color = Colors.white.withValues(alpha: 0.18);
    for (var i = 0; i < 26; i++) {
      final d = ((i * 0.071) + motion * 1.9) % 1;
      if (d < 0.08) {
        continue;
      }
      final lane = -0.92 + (i * 0.37 % 1.84);
      final y = horizon + pow(d, 1.72) * (size.height - horizon);
      final x = center + lane * (42 + d * size.width * 0.32);
      streakPaint.strokeWidth = 0.8 + d * 2.6;
      canvas.drawLine(
        Offset(x, y),
        Offset(x + lane * 12, min(size.height, y + 18 + d * 32)),
        streakPaint..color = Colors.white.withValues(alpha: 0.05 + d * 0.16),
      );
    }
    canvas.restore();

    final linePaint = Paint()
      ..color = Colors.white.withValues(alpha: 0.22)
      ..strokeWidth = 1.8;
    for (final lane in [-0.45, 0.0, 0.45]) {
      canvas.drawLine(
        Offset(center + lane * 42, horizon),
        Offset(center + lane * size.width * 0.36, size.height),
        linePaint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant _PreviewRoadPainter oldDelegate) =>
      oldDelegate.arena != arena || oldDelegate.motion != motion;
}
