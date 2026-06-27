import 'package:crash_car/src/screens/arena_select_screen.dart';
import 'package:crash_car/src/screens/garage_screen.dart';
import 'package:crash_car/src/screens/shop_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:crash_car/src/widgets/chrome.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final progress = ref.watch(progressProvider);
    return Scaffold(
      body: CrashBackground(
        image: 'assets/images/key_art.png',
        dim: 0.48,
        child: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: LayoutBuilder(
              builder: (context, constraints) {
                final compact = constraints.maxWidth < 720;
                final title = _HeroTitle(
                  highScore: progress.highScore,
                  coins: progress.coins,
                );
                const actions = _HomeActions();
                if (compact) {
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(child: title),
                      actions,
                    ],
                  );
                }
                return Row(
                  children: [
                    Expanded(flex: 5, child: title),
                    const SizedBox(width: 24),
                    SizedBox(width: 360, child: actions),
                  ],
                );
              },
            ),
          ),
        ),
      ),
    );
  }
}

class _HeroTitle extends StatelessWidget {
  const _HeroTitle({required this.highScore, required this.coins});

  final int highScore;
  final int coins;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisAlignment: MainAxisAlignment.end,
      children: [
        ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 520),
          child: FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: RichText(
              text: TextSpan(
                style: textTheme.headlineLarge?.copyWith(
                  fontSize: 74,
                  height: 0.88,
                  fontWeight: FontWeight.w900,
                ),
                children: const [
                  TextSpan(
                    text: 'CRASH\n',
                    style: TextStyle(color: Colors.white),
                  ),
                  TextSpan(
                    text: 'CAR',
                    style: TextStyle(color: Color(0xFFFFC533)),
                  ),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: 14),
        const Text(
          'Smash, crash, and score through a construction yard packed with crates, barrels, cones, and barricades.',
          style: TextStyle(fontSize: 17, color: Colors.white, height: 1.35),
        ),
        const SizedBox(height: 18),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: [
            _Metric(label: 'Best', value: highScore.toString()),
            _Metric(label: 'Coins', value: coins.toString()),
            const _Metric(label: 'Mode', value: 'Arcade'),
          ],
        ),
      ],
    );
  }
}

class _HomeActions extends StatelessWidget {
  const _HomeActions();

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          CrashButton(
            label: 'Play',
            icon: Icons.play_arrow_rounded,
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const ArenaSelectScreen()),
            ),
          ),
          const SizedBox(height: 10),
          CrashButton(
            label: 'Garage',
            icon: Icons.garage_rounded,
            primary: false,
            onPressed: () => Navigator.of(
              context,
            ).push(MaterialPageRoute(builder: (_) => const GarageScreen())),
          ),
          const SizedBox(height: 10),
          CrashButton(
            label: 'Shop',
            icon: Icons.storefront_rounded,
            primary: false,
            onPressed: () => Navigator.of(
              context,
            ).push(MaterialPageRoute(builder: (_) => const ShopScreen())),
          ),
          const SizedBox(height: 16),
          const Divider(height: 1),
          const SizedBox(height: 14),
          const _HowItWorksRow(
            icon: Icons.local_gas_station_rounded,
            title: 'Hold gas',
            body: 'Accelerate into traffic and heavy targets.',
          ),
          const _HowItWorksRow(
            icon: Icons.car_crash_rounded,
            title: 'Crash',
            body: 'Shatter cars, props, and shops for combos.',
          ),
          const _HowItWorksRow(
            icon: Icons.emoji_events_rounded,
            title: 'Upgrade',
            body: 'Spend coins on faster, tougher cars.',
          ),
        ],
      ),
    );
  }
}

class _Metric extends StatelessWidget {
  const _Metric({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: const TextStyle(color: Colors.white60, fontSize: 12),
          ),
          Text(
            value,
            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 20),
          ),
        ],
      ),
    );
  }
}

class _HowItWorksRow extends StatelessWidget {
  const _HowItWorksRow({
    required this.icon,
    required this.title,
    required this.body,
  });

  final IconData icon;
  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, color: Theme.of(context).colorScheme.primary, size: 28),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  body,
                  style: const TextStyle(color: Colors.white70, height: 1.25),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
