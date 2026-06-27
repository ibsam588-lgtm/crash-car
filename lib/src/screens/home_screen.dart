import 'package:crash_car/src/screens/arena_select_screen.dart';
import 'package:crash_car/src/screens/garage_screen.dart';
import 'package:crash_car/src/screens/ramp_setup_screen.dart';
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
          'Smash through destructible arenas, or launch off a ramp into city traffic for slow-motion crash chains.',
          style: TextStyle(fontSize: 17, color: Colors.white, height: 1.35),
        ),
        const SizedBox(height: 18),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: [
            _Metric(label: 'Best', value: highScore.toString()),
            _Metric(label: 'Coins', value: coins.toString()),
            const _Metric(label: 'Modes', value: '2'),
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
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _ModeButton(
              title: 'Classic Arena',
              subtitle: 'Drive through traffic and destructible props.',
              icon: Icons.sports_motorsports_rounded,
              asset: 'assets/images/cars/realistic_muscle_orange.png',
              accent: const Color(0xFFFFC533),
              onPressed: () => Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const ArenaSelectScreen()),
              ),
            ),
            const SizedBox(height: 10),
            _ModeButton(
              title: 'Ramp Launch',
              subtitle: 'Pick arena, charge speed, then crash into the city.',
              icon: Icons.speed_rounded,
              asset: 'assets/images/traffic/realistic_sedan_blue.png',
              accent: const Color(0xFF42C7FF),
              onPressed: () => Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const RampSetupScreen()),
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: CrashButton(
                    label: 'Garage',
                    icon: Icons.garage_rounded,
                    primary: false,
                    onPressed: () => Navigator.of(context).push(
                      MaterialPageRoute(builder: (_) => const GarageScreen()),
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: CrashButton(
                    label: 'Shop',
                    icon: Icons.storefront_rounded,
                    primary: false,
                    onPressed: () => Navigator.of(context).push(
                      MaterialPageRoute(builder: (_) => const ShopScreen()),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Divider(height: 1),
            const SizedBox(height: 14),
            const _HowItWorksRow(
              icon: Icons.map_rounded,
              title: 'Choose arena',
              body: 'Downtown, docks, market, or construction yard.',
            ),
            const _HowItWorksRow(
              icon: Icons.local_gas_station_rounded,
              title: 'Build speed',
              body: 'Hold gas and steer into the best collision line.',
            ),
            const _HowItWorksRow(
              icon: Icons.car_crash_rounded,
              title: 'Chain impacts',
              body: 'Realistic cars break apart and keep scoring.',
            ),
          ],
        ),
      ),
    );
  }
}

class _ModeButton extends StatelessWidget {
  const _ModeButton({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.asset,
    required this.accent,
    required this.onPressed,
  });

  final String title;
  final String subtitle;
  final IconData icon;
  final String asset;
  final Color accent;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white.withValues(alpha: 0.06),
      borderRadius: BorderRadius.circular(8),
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: onPressed,
        child: DecoratedBox(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: accent.withValues(alpha: 0.45)),
          ),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                DecoratedBox(
                  decoration: BoxDecoration(
                    color: accent.withValues(alpha: 0.14),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: SizedBox(
                    width: 78,
                    height: 82,
                    child: Stack(
                      alignment: Alignment.center,
                      children: [
                        Icon(
                          icon,
                          color: accent.withValues(alpha: 0.34),
                          size: 54,
                        ),
                        Image.asset(asset, fit: BoxFit.contain),
                      ],
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: accent,
                          fontSize: 17,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        subtitle,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Colors.white70,
                          height: 1.25,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                Icon(Icons.chevron_right_rounded, color: accent),
              ],
            ),
          ),
        ),
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
