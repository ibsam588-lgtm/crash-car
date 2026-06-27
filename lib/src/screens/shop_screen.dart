import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:crash_car/src/widgets/chrome.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class ShopScreen extends ConsumerWidget {
  const ShopScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final progress = ref.watch(progressProvider);
    final wide = MediaQuery.sizeOf(context).width > 760;
    return Scaffold(
      body: CrashBackground(
        child: Column(
          children: [
            AppTopBar(
              title: 'Shop',
              coins: progress.coins,
              onBack: () => Navigator.of(context).pop(),
            ),
            Expanded(
              child: GridView.count(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                crossAxisCount: wide ? 3 : 1,
                mainAxisSpacing: 14,
                crossAxisSpacing: 14,
                childAspectRatio: wide ? 0.92 : 1.85,
                children: [
                  _ShopCard(
                    icon: Icons.play_circle_fill_rounded,
                    title: 'Sponsor Bonus',
                    body: 'Rewarded-ad placeholder for Play Store builds.',
                    action: 'Claim 75',
                    onPressed: () =>
                        ref.read(progressProvider.notifier).claimSponsorBonus(),
                  ),
                  const _ShopCard(
                    icon: Icons.payments_rounded,
                    title: 'Coin Packs',
                    body:
                        'IAP-ready tile. Add Google Play Billing when merchant and product IDs are ready.',
                    action: 'Coming Soon',
                  ),
                  const _ShopCard(
                    icon: Icons.no_crash_rounded,
                    title: 'Remove Ads',
                    body:
                        'Reserved for a premium purchase SKU after Play Console setup.',
                    action: 'Coming Soon',
                  ),
                  for (final car in carSpecs.skip(1))
                    _CarUnlockCard(
                      car: car,
                      unlocked: progress.unlockedCarIds.contains(car.id),
                      onUnlock: () =>
                          ref.read(progressProvider.notifier).unlockCar(car.id),
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

class _ShopCard extends StatelessWidget {
  const _ShopCard({
    required this.icon,
    required this.title,
    required this.body,
    required this.action,
    this.onPressed,
  });

  final IconData icon;
  final String title;
  final String body;
  final String action;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: Theme.of(context).colorScheme.primary, size: 40),
          const SizedBox(height: 12),
          Text(title, style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          Expanded(
            child: Text(
              body,
              style: const TextStyle(color: Colors.white70, height: 1.3),
            ),
          ),
          CrashButton(
            label: action,
            icon: Icons.arrow_forward_rounded,
            onPressed: onPressed,
          ),
        ],
      ),
    );
  }
}

class _CarUnlockCard extends StatelessWidget {
  const _CarUnlockCard({
    required this.car,
    required this.unlocked,
    required this.onUnlock,
  });

  final CarSpec car;
  final bool unlocked;
  final VoidCallback onUnlock;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      borderColor: car.color.withValues(alpha: 0.55),
      child: Row(
        children: [
          SizedBox(
            width: 112,
            child: Image.asset(car.asset, fit: BoxFit.contain),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(car.name, style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                Text(
                  unlocked ? 'Unlocked' : '${car.price} coins',
                  style: const TextStyle(color: Colors.white70),
                ),
                const SizedBox(height: 12),
                CrashButton(
                  label: unlocked ? 'Owned' : 'Unlock',
                  icon: unlocked
                      ? Icons.check_rounded
                      : Icons.lock_open_rounded,
                  onPressed: unlocked ? null : onUnlock,
                  primary: false,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
