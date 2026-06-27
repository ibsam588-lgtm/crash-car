import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:crash_car/src/widgets/chrome.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class GarageScreen extends ConsumerStatefulWidget {
  const GarageScreen({super.key});

  @override
  ConsumerState<GarageScreen> createState() => _GarageScreenState();
}

class _GarageScreenState extends ConsumerState<GarageScreen> {
  int _index = 0;

  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      final selected = ref.read(progressProvider).selectedCarId;
      final selectedIndex = carSpecs.indexWhere((car) => car.id == selected);
      if (mounted && selectedIndex >= 0) {
        setState(() => _index = selectedIndex);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final progress = ref.watch(progressProvider);
    final car = carSpecs[_index];
    final unlocked = progress.unlockedCarIds.contains(car.id);
    final selected = progress.selectedCarId == car.id;
    final level = progress.upgradeLevel(car.id);
    final cost = ref.read(progressProvider.notifier).upgradeCost(car.id);
    final canUpgrade =
        unlocked && selected && level < 5 && progress.coins >= cost;

    return Scaffold(
      body: CrashBackground(
        image: 'assets/images/ui/garage_floor.png',
        dim: 0.32,
        child: Column(
          children: [
            AppTopBar(
              title: 'Garage',
              coins: progress.coins,
              onBack: () => Navigator.of(context).pop(),
            ),
            Expanded(
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final compact = constraints.maxWidth < 760;
                  final carPreview = _CarPreview(
                    car: car,
                    unlocked: unlocked,
                    onPrevious: _previous,
                    onNext: _next,
                  );
                  final details = _CarDetails(
                    car: car,
                    level: level,
                    unlocked: unlocked,
                    selected: selected,
                    upgradeCost: cost,
                    canUpgrade: canUpgrade,
                    onSelect: () =>
                        ref.read(progressProvider.notifier).selectCar(car.id),
                    onUnlock: () =>
                        ref.read(progressProvider.notifier).unlockCar(car.id),
                    onUpgrade: () => ref
                        .read(progressProvider.notifier)
                        .upgradeSelectedCar(),
                  );
                  if (compact) {
                    return ListView(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                      children: [
                        SizedBox(height: 380, child: carPreview),
                        const SizedBox(height: 14),
                        details,
                      ],
                    );
                  }
                  return Padding(
                    padding: const EdgeInsets.fromLTRB(18, 12, 18, 24),
                    child: Row(
                      children: [
                        Expanded(flex: 6, child: carPreview),
                        const SizedBox(width: 18),
                        SizedBox(width: 380, child: details),
                      ],
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _previous() {
    setState(() => _index = (_index - 1) % carSpecs.length);
  }

  void _next() {
    setState(() => _index = (_index + 1) % carSpecs.length);
  }
}

class _CarPreview extends StatelessWidget {
  const _CarPreview({
    required this.car,
    required this.unlocked,
    required this.onPrevious,
    required this.onNext,
  });

  final CarSpec car;
  final bool unlocked;
  final VoidCallback onPrevious;
  final VoidCallback onNext;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      child: Stack(
        children: [
          Positioned.fill(
            child: DecoratedBox(
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(8),
                gradient: RadialGradient(
                  colors: [
                    car.color.withValues(alpha: 0.32),
                    Colors.transparent,
                  ],
                  radius: 0.9,
                ),
              ),
            ),
          ),
          Center(
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 220),
              child: Opacity(
                key: ValueKey(car.id),
                opacity: unlocked ? 1 : 0.34,
                child: Image.asset(car.asset, height: 320, fit: BoxFit.contain),
              ),
            ),
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: IconButton.filledTonal(
              tooltip: 'Previous car',
              onPressed: onPrevious,
              icon: const Icon(Icons.chevron_left_rounded),
            ),
          ),
          Align(
            alignment: Alignment.centerRight,
            child: IconButton.filledTonal(
              tooltip: 'Next car',
              onPressed: onNext,
              icon: const Icon(Icons.chevron_right_rounded),
            ),
          ),
        ],
      ),
    );
  }
}

class _CarDetails extends StatelessWidget {
  const _CarDetails({
    required this.car,
    required this.level,
    required this.unlocked,
    required this.selected,
    required this.upgradeCost,
    required this.canUpgrade,
    required this.onSelect,
    required this.onUnlock,
    required this.onUpgrade,
  });

  final CarSpec car;
  final int level;
  final bool unlocked;
  final bool selected;
  final int upgradeCost;
  final bool canUpgrade;
  final VoidCallback onSelect;
  final VoidCallback onUnlock;
  final VoidCallback onUpgrade;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(car.name, style: Theme.of(context).textTheme.headlineMedium),
          const SizedBox(height: 4),
          Text(
            unlocked
                ? 'Unlocked  |  Upgrade $level/5'
                : 'Locked  |  ${car.price} coins',
            style: const TextStyle(color: Colors.white70),
          ),
          const SizedBox(height: 18),
          StatBar(
            label: 'Speed',
            value: (car.speed + level * 0.04).clamp(0, 1),
            color: car.color,
          ),
          const SizedBox(height: 12),
          StatBar(
            label: 'Handling',
            value: (car.handling + level * 0.035).clamp(0, 1),
            color: const Color(0xFF42C7FF),
          ),
          const SizedBox(height: 12),
          StatBar(
            label: 'Durability',
            value: (car.durability + level * 0.04).clamp(0, 1),
            color: const Color(0xFF7CD957),
          ),
          const SizedBox(height: 20),
          if (!unlocked)
            CrashButton(
              label: 'Unlock',
              icon: Icons.lock_open_rounded,
              onPressed: onUnlock,
            )
          else if (!selected)
            CrashButton(
              label: 'Select',
              icon: Icons.check_circle_rounded,
              onPressed: onSelect,
            )
          else
            CrashButton(
              label: 'Selected',
              icon: Icons.verified_rounded,
              onPressed: null,
            ),
          const SizedBox(height: 10),
          CrashButton(
            label: level >= 5 ? 'Max Upgrade' : 'Upgrade $upgradeCost',
            icon: Icons.build_rounded,
            primary: false,
            onPressed: canUpgrade ? onUpgrade : null,
          ),
        ],
      ),
    );
  }
}
