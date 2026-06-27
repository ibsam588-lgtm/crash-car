import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/screens/ramp_play_screen.dart';
import 'package:crash_car/src/state/progress_controller.dart';
import 'package:crash_car/src/widgets/chrome.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class RampSetupScreen extends ConsumerStatefulWidget {
  const RampSetupScreen({super.key});

  @override
  ConsumerState<RampSetupScreen> createState() => _RampSetupScreenState();
}

class _RampSetupScreenState extends ConsumerState<RampSetupScreen> {
  ArenaSpec _arena = arenaSpecs[1];
  String? _carId;
  int _chargeSeconds = 10;

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
        image: 'assets/images/key_art.png',
        dim: 0.66,
        child: Column(
          children: [
            AppTopBar(
              title: 'Ramp Launch',
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
                            child: _LaunchPreview(
                              arena: _arena,
                              car: selectedCar,
                              chargeSeconds: _chargeSeconds,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            flex: 4,
                            child: _SetupControls(
                              arena: _arena,
                              selectedCar: selectedCar,
                              unlockedCars: unlockedCars,
                              chargeSeconds: _chargeSeconds,
                              onArenaChanged: (arena) =>
                                  setState(() => _arena = arena),
                              onCarChanged: (car) =>
                                  setState(() => _carId = car.id),
                              onChargeChanged: (seconds) =>
                                  setState(() => _chargeSeconds = seconds),
                              onStart: () => _start(selectedCar),
                            ),
                          ),
                        ],
                      )
                    : Column(
                        children: [
                          _LaunchPreview(
                            arena: _arena,
                            car: selectedCar,
                            chargeSeconds: _chargeSeconds,
                          ),
                          const SizedBox(height: 14),
                          _SetupControls(
                            arena: _arena,
                            selectedCar: selectedCar,
                            unlockedCars: unlockedCars,
                            chargeSeconds: _chargeSeconds,
                            onArenaChanged: (arena) =>
                                setState(() => _arena = arena),
                            onCarChanged: (car) =>
                                setState(() => _carId = car.id),
                            onChargeChanged: (seconds) =>
                                setState(() => _chargeSeconds = seconds),
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
        builder: (_) => RampPlayScreen(
          arena: _arena,
          car: selectedCar,
          chargeSeconds: _chargeSeconds,
        ),
      ),
    );
  }
}

class _LaunchPreview extends StatelessWidget {
  const _LaunchPreview({
    required this.arena,
    required this.car,
    required this.chargeSeconds,
  });

  final ArenaSpec arena;
  final CarSpec car;
  final int chargeSeconds;

  @override
  Widget build(BuildContext context) {
    return GlassPanel(
      borderColor: arena.primary.withValues(alpha: 0.45),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(arena.icon, color: arena.primary, size: 34),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      arena.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.headlineSmall
                          ?.copyWith(fontWeight: FontWeight.w900),
                    ),
                    Text(
                      '$chargeSeconds sec ramp / ${car.name}',
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
            aspectRatio: 1.42,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: Stack(
                fit: StackFit.expand,
                children: [
                  Image.asset(
                    'assets/images/ui/city_intersection.png',
                    fit: BoxFit.cover,
                  ),
                  ColoredBox(color: Colors.black.withValues(alpha: 0.22)),
                  Positioned(
                    left: 0,
                    right: 0,
                    bottom: -38,
                    child: Center(
                      child: Transform.rotate(
                        angle: -0.13,
                        child: Image.asset(
                          car.asset,
                          width: 116,
                          height: 254,
                          fit: BoxFit.contain,
                        ),
                      ),
                    ),
                  ),
                  Positioned(
                    right: 18,
                    top: 18,
                    child: Image.asset(
                      'assets/images/traffic/realistic_sedan_blue.png',
                      width: 70,
                      height: 134,
                      fit: BoxFit.contain,
                    ),
                  ),
                  Positioned(
                    left: 16,
                    top: 28,
                    child: Image.asset(
                      'assets/images/traffic/delivery_truck_blue.png',
                      width: 82,
                      height: 154,
                      fit: BoxFit.contain,
                    ),
                  ),
                ],
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
                child: StatBar(label: 'Damage', value: car.durability),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _SetupControls extends StatelessWidget {
  const _SetupControls({
    required this.arena,
    required this.selectedCar,
    required this.unlockedCars,
    required this.chargeSeconds,
    required this.onArenaChanged,
    required this.onCarChanged,
    required this.onChargeChanged,
    required this.onStart,
  });

  final ArenaSpec arena;
  final CarSpec selectedCar;
  final List<CarSpec> unlockedCars;
  final int chargeSeconds;
  final ValueChanged<ArenaSpec> onArenaChanged;
  final ValueChanged<CarSpec> onCarChanged;
  final ValueChanged<int> onChargeChanged;
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
            height: 132,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: unlockedCars.length,
              separatorBuilder: (_, _) => const SizedBox(width: 10),
              itemBuilder: (context, index) {
                final car = unlockedCars[index];
                final selected = car.id == selectedCar.id;
                return _CarPickCard(
                  car: car,
                  selected: selected,
                  onPressed: () => onCarChanged(car),
                );
              },
            ),
          ),
          const SizedBox(height: 18),
          Text('Ramp Time', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            children: [
              for (final seconds in const [5, 10, 15, 20])
                ChoiceChip(
                  selected: chargeSeconds == seconds,
                  label: Text('${seconds}s'),
                  selectedColor: const Color(
                    0xFFFFC533,
                  ).withValues(alpha: 0.26),
                  onSelected: (_) => onChargeChanged(seconds),
                ),
            ],
          ),
          const SizedBox(height: 20),
          CrashButton(
            label: 'Launch',
            icon: Icons.speed_rounded,
            onPressed: onStart,
          ),
        ],
      ),
    );
  }
}

class _CarPickCard extends StatelessWidget {
  const _CarPickCard({
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
          width: 116,
          child: Padding(
            padding: const EdgeInsets.all(10),
            child: Column(
              children: [
                Expanded(child: Image.asset(car.asset, fit: BoxFit.contain)),
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
