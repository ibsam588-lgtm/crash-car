import 'dart:math';

import 'package:crash_car/src/screens/arena_select_screen.dart';
import 'package:crash_car/src/screens/garage_screen.dart';
import 'package:crash_car/src/screens/impact_setup_screen.dart';
import 'package:crash_car/src/screens/ramp_setup_screen.dart';
import 'package:crash_car/src/screens/shop_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen>
    with SingleTickerProviderStateMixin {
  late final AnimationController _motion;

  @override
  void initState() {
    super.initState();
    _motion = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2600),
    )..repeat();
  }

  @override
  void dispose() {
    _motion.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final progress = ref.watch(progressProvider);
    return Scaffold(
      backgroundColor: const Color(0xFF061116),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final maxWidth = constraints.maxWidth;
          final maxHeight = constraints.maxHeight;
          final phoneWidth = min(maxWidth - 14, maxHeight * 0.49);
          final shellWidth = phoneWidth.clamp(300.0, 520.0).toDouble();
          final shellHeight = min(maxHeight - 8, shellWidth / 0.49);

          return Center(
            child: SizedBox(
              width: shellWidth,
              height: shellHeight,
              child: _PhoneShell(
                child: AnimatedBuilder(
                  animation: _motion,
                  builder: (context, _) => _ImpactHomeCanvas(
                    coins: progress.coins,
                    motion: _motion.value,
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

class _PhoneShell extends StatelessWidget {
  const _PhoneShell({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: const Color(0xFF010304),
        borderRadius: BorderRadius.circular(42),
        border: Border.all(color: const Color(0xFF263139), width: 3),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.72),
            blurRadius: 34,
            offset: const Offset(0, 16),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsets.all(9),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(33),
          child: child,
        ),
      ),
    );
  }
}

class _ImpactHomeCanvas extends StatelessWidget {
  const _ImpactHomeCanvas({required this.coins, required this.motion});

  final int coins;
  final double motion;

  @override
  Widget build(BuildContext context) {
    final drift = sin(motion * pi * 2) * 3.5;
    return Stack(
      fit: StackFit.expand,
      children: [
        Transform.translate(
          offset: Offset(drift * 0.18, -drift.abs() * 0.35),
          child: Transform.scale(
            scale: 1.022,
            child: Image.asset(
              'assets/images/home/impact_run_selected_bg.png',
              fit: BoxFit.cover,
              alignment: Alignment.center,
              filterQuality: FilterQuality.high,
            ),
          ),
        ),
        const _SceneShade(),
        Positioned(
          top: 18,
          left: 16,
          child: _PauseButton(
            onPressed: () => _showHomeMenu(context, coins),
          ),
        ),
        const Positioned(
          top: 30,
          right: 25,
          child: _ScoreCluster(),
        ),
        Positioned(
          left: 20,
          right: 20,
          bottom: 174,
          child: GestureDetector(
            onTap: () => _open(context, const ImpactSetupScreen()),
            child: const _ImpactRunLogo(),
          ),
        ),
        Positioned(
          left: 30,
          bottom: 48,
          child: _RoundActionButton(
            tooltip: 'Impact Run',
            icon: Icons.bolt_rounded,
            accent: const Color(0xFF56D6FF),
            onPressed: () => _open(context, const ImpactSetupScreen()),
          ),
        ),
        Positioned(
          right: 30,
          bottom: 48,
          child: _RoundActionButton(
            tooltip: 'Garage',
            icon: Icons.directions_car_filled_rounded,
            accent: const Color(0xFFFFEA42),
            onPressed: () => _open(context, const GarageScreen()),
          ),
        ),
        Positioned(
          left: 0,
          right: 0,
          bottom: 28,
          child: GestureDetector(
            onTap: () => _open(context, const ImpactSetupScreen()),
            child: const Center(child: _Speedometer()),
          ),
        ),
        const Positioned(
          left: 0,
          right: 0,
          bottom: 11,
          child: Center(child: _HomeIndicator()),
        ),
      ],
    );
  }
}

class _SceneShade extends StatelessWidget {
  const _SceneShade();

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            Colors.black.withValues(alpha: 0.04),
            Colors.transparent,
            Colors.black.withValues(alpha: 0.2),
            Colors.black.withValues(alpha: 0.72),
          ],
          stops: const [0, 0.46, 0.76, 1],
        ),
      ),
    );
  }
}

class _PauseButton extends StatelessWidget {
  const _PauseButton({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: 'Menu',
      child: IconButton.filled(
        style: IconButton.styleFrom(
          fixedSize: const Size(58, 58),
          backgroundColor: Colors.black.withValues(alpha: 0.3),
          foregroundColor: Colors.white,
          side: BorderSide(color: Colors.white.withValues(alpha: 0.16)),
        ),
        onPressed: onPressed,
        icon: const Icon(Icons.pause_rounded, size: 34),
      ),
    );
  }
}

class _ScoreCluster extends StatelessWidget {
  const _ScoreCluster();

  @override
  Widget build(BuildContext context) {
    return const Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Text(
          '12,750',
          style: TextStyle(
            color: Colors.white,
            fontSize: 31,
            fontWeight: FontWeight.w900,
            shadows: [Shadow(color: Colors.black54, blurRadius: 10)],
          ),
        ),
        SizedBox(height: 6),
        Text(
          'x2',
          style: TextStyle(
            color: Color(0xFFFFEF4D),
            fontSize: 31,
            fontWeight: FontWeight.w900,
            shadows: [Shadow(color: Colors.black54, blurRadius: 10)],
          ),
        ),
      ],
    );
  }
}

class _ImpactRunLogo extends StatelessWidget {
  const _ImpactRunLogo();

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 86,
      child: Stack(
        alignment: Alignment.center,
        children: [
          CustomPaint(
            size: const Size(258, 66),
            painter: _BrushSplashPainter(),
          ),
          Transform.rotate(
            angle: -0.035,
            child: const Text(
              'IMPACT RUN',
              textAlign: TextAlign.center,
              style: TextStyle(
                color: Colors.white,
                fontSize: 36,
                height: 1,
                fontStyle: FontStyle.italic,
                fontWeight: FontWeight.w900,
                shadows: [
                  Shadow(
                    color: Colors.black,
                    blurRadius: 9,
                    offset: Offset(0, 3),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _BrushSplashPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = const Color(0xFFFFC533).withValues(alpha: 0.92)
      ..strokeCap = StrokeCap.square
      ..strokeWidth = 10;
    canvas.drawLine(
      Offset(size.width * 0.09, size.height * 0.61),
      Offset(size.width * 0.84, size.height * 0.47),
      paint,
    );
    canvas.drawLine(
      Offset(size.width * 0.18, size.height * 0.77),
      Offset(size.width * 0.92, size.height * 0.59),
      paint..strokeWidth = 8,
    );

    final splatter = Paint()
      ..color = const Color(0xFFFFC533).withValues(alpha: 0.72);
    for (var i = 0; i < 30; i++) {
      final x = (sin(i * 12.91) * 0.5 + 0.5) * size.width;
      final y = size.height * (0.42 + (cos(i * 8.13) * 0.5 + 0.5) * 0.42);
      canvas.drawCircle(Offset(x, y), 1.1 + (i % 4) * 0.65, splatter);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class _RoundActionButton extends StatelessWidget {
  const _RoundActionButton({
    required this.tooltip,
    required this.icon,
    required this.accent,
    required this.onPressed,
  });

  final String tooltip;
  final IconData icon;
  final Color accent;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkResponse(
        onTap: onPressed,
        radius: 42,
        child: DecoratedBox(
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: Colors.black.withValues(alpha: 0.24),
            border: Border.all(color: Colors.white.withValues(alpha: 0.28)),
            boxShadow: [
              BoxShadow(
                color: accent.withValues(alpha: 0.16),
                blurRadius: 18,
              ),
            ],
          ),
          child: SizedBox(
            width: 78,
            height: 78,
            child: Icon(icon, color: accent, size: 39),
          ),
        ),
      ),
    );
  }
}

class _Speedometer extends StatelessWidget {
  const _Speedometer();

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 132,
      height: 132,
      child: CustomPaint(
        painter: _SpeedometerPainter(),
        child: const Center(
          child: Padding(
            padding: EdgeInsets.only(top: 18),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  '183',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 36,
                    height: 1,
                    fontWeight: FontWeight.w900,
                    shadows: [Shadow(color: Colors.black, blurRadius: 8)],
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'KM/H',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 15,
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

class _SpeedometerPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final radius = size.shortestSide * 0.45;
    final rect = Rect.fromCircle(center: center, radius: radius);
    canvas.drawCircle(
      center,
      radius + 7,
      Paint()..color = Colors.black.withValues(alpha: 0.4),
    );
    canvas.drawArc(
      rect,
      pi * 0.72,
      pi * 1.56,
      false,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 7
        ..strokeCap = StrokeCap.round
        ..color = Colors.white.withValues(alpha: 0.25),
    );
    canvas.drawArc(
      rect,
      pi * 0.72,
      pi * 0.78,
      false,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 7
        ..strokeCap = StrokeCap.round
        ..color = const Color(0xFF5DEBFF),
    );
    canvas.drawArc(
      rect,
      pi * 1.72,
      pi * 0.35,
      false,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 7
        ..strokeCap = StrokeCap.round
        ..color = const Color(0xFFFF7A30),
    );

    for (var i = 0; i < 18; i++) {
      final angle = pi * 0.78 + i * pi * 1.42 / 17;
      final tickLength = i % 3 == 0 ? 14.0 : 9.0;
      final inner = Offset(
        center.dx + cos(angle) * (radius - tickLength),
        center.dy + sin(angle) * (radius - tickLength),
      );
      final outer = Offset(
        center.dx + cos(angle) * (radius - 2),
        center.dy + sin(angle) * (radius - 2),
      );
      canvas.drawLine(
        inner,
        outer,
        Paint()
          ..color = Colors.white.withValues(alpha: 0.52)
          ..strokeWidth = i % 3 == 0 ? 2 : 1,
      );
    }

    final needleAngle = pi * 1.77;
    canvas.drawLine(
      center,
      Offset(
        center.dx + cos(needleAngle) * (radius - 16),
        center.dy + sin(needleAngle) * (radius - 16),
      ),
      Paint()
        ..color = Colors.white.withValues(alpha: 0.82)
        ..strokeWidth = 2.2
        ..strokeCap = StrokeCap.round,
    );
    canvas.drawCircle(center, 4, Paint()..color = Colors.white);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class _HomeIndicator extends StatelessWidget {
  const _HomeIndicator();

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.7),
        borderRadius: BorderRadius.circular(999),
      ),
      child: const SizedBox(width: 92, height: 5),
    );
  }
}

void _showHomeMenu(BuildContext context, int coins) {
  showModalBottomSheet<void>(
    context: context,
    backgroundColor: Colors.transparent,
    builder: (context) => _HomeMenuSheet(coins: coins),
  );
}

class _HomeMenuSheet extends StatelessWidget {
  const _HomeMenuSheet({required this.coins});

  final int coins;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: const Color(0xFF071014).withValues(alpha: 0.96),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: Colors.white.withValues(alpha: 0.16)),
          ),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(14, 12, 14, 14),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Row(
                  children: [
                    const Expanded(
                      child: Text(
                        'CRASH CAR',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                    const Icon(Icons.paid_rounded, color: Color(0xFFFFC533)),
                    const SizedBox(width: 4),
                    Text(
                      '$coins',
                      style: const TextStyle(fontWeight: FontWeight.w900),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                _MenuAction(
                  icon: Icons.bolt_rounded,
                  label: 'Impact Run',
                  onTap: () => _replaceSheetWith(
                    context,
                    const ImpactSetupScreen(),
                  ),
                ),
                _MenuAction(
                  icon: Icons.sports_motorsports_rounded,
                  label: 'Classic',
                  onTap: () => _replaceSheetWith(
                    context,
                    const ArenaSelectScreen(),
                  ),
                ),
                _MenuAction(
                  icon: Icons.speed_rounded,
                  label: 'Ramp',
                  onTap: () => _replaceSheetWith(
                    context,
                    const RampSetupScreen(),
                  ),
                ),
                _MenuAction(
                  icon: Icons.garage_rounded,
                  label: 'Garage',
                  onTap: () => _replaceSheetWith(context, const GarageScreen()),
                ),
                _MenuAction(
                  icon: Icons.storefront_rounded,
                  label: 'Store',
                  onTap: () => _replaceSheetWith(context, const ShopScreen()),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _MenuAction extends StatelessWidget {
  const _MenuAction({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: ListTile(
        dense: true,
        leading: Icon(icon, color: const Color(0xFFFFC533)),
        title: Text(
          label,
          style: const TextStyle(fontWeight: FontWeight.w900),
        ),
        trailing: const Icon(Icons.chevron_right_rounded),
        onTap: onTap,
      ),
    );
  }
}

void _replaceSheetWith(BuildContext context, Widget screen) {
  Navigator.of(context).pop();
  _open(context, screen);
}

void _open(BuildContext context, Widget screen) {
  Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));
}
