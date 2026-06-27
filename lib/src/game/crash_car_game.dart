import 'dart:math';

import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:flame/collisions.dart';
import 'package:flame/components.dart';
import 'package:flame/events.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';

class CrashCarGame extends FlameGame with HasCollisionDetection, PanDetector {
  CrashCarGame({
    required this.car,
    required this.arena,
    required this.upgradeLevel,
    required this.onFinished,
  });

  final CarSpec car;
  final ArenaSpec arena;
  final int upgradeLevel;
  final void Function(GameResult result) onFinished;

  final score = ValueNotifier<int>(0);
  final coins = ValueNotifier<int>(0);
  final damage = ValueNotifier<int>(0);
  final objectsHit = ValueNotifier<int>(0);
  final speedKmh = ValueNotifier<int>(0);
  final combo = ValueNotifier<int>(1);
  final levelProgress = ValueNotifier<double>(0);
  final slowMotion = ValueNotifier<bool>(false);
  final impactText = ValueNotifier<String>('');
  final scorePopupText = ValueNotifier<String>('');

  final Random _random = Random();
  late PlayerCar _player;
  late final Sprite _crateSprite;
  late final Sprite _redBarrelSprite;
  late final Sprite _steelBarrelSprite;
  late final Sprite _coneSprite;
  late final Sprite _barricadeSprite;
  late final List<VehicleVisual> _trafficCarVisuals;
  late final Sprite _boxTruckSprite;
  late final Sprite _boxTruckWreckSprite;
  late final Sprite _deliveryTruckSprite;
  late final Sprite _deliveryTruckWreckSprite;
  late final Sprite _cityBusSprite;
  late final Sprite _cityBusWreckSprite;
  late final List<Sprite> _shopSprites;
  late final List<Sprite> _debrisSprites;
  late final List<Sprite> _carFragmentSprites;
  late final List<Sprite> _glassShardSprites;
  late final List<Sprite> _metalShardSprites;

  double _breakableTimer = 0;
  double _trafficTimer = 0.35;
  double _sceneryTimer = 0.8;
  double _elapsed = 0;
  double _roadScroll = 0;
  double _steering = 0;
  bool _accelerating = false;
  bool _finished = false;
  int _bestCombo = 1;
  double _comboTimer = 0;
  double _slowMotionTimer = 0;
  double _impactTextTimer = 0;
  double _scorePopupTimer = 0;
  double _currentRoadSpeed = 0;
  double _playerImpactTimer = 0;
  double _playerImpactDuration = 0.001;
  double _playerImpactLean = 0;
  double _playerImpactDrift = 0;

  double get _carBoost => upgradeLevel * 0.04;

  double get _speedTune => car.speed + _carBoost + arena.speedBonus;

  double get _idleRoadSpeed => 210 + _speedTune * 120;

  double get _cruiseRoadSpeed => 305 + _speedTune * 230;

  double get _maxRoadSpeed => _cruiseRoadSpeed + 175;

  double get _targetRoadSpeed => _accelerating ? _maxRoadSpeed : _idleRoadSpeed;

  double get _slowFactor => _slowMotionTimer > 0 ? 0.34 : 1;

  double get effectiveRoadSpeed => _currentRoadSpeed * _slowFactor;

  double get _steerSpeed => 290 + car.handling * 260 + upgradeLevel * 18;

  double get _damageMultiplier =>
      max(0.45, 1.06 - car.durability * 0.42 - upgradeLevel * 0.035);

  double get _levelLength => 70;

  @override
  Color backgroundColor() => const Color(0xFF071014);

  @override
  Future<void> onLoad() async {
    final carFragmentKeys = List.generate(
      20,
      (index) =>
          'debris/car_fragment_${(index + 1).toString().padLeft(2, '0')}.png',
    );
    final glassShardKeys = List.generate(
      10,
      (index) =>
          'debris/glass_shard_${(index + 1).toString().padLeft(2, '0')}.png',
    );
    final metalShardKeys = List.generate(
      8,
      (index) =>
          'debris/metal_shard_${(index + 1).toString().padLeft(2, '0')}.png',
    );

    await images.loadAll([
      _assetKey(car.asset),
      'cars/realistic_interceptor_blue.png',
      'cars/wrecked_realistic_interceptor_blue.png',
      'cars/realistic_rally_green.png',
      'cars/wrecked_realistic_rally_green.png',
      'cars/realistic_stunt_red.png',
      'cars/wrecked_realistic_stunt_red.png',
      'ui/road_lane.png',
      'obstacles/crate.png',
      'obstacles/red_barrel.png',
      'obstacles/steel_barrel.png',
      'obstacles/cone.png',
      'obstacles/barricade.png',
      'traffic/box_truck_red.png',
      'traffic/wrecked_box_truck_red.png',
      'traffic/delivery_truck_blue.png',
      'traffic/wrecked_delivery_truck_blue.png',
      'traffic/city_bus.png',
      'traffic/wrecked_city_bus.png',
      'traffic/corner_shop.png',
      'traffic/repair_shop.png',
      'traffic/market_stall.png',
      'debris/wood_1.png',
      'debris/wood_2.png',
      'debris/metal_1.png',
      'debris/glass_1.png',
      ...carFragmentKeys,
      ...glassShardKeys,
      ...metalShardKeys,
    ]);

    _crateSprite = Sprite(images.fromCache('obstacles/crate.png'));
    _redBarrelSprite = Sprite(images.fromCache('obstacles/red_barrel.png'));
    _steelBarrelSprite = Sprite(images.fromCache('obstacles/steel_barrel.png'));
    _coneSprite = Sprite(images.fromCache('obstacles/cone.png'));
    _barricadeSprite = Sprite(images.fromCache('obstacles/barricade.png'));
    _trafficCarVisuals = [
      VehicleVisual(
        sprite: Sprite(images.fromCache('cars/realistic_interceptor_blue.png')),
        wreckSprite: Sprite(
          images.fromCache('cars/wrecked_realistic_interceptor_blue.png'),
        ),
      ),
      VehicleVisual(
        sprite: Sprite(images.fromCache('cars/realistic_rally_green.png')),
        wreckSprite: Sprite(
          images.fromCache('cars/wrecked_realistic_rally_green.png'),
        ),
      ),
      VehicleVisual(
        sprite: Sprite(images.fromCache('cars/realistic_stunt_red.png')),
        wreckSprite: Sprite(
          images.fromCache('cars/wrecked_realistic_stunt_red.png'),
        ),
      ),
    ];
    _boxTruckSprite = Sprite(images.fromCache('traffic/box_truck_red.png'));
    _boxTruckWreckSprite = Sprite(
      images.fromCache('traffic/wrecked_box_truck_red.png'),
    );
    _deliveryTruckSprite = Sprite(
      images.fromCache('traffic/delivery_truck_blue.png'),
    );
    _deliveryTruckWreckSprite = Sprite(
      images.fromCache('traffic/wrecked_delivery_truck_blue.png'),
    );
    _cityBusSprite = Sprite(images.fromCache('traffic/city_bus.png'));
    _cityBusWreckSprite = Sprite(
      images.fromCache('traffic/wrecked_city_bus.png'),
    );
    _shopSprites = [
      Sprite(images.fromCache('traffic/corner_shop.png')),
      Sprite(images.fromCache('traffic/repair_shop.png')),
      Sprite(images.fromCache('traffic/market_stall.png')),
    ];
    _debrisSprites = [
      Sprite(images.fromCache('debris/wood_1.png')),
      Sprite(images.fromCache('debris/wood_2.png')),
      Sprite(images.fromCache('debris/metal_1.png')),
      Sprite(images.fromCache('debris/glass_1.png')),
    ];
    _carFragmentSprites = [
      for (final key in carFragmentKeys) Sprite(images.fromCache(key)),
    ];
    _glassShardSprites = [
      for (final key in glassShardKeys) Sprite(images.fromCache(key)),
    ];
    _metalShardSprites = [
      for (final key in metalShardKeys) Sprite(images.fromCache(key)),
    ];
    _currentRoadSpeed = _idleRoadSpeed;

    add(
      RoadLayer(
        sprite: Sprite(images.fromCache('ui/road_lane.png')),
        arena: arena,
      ),
    );
    _player = PlayerCar(
      sprite: Sprite(images.fromCache(_assetKey(car.asset))),
      game: this,
    );
    add(_player);
  }

  @override
  void onGameResize(Vector2 size) {
    super.onGameResize(size);
    if (isLoaded && _player.isMounted) {
      _player.position = Vector2(size.x / 2, size.y - 190);
      _player.size = Vector2(82, 178);
    }
  }

  @override
  void update(double dt) {
    super.update(dt);
    if (_finished || size.x <= 0 || size.y <= 0) {
      return;
    }

    _updateSpeed(dt);
    _elapsed += dt;
    _roadScroll = (_roadScroll + effectiveRoadSpeed * dt) % max(1, size.y);
    _breakableTimer -= dt;
    _trafficTimer -= dt;
    _sceneryTimer -= dt;
    _comboTimer -= dt;
    _slowMotionTimer -= dt;
    _impactTextTimer -= dt;
    _scorePopupTimer -= dt;
    _playerImpactTimer -= dt;

    if (_slowMotionTimer <= 0 && slowMotion.value) {
      slowMotion.value = false;
    }
    if (_impactTextTimer <= 0 && impactText.value.isNotEmpty) {
      impactText.value = '';
    }
    if (_scorePopupTimer <= 0 && scorePopupText.value.isNotEmpty) {
      scorePopupText.value = '';
    }
    if (_comboTimer <= 0 && combo.value != 1) {
      combo.value = 1;
    }

    var playerX = _player.x;
    if (_playerImpactTimer > 0) {
      playerX += _playerImpactDrift * dt;
      _playerImpactDrift *= pow(0.08, dt).toDouble();
    }

    final nextX = (playerX + _steering * _steerSpeed * dt)
        .clamp(54, size.x - 54)
        .toDouble();
    _player.x = nextX;
    final impactLean = _playerImpactTimer > 0
        ? _playerImpactLean * (_playerImpactTimer / _playerImpactDuration)
        : 0.0;
    _player.angle = _steering * 0.08 + impactLean;

    final speed = (_currentRoadSpeed * 0.36).round().clamp(0, 285);
    if (speedKmh.value != speed) {
      speedKmh.value = speed;
    }

    final progress = (_elapsed / _levelLength).clamp(0.0, 1.0);
    if ((levelProgress.value - progress).abs() > 0.002) {
      levelProgress.value = progress;
    }

    if (_breakableTimer <= 0) {
      _spawnBreakable();
      _breakableTimer = max(
        0.32,
        (0.92 - min(0.32, _elapsed * 0.004)) / max(0.7, arena.sceneryDensity),
      );
    }

    if (_trafficTimer <= 0) {
      _spawnTraffic();
      _trafficTimer = max(
        0.55,
        (1.55 - min(0.42, _elapsed * 0.006)) / max(0.65, arena.trafficDensity),
      );
    }

    if (_sceneryTimer <= 0) {
      _spawnRoadsideScenery();
      _sceneryTimer = max(
        0.74,
        (1.8 - min(0.5, _elapsed * 0.005)) / max(0.7, arena.sceneryDensity),
      );
    }

    if (_elapsed >= _levelLength) {
      _finish(levelComplete: true);
    }
  }

  @override
  void render(Canvas canvas) {
    super.render(canvas);
    final paint = Paint()..color = arena.primary.withValues(alpha: 0.055);
    final horizon = Path()
      ..moveTo(0, size.y * 0.18)
      ..lineTo(size.x, size.y * 0.05)
      ..lineTo(size.x, 0)
      ..lineTo(0, 0)
      ..close();
    canvas.drawPath(horizon, paint);
  }

  @override
  void onPanUpdate(DragUpdateInfo info) {
    _player.x = (_player.x + info.delta.global.x)
        .clamp(54, size.x - 54)
        .toDouble();
  }

  void setSteering(double value) {
    _steering = value.clamp(-1, 1).toDouble();
  }

  void setAccelerating(bool value) {
    _accelerating = value;
  }

  void setBoosting(bool value) {
    setAccelerating(value);
  }

  void _updateSpeed(double dt) {
    final target = _targetRoadSpeed;
    final acceleration = (_accelerating ? 310.0 : 165.0) * dt;
    if (_currentRoadSpeed < target) {
      _currentRoadSpeed = min(target, _currentRoadSpeed + acceleration);
    } else {
      _currentRoadSpeed = max(target, _currentRoadSpeed - acceleration);
    }

    final floor = _idleRoadSpeed * (_accelerating ? 0.46 : 0.66);
    _currentRoadSpeed = _currentRoadSpeed
        .clamp(floor, _maxRoadSpeed)
        .toDouble();
  }

  void pauseOrResume() {
    paused = !paused;
  }

  void forceFinish() {
    _finish(levelComplete: false);
  }

  void smash(ImpactTargetComponent target) {
    if (target.hit || _finished) {
      return;
    }
    final hitPosition = target.position.clone();
    final impactSide = _impactSide(hitPosition);
    target.hit = true;
    target.removeFromParent();

    final activeCombo = (_comboTimer > 0 ? combo.value + 1 : 1).clamp(1, 12);
    combo.value = activeCombo;
    _bestCombo = max(_bestCombo, activeCombo);
    _comboTimer = 1.45;

    objectsHit.value += 1;
    final damageGain = (target.damage * _damageMultiplier).round().clamp(1, 18);
    damage.value = (damage.value + damageGain).clamp(0, 100);

    final speedBonus = (speedKmh.value * target.mass * 2.4).round();
    final points =
        ((target.points + speedBonus) *
                arena.scoreBonus *
                (1 + activeCombo * 0.18))
            .round();
    score.value += points;
    coins.value += max(1, points ~/ 90);
    scorePopupText.value = '+$points';
    _scorePopupTimer = max(_scorePopupTimer, 0.95);

    _applyCollisionSlowdown(target);
    _applyPlayerImpact(target, impactSide);
    _triggerSlowMotion(target);
    _spawnWreck(target, hitPosition, impactSide);
    _burst(target, hitPosition);
  }

  void _triggerSlowMotion(ImpactTargetComponent target) {
    _slowMotionTimer = max(_slowMotionTimer, target.slowMotionSeconds);
    slowMotion.value = true;
    impactText.value = target.impactLabel;
    _impactTextTimer = max(_impactTextTimer, target.slowMotionSeconds + 0.35);
  }

  void _applyCollisionSlowdown(ImpactTargetComponent target) {
    final severity = target.isVehicle
        ? 1.45
        : target.isHeavy
        ? 1.12
        : 0.72;
    final loss = 38 + target.mass * 52 * severity + _random.nextDouble() * 26;
    final floor = _idleRoadSpeed * (target.isVehicle ? 0.42 : 0.58);
    _currentRoadSpeed = max(floor, _currentRoadSpeed - loss);
  }

  void _applyPlayerImpact(ImpactTargetComponent target, double side) {
    _playerImpactDuration = target.isVehicle ? 0.82 : 0.48;
    _playerImpactTimer = _playerImpactDuration;
    _playerImpactLean = side * (target.isVehicle ? 0.24 : 0.14);
    _playerImpactDrift = side * (target.isVehicle ? 120 : 62) * target.mass;
  }

  double _impactSide(Vector2 hitPosition) {
    final delta = _player.x - hitPosition.x;
    if (delta.abs() < 8) {
      return _random.nextBool() ? 1 : -1;
    }
    return delta.sign.toDouble();
  }

  void _spawnBreakable() {
    final lanes = _lanePositions();
    final roll = _random.nextDouble();
    final TargetKind kind;
    if (roll < 0.25) {
      kind = TargetKind.crate;
    } else if (roll < 0.45) {
      kind = TargetKind.redBarrel;
    } else if (roll < 0.63) {
      kind = TargetKind.steelBarrel;
    } else if (roll < 0.84) {
      kind = TargetKind.cone;
    } else {
      kind = TargetKind.barricade;
    }

    add(
      ImpactTargetComponent(
        kind: kind,
        sprite: _spriteFor(kind),
        crashGame: this,
        position: Vector2(lanes[_random.nextInt(lanes.length)], -84),
        roadSpeedFactor: 1,
        lateralVelocity: (_random.nextDouble() - 0.5) * 10,
      ),
    );
  }

  void _spawnTraffic() {
    final lanes = _lanePositions();
    final roll = _random.nextDouble();
    late final TargetKind kind;
    late final Sprite sprite;
    late final Sprite wreckSprite;

    final truckChance = switch (arena.id) {
      'industrial_docks' => 0.52,
      'downtown_strip' => 0.34,
      _ => 0.24,
    };
    final busChance = arena.id == 'downtown_strip' ? 0.18 : 0.07;

    if (roll < truckChance) {
      kind = TargetKind.truck;
      final boxTruck = _random.nextBool();
      sprite = boxTruck ? _boxTruckSprite : _deliveryTruckSprite;
      wreckSprite = boxTruck ? _boxTruckWreckSprite : _deliveryTruckWreckSprite;
    } else if (roll < truckChance + busChance) {
      kind = TargetKind.bus;
      sprite = _cityBusSprite;
      wreckSprite = _cityBusWreckSprite;
    } else {
      kind = TargetKind.trafficCar;
      final visual =
          _trafficCarVisuals[_random.nextInt(_trafficCarVisuals.length)];
      sprite = visual.sprite;
      wreckSprite = visual.wreckSprite;
    }

    final oncoming = _random.nextDouble() < 0.34;
    add(
      ImpactTargetComponent(
        kind: kind,
        sprite: sprite,
        wreckSprite: wreckSprite,
        crashGame: this,
        position: Vector2(lanes[_random.nextInt(lanes.length)], -120),
        roadSpeedFactor: oncoming ? 1.28 : 0.64 + _random.nextDouble() * 0.18,
        lateralVelocity:
            (_random.nextDouble() - 0.5) * (kind == TargetKind.truck ? 22 : 38),
        reverseFacing: !oncoming,
      ),
    );
  }

  void _spawnRoadsideScenery() {
    final bounds = _roadBounds();
    final onRight = _random.nextBool();
    final kind = _random.nextDouble() < 0.72
        ? TargetKind.shop
        : TargetKind.barricade;
    final x = onRight ? bounds.right - 46 : bounds.left + 46;
    final sprite = kind == TargetKind.shop
        ? _shopSprites[_random.nextInt(_shopSprites.length)]
        : _barricadeSprite;

    add(
      ImpactTargetComponent(
        kind: kind,
        sprite: sprite,
        crashGame: this,
        position: Vector2(x, -86),
        roadSpeedFactor: 1,
        lateralVelocity: onRight ? -6 : 6,
      ),
    );
  }

  Sprite _spriteFor(TargetKind kind) {
    return switch (kind) {
      TargetKind.crate => _crateSprite,
      TargetKind.redBarrel => _redBarrelSprite,
      TargetKind.steelBarrel => _steelBarrelSprite,
      TargetKind.cone => _coneSprite,
      TargetKind.barricade => _barricadeSprite,
      TargetKind.trafficCar =>
        _trafficCarVisuals[_random.nextInt(_trafficCarVisuals.length)].sprite,
      TargetKind.truck => _boxTruckSprite,
      TargetKind.bus => _cityBusSprite,
      TargetKind.shop => _shopSprites[_random.nextInt(_shopSprites.length)],
    };
  }

  List<double> _lanePositions() {
    final bounds = _roadBounds();
    return [
      bounds.left + 55,
      bounds.left + bounds.width * 0.34,
      bounds.left + bounds.width * 0.66,
      bounds.right - 55,
    ];
  }

  Rect _roadBounds() {
    final roadWidth = min(size.x, 520.0);
    final left = (size.x - roadWidth) / 2;
    return Rect.fromLTWH(left, 0, roadWidth, size.y);
  }

  void _spawnWreck(
    ImpactTargetComponent target,
    Vector2 at,
    double playerImpactSide,
  ) {
    if (!target.isVehicle) {
      return;
    }

    final wreckSprite = target.wreckSprite ?? target.sprite;
    if (wreckSprite == null) {
      return;
    }

    final wreckSide = -playerImpactSide;
    final isLargeVehicle =
        target.kind == TargetKind.truck || target.kind == TargetKind.bus;
    final lateralSpeed = isLargeVehicle
        ? 145 + _random.nextDouble() * 70
        : 210 + _random.nextDouble() * 115;
    final spin =
        wreckSide *
        (isLargeVehicle ? 1.05 : 2.25) *
        (0.82 + _random.nextDouble() * 0.46);

    add(
      WreckedVehicleComponent(
        sprite: wreckSprite,
        position: at,
        size: target.size.clone(),
        angle: target.angle + wreckSide * 0.16,
        crushSide: wreckSide,
        velocity: Vector2(
          wreckSide * lateralSpeed,
          effectiveRoadSpeed * (isLargeVehicle ? 0.34 : 0.22) +
              42 +
              _random.nextDouble() * 44,
        ),
        spin: spin,
        life: isLargeVehicle ? 2.25 : 1.85,
      ),
    );
  }

  void _burst(ImpactTargetComponent target, Vector2 at) {
    if (target.isVehicle) {
      final bodyCount = target.kind == TargetKind.trafficCar ? 16 : 24;
      final glassCount = target.kind == TargetKind.trafficCar ? 11 : 15;
      final metalCount = target.kind == TargetKind.trafficCar ? 8 : 13;

      for (var i = 0; i < bodyCount; i++) {
        _spawnDebris(
          sprites: _carFragmentSprites,
          at: at,
          minSpeed: 130,
          maxSpeed: target.kind == TargetKind.trafficCar ? 470 : 610,
          spread: 3.35,
          edge: target.kind == TargetKind.trafficCar ? 48 : 58,
          life: 1.28,
        );
      }
      for (var i = 0; i < glassCount; i++) {
        _spawnDebris(
          sprites: _glassShardSprites,
          at: at + Vector2((_random.nextDouble() - 0.5) * 34, -18),
          minSpeed: 170,
          maxSpeed: 620,
          spread: 3.75,
          edge: 25,
          life: 0.96,
        );
      }
      for (var i = 0; i < metalCount; i++) {
        _spawnDebris(
          sprites: _metalShardSprites,
          at: at,
          minSpeed: 120,
          maxSpeed: 520,
          spread: 3.2,
          edge: 30,
          life: 1.2,
        );
      }
      return;
    }

    final heavy = target.isExplosive || target.isHeavy;
    final count = heavy ? 18 : 11;
    for (var i = 0; i < count; i++) {
      _spawnDebris(
        sprites: _debrisSprites,
        at: at,
        minSpeed: 90,
        maxSpeed: heavy ? 480 : 330,
        spread: 2.6,
        edge: heavy ? 42 : 30,
      );
    }

    if (target.kind == TargetKind.shop || target.kind == TargetKind.barricade) {
      for (var i = 0; i < 6; i++) {
        _spawnDebris(
          sprites: _glassShardSprites,
          at: at,
          minSpeed: 120,
          maxSpeed: 420,
          spread: 3.0,
          edge: 22,
          life: 0.95,
        );
      }
    }
  }

  void _spawnDebris({
    required List<Sprite> sprites,
    required Vector2 at,
    required double minSpeed,
    required double maxSpeed,
    required double spread,
    required double edge,
    double life = 1.15,
  }) {
    final sprite = sprites[_random.nextInt(sprites.length)];
    final angle = -pi / 2 + (_random.nextDouble() - 0.5) * spread;
    final speed = minSpeed + _random.nextDouble() * (maxSpeed - minSpeed);
    add(
      DebrisParticle(
        sprite: sprite,
        position:
            at +
            Vector2(
              (_random.nextDouble() - 0.5) * 58,
              (_random.nextDouble() - 0.5) * 58,
            ),
        velocity:
            Vector2(cos(angle), sin(angle)) * speed +
            Vector2(0, effectiveRoadSpeed * 0.5),
        spin: (_random.nextDouble() - 0.5) * 12,
        size: _debrisSize(sprite, edge * (0.76 + _random.nextDouble() * 0.62)),
        life: life,
      ),
    );
  }

  Vector2 _debrisSize(Sprite sprite, double longestEdge) {
    final source = sprite.srcSize;
    final maxSide = max(source.x, source.y);
    if (maxSide <= 0) {
      return Vector2.all(longestEdge);
    }
    return source * (longestEdge / maxSide);
  }

  void _finish({required bool levelComplete}) {
    if (_finished) {
      return;
    }
    _finished = true;
    onFinished(
      GameResult(
        arenaName: arena.name,
        score: score.value,
        coins: coins.value + (levelComplete ? 80 : 0),
        damagePercent: damage.value,
        objectsHit: objectsHit.value,
        maxSpeed: speedKmh.value,
        bestCombo: _bestCombo,
        levelComplete: levelComplete,
      ),
    );
  }

  double get roadScroll => _roadScroll;

  String _assetKey(String fullPath) =>
      fullPath.replaceFirst('assets/images/', '');
}

class RoadLayer extends PositionComponent with HasGameReference<CrashCarGame> {
  RoadLayer({required this.sprite, required this.arena});

  final Sprite sprite;
  final ArenaSpec arena;

  @override
  void render(Canvas canvas) {
    final gameSize = game.size;
    if (gameSize.x <= 0 || gameSize.y <= 0) {
      return;
    }
    final roadWidth = min(gameSize.x, 520.0);
    final left = (gameSize.x - roadWidth) / 2;
    final segmentHeight = gameSize.y;
    for (
      var y = -segmentHeight + game.roadScroll;
      y < gameSize.y + segmentHeight;
      y += segmentHeight
    ) {
      sprite.render(
        canvas,
        position: Vector2(left, y),
        size: Vector2(roadWidth, segmentHeight),
      );
    }

    final roadTint = Paint()..color = arena.roadTint.withValues(alpha: 0.18);
    canvas.drawRect(Rect.fromLTWH(left, 0, roadWidth, gameSize.y), roadTint);

    final sidePaint = Paint()..color = arena.sideTint;
    canvas.drawRect(Rect.fromLTWH(0, 0, left, gameSize.y), sidePaint);
    canvas.drawRect(
      Rect.fromLTWH(left + roadWidth, 0, left, gameSize.y),
      sidePaint,
    );

    final edgePaint = Paint()
      ..color = arena.primary.withValues(alpha: 0.18)
      ..strokeWidth = 3;
    canvas.drawLine(
      Offset(left + 36, 0),
      Offset(left + 36, gameSize.y),
      edgePaint,
    );
    canvas.drawLine(
      Offset(left + roadWidth - 36, 0),
      Offset(left + roadWidth - 36, gameSize.y),
      edgePaint,
    );
  }
}

class PlayerCar extends SpriteComponent with CollisionCallbacks {
  PlayerCar({required super.sprite, required this.game})
    : super(anchor: Anchor.center, priority: 20);

  final CrashCarGame game;

  @override
  Future<void> onLoad() async {
    size = Vector2(82, 178);
    position = Vector2(game.size.x / 2, game.size.y - 190);
    add(RectangleHitbox(size: Vector2(58, 142), position: Vector2(12, 18)));
  }

  @override
  void onCollisionStart(
    Set<Vector2> intersectionPoints,
    PositionComponent other,
  ) {
    super.onCollisionStart(intersectionPoints, other);
    if (other is ImpactTargetComponent) {
      game.smash(other);
    }
  }
}

enum TargetKind {
  crate,
  redBarrel,
  steelBarrel,
  cone,
  barricade,
  trafficCar,
  truck,
  bus,
  shop,
}

class VehicleVisual {
  const VehicleVisual({required this.sprite, required this.wreckSprite});

  final Sprite sprite;
  final Sprite wreckSprite;
}

class ImpactTargetComponent extends SpriteComponent with CollisionCallbacks {
  ImpactTargetComponent({
    required this.kind,
    required super.sprite,
    required this.crashGame,
    required super.position,
    required this.roadSpeedFactor,
    required this.lateralVelocity,
    this.wreckSprite,
    this.reverseFacing = false,
  }) : super(anchor: Anchor.center, priority: 10);

  final TargetKind kind;
  final CrashCarGame crashGame;
  final double roadSpeedFactor;
  final double lateralVelocity;
  final Sprite? wreckSprite;
  final bool reverseFacing;
  bool hit = false;

  int get damage {
    return switch (kind) {
      TargetKind.crate => 4,
      TargetKind.redBarrel => 7,
      TargetKind.steelBarrel => 6,
      TargetKind.cone => 2,
      TargetKind.barricade => 9,
      TargetKind.trafficCar => 10,
      TargetKind.truck => 16,
      TargetKind.bus => 15,
      TargetKind.shop => 12,
    };
  }

  int get points {
    return switch (kind) {
      TargetKind.crate => 180,
      TargetKind.redBarrel => 260,
      TargetKind.steelBarrel => 220,
      TargetKind.cone => 90,
      TargetKind.barricade => 340,
      TargetKind.trafficCar => 520,
      TargetKind.truck => 860,
      TargetKind.bus => 780,
      TargetKind.shop => 680,
    };
  }

  double get mass {
    return switch (kind) {
      TargetKind.crate => 0.7,
      TargetKind.redBarrel => 0.9,
      TargetKind.steelBarrel => 1.0,
      TargetKind.cone => 0.4,
      TargetKind.barricade => 1.15,
      TargetKind.trafficCar => 1.45,
      TargetKind.truck => 2.3,
      TargetKind.bus => 2.15,
      TargetKind.shop => 1.9,
    };
  }

  double get slowMotionSeconds {
    return switch (kind) {
      TargetKind.cone => 0.3,
      TargetKind.crate ||
      TargetKind.redBarrel ||
      TargetKind.steelBarrel => 0.58,
      TargetKind.barricade => 0.72,
      TargetKind.trafficCar => 1.32,
      TargetKind.truck || TargetKind.bus => 1.58,
      TargetKind.shop => 1.36,
    };
  }

  String get impactLabel {
    return switch (kind) {
      TargetKind.trafficCar => 'CAR SHATTER',
      TargetKind.truck => 'TRUCK CRASH',
      TargetKind.bus => 'BUS IMPACT',
      TargetKind.shop => 'SHOP SMASH',
      TargetKind.barricade => 'BARRICADE BREAK',
      TargetKind.redBarrel => 'BARREL BURST',
      TargetKind.crate => 'CRATE SMASH',
      TargetKind.steelBarrel => 'STEEL HIT',
      TargetKind.cone => 'CONE CLIP',
    };
  }

  bool get isExplosive => kind == TargetKind.redBarrel;

  bool get isVehicle =>
      kind == TargetKind.trafficCar ||
      kind == TargetKind.truck ||
      kind == TargetKind.bus;

  bool get isHeavy => isVehicle || kind == TargetKind.shop;

  @override
  Future<void> onLoad() async {
    final base = switch (kind) {
      TargetKind.crate => Vector2(64, 64),
      TargetKind.redBarrel => Vector2(48, 62),
      TargetKind.steelBarrel => Vector2(48, 62),
      TargetKind.cone => Vector2(42, 52),
      TargetKind.barricade => Vector2(98, 70),
      TargetKind.trafficCar => Vector2(62, 134),
      TargetKind.truck => Vector2(88, 156),
      TargetKind.bus => Vector2(84, 164),
      TargetKind.shop => Vector2(126, 82),
    };
    size = base * (0.92 + crashGame._random.nextDouble() * 0.18);
    if (reverseFacing) {
      angle = pi;
    } else if (kind == TargetKind.shop || kind == TargetKind.barricade) {
      angle = (crashGame._random.nextDouble() - 0.5) * 0.14;
    } else {
      angle = (crashGame._random.nextDouble() - 0.5) * 0.1;
    }
    add(
      RectangleHitbox.relative(
        kind == TargetKind.shop ? Vector2(0.82, 0.72) : Vector2(0.72, 0.72),
        parentSize: size,
        anchor: Anchor.center,
      ),
    );
  }

  @override
  void update(double dt) {
    super.update(dt);
    y += crashGame.effectiveRoadSpeed * roadSpeedFactor * dt;
    x += lateralVelocity * dt;
    if (kind != TargetKind.shop) {
      angle += lateralVelocity * 0.00045;
    }
    final bounds = crashGame._roadBounds();
    x = x.clamp(bounds.left + 38, bounds.right - 38).toDouble();
    if (y > crashGame.size.y + 150) {
      removeFromParent();
    }
  }
}

class WreckedVehicleComponent extends SpriteComponent
    with HasGameReference<CrashCarGame> {
  WreckedVehicleComponent({
    required super.sprite,
    required super.position,
    required super.size,
    required super.angle,
    required this.crushSide,
    required this.velocity,
    required this.spin,
    required double life,
  }) : totalLife = life,
       remainingLife = life,
       super(anchor: Anchor.center, priority: 24);

  final double crushSide;
  Vector2 velocity;
  final double spin;
  final double totalLife;
  double remainingLife;

  @override
  void render(Canvas canvas) {
    super.render(canvas);
    final w = size.x;
    final h = size.y;
    final sideStart = crushSide < 0 ? w * 0.02 : w * 0.62;
    final sideEnd = crushSide < 0 ? w * 0.38 : w * 0.98;

    final crumplePaint = Paint()..color = Colors.black.withValues(alpha: 0.62);
    final metalPaint = Paint()
      ..color = const Color(0xFF9EA4A5).withValues(alpha: 0.72);
    final glassPaint = Paint()
      ..color = const Color(0xFFBEEBFF).withValues(alpha: 0.78)
      ..style = PaintingStyle.stroke
      ..strokeWidth = max(1.1, w * 0.022);

    final noseDent = Path()
      ..moveTo(w * 0.22, h * 0.02)
      ..lineTo(w * 0.78, h * 0.04)
      ..lineTo(w * 0.66, h * 0.18)
      ..lineTo(w * 0.47, h * 0.12)
      ..lineTo(w * 0.31, h * 0.21)
      ..close();
    canvas.drawPath(noseDent, crumplePaint);

    final sideTear = Path()
      ..moveTo(sideStart, h * 0.28)
      ..lineTo(sideEnd, h * 0.34)
      ..lineTo(sideEnd - crushSide * w * 0.10, h * 0.55)
      ..lineTo(sideStart + crushSide * w * 0.08, h * 0.49)
      ..close();
    canvas.drawPath(sideTear, crumplePaint);

    final exposedPanel = Path()
      ..moveTo(w * 0.32, h * 0.38)
      ..lineTo(w * 0.58, h * 0.42)
      ..lineTo(w * 0.50, h * 0.57)
      ..lineTo(w * 0.25, h * 0.52)
      ..close();
    canvas.drawPath(exposedPanel, metalPaint);

    for (final crack in [
      [Offset(w * 0.28, h * 0.31), Offset(w * 0.46, h * 0.41)],
      [Offset(w * 0.48, h * 0.31), Offset(w * 0.35, h * 0.49)],
      [Offset(w * 0.56, h * 0.60), Offset(w * 0.40, h * 0.74)],
      [Offset(sideStart, h * 0.68), Offset(sideEnd, h * 0.78)],
    ]) {
      canvas.drawLine(crack[0], crack[1], glassPaint);
    }
  }

  @override
  void update(double dt) {
    super.update(dt);
    final motionDt = dt * game._slowFactor;
    position += velocity * motionDt;
    velocity.x *= pow(0.16, motionDt).toDouble();
    velocity.y += 135 * motionDt;
    angle += spin * motionDt;

    remainingLife -= dt;
    if (remainingLife < totalLife * 0.36) {
      opacity = (remainingLife / (totalLife * 0.36)).clamp(0, 1).toDouble();
    }

    if (remainingLife <= 0 ||
        y > game.size.y + 220 ||
        x < -180 ||
        x > game.size.x + 180) {
      removeFromParent();
    }
  }
}

class DebrisParticle extends SpriteComponent
    with HasGameReference<CrashCarGame> {
  DebrisParticle({
    required super.sprite,
    required super.position,
    required this.velocity,
    required this.spin,
    required Vector2 size,
    required this.life,
  }) : super(anchor: Anchor.center, size: size, priority: 25);

  Vector2 velocity;
  final double spin;
  double life;

  @override
  void update(double dt) {
    super.update(dt);
    final motionDt = dt * game._slowFactor;
    position += velocity * motionDt;
    velocity.y += 360 * motionDt;
    angle += spin * motionDt;
    life -= dt;
    opacity = life.clamp(0, 1).toDouble();
    if (life <= 0 || y > game.size.y + 80) {
      removeFromParent();
    }
  }
}
