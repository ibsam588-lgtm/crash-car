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

  final Random _random = Random();
  late PlayerCar _player;
  late final Sprite _crateSprite;
  late final Sprite _redBarrelSprite;
  late final Sprite _steelBarrelSprite;
  late final Sprite _coneSprite;
  late final Sprite _barricadeSprite;
  late final List<Sprite> _trafficCarSprites;
  late final Sprite _boxTruckSprite;
  late final Sprite _deliveryTruckSprite;
  late final Sprite _cityBusSprite;
  late final List<Sprite> _shopSprites;
  late final List<Sprite> _debrisSprites;

  double _breakableTimer = 0;
  double _trafficTimer = 0.35;
  double _sceneryTimer = 0.8;
  double _elapsed = 0;
  double _roadScroll = 0;
  double _steering = 0;
  bool _boosting = false;
  bool _finished = false;
  int _bestCombo = 1;
  double _comboTimer = 0;
  double _slowMotionTimer = 0;
  double _impactTextTimer = 0;

  double get _carBoost => upgradeLevel * 0.04;

  double get _baseRoadSpeed =>
      280 +
      (car.speed + _carBoost + arena.speedBonus) * 250 +
      (_boosting ? 130 : 0);

  double get _slowFactor => _slowMotionTimer > 0 ? 0.36 : 1;

  double get effectiveRoadSpeed => _baseRoadSpeed * _slowFactor;

  double get _steerSpeed => 290 + car.handling * 260 + upgradeLevel * 18;

  double get _damageMultiplier =>
      max(0.45, 1.06 - car.durability * 0.42 - upgradeLevel * 0.035);

  double get _levelLength => 70;

  @override
  Color backgroundColor() => const Color(0xFF071014);

  @override
  Future<void> onLoad() async {
    await images.loadAll([
      _assetKey(car.asset),
      'cars/realistic_interceptor_blue.png',
      'cars/realistic_rally_green.png',
      'cars/realistic_stunt_red.png',
      'ui/road_lane.png',
      'obstacles/crate.png',
      'obstacles/red_barrel.png',
      'obstacles/steel_barrel.png',
      'obstacles/cone.png',
      'obstacles/barricade.png',
      'traffic/box_truck_red.png',
      'traffic/delivery_truck_blue.png',
      'traffic/city_bus.png',
      'traffic/corner_shop.png',
      'traffic/repair_shop.png',
      'traffic/market_stall.png',
      'debris/wood_1.png',
      'debris/wood_2.png',
      'debris/metal_1.png',
      'debris/glass_1.png',
    ]);

    _crateSprite = Sprite(images.fromCache('obstacles/crate.png'));
    _redBarrelSprite = Sprite(images.fromCache('obstacles/red_barrel.png'));
    _steelBarrelSprite = Sprite(images.fromCache('obstacles/steel_barrel.png'));
    _coneSprite = Sprite(images.fromCache('obstacles/cone.png'));
    _barricadeSprite = Sprite(images.fromCache('obstacles/barricade.png'));
    _trafficCarSprites = [
      Sprite(images.fromCache('cars/realistic_interceptor_blue.png')),
      Sprite(images.fromCache('cars/realistic_rally_green.png')),
      Sprite(images.fromCache('cars/realistic_stunt_red.png')),
    ];
    _boxTruckSprite = Sprite(images.fromCache('traffic/box_truck_red.png'));
    _deliveryTruckSprite = Sprite(
      images.fromCache('traffic/delivery_truck_blue.png'),
    );
    _cityBusSprite = Sprite(images.fromCache('traffic/city_bus.png'));
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

    _elapsed += dt;
    _roadScroll = (_roadScroll + effectiveRoadSpeed * dt) % max(1, size.y);
    _breakableTimer -= dt;
    _trafficTimer -= dt;
    _sceneryTimer -= dt;
    _comboTimer -= dt;
    _slowMotionTimer -= dt;
    _impactTextTimer -= dt;

    if (_slowMotionTimer <= 0 && slowMotion.value) {
      slowMotion.value = false;
    }
    if (_impactTextTimer <= 0 && impactText.value.isNotEmpty) {
      impactText.value = '';
    }
    if (_comboTimer <= 0 && combo.value != 1) {
      combo.value = 1;
    }

    final nextX = (_player.x + _steering * _steerSpeed * dt)
        .clamp(54, size.x - 54)
        .toDouble();
    _player.x = nextX;
    _player.angle = _steering * 0.08;

    final speed = (_baseRoadSpeed * 0.36).round().clamp(0, 260);
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

  void setBoosting(bool value) {
    _boosting = value;
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
    target.hit = true;
    target.removeFromParent();

    final activeCombo = (_comboTimer > 0 ? combo.value + 1 : 1).clamp(1, 12);
    combo.value = activeCombo;
    _bestCombo = max(_bestCombo, activeCombo);
    _comboTimer = 1.45;

    objectsHit.value += 1;
    final damageGain = (target.damage * _damageMultiplier).round().clamp(3, 34);
    damage.value = (damage.value + damageGain).clamp(0, 100);

    final speedBonus = (speedKmh.value * target.mass * 2.4).round();
    final points =
        ((target.points + speedBonus) *
                arena.scoreBonus *
                (1 + activeCombo * 0.18))
            .round();
    score.value += points;
    coins.value += max(1, points ~/ 90);

    _triggerSlowMotion(target);
    _burst(target.position.clone(), target.isExplosive || target.isHeavy);

    if (damage.value >= 100) {
      _finish(levelComplete: false);
    }
  }

  void _triggerSlowMotion(ImpactTargetComponent target) {
    _slowMotionTimer = max(_slowMotionTimer, target.slowMotionSeconds);
    slowMotion.value = true;
    impactText.value = target.impactLabel;
    _impactTextTimer = max(_impactTextTimer, target.slowMotionSeconds + 0.35);
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

    final truckChance = switch (arena.id) {
      'industrial_docks' => 0.52,
      'downtown_strip' => 0.34,
      _ => 0.24,
    };
    final busChance = arena.id == 'downtown_strip' ? 0.18 : 0.07;

    if (roll < truckChance) {
      kind = TargetKind.truck;
      sprite = _random.nextBool() ? _boxTruckSprite : _deliveryTruckSprite;
    } else if (roll < truckChance + busChance) {
      kind = TargetKind.bus;
      sprite = _cityBusSprite;
    } else {
      kind = TargetKind.trafficCar;
      sprite = _trafficCarSprites[_random.nextInt(_trafficCarSprites.length)];
    }

    final oncoming = _random.nextDouble() < 0.34;
    add(
      ImpactTargetComponent(
        kind: kind,
        sprite: sprite,
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
        _trafficCarSprites[_random.nextInt(_trafficCarSprites.length)],
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

  void _burst(Vector2 at, bool heavy) {
    final count = heavy ? 22 : 12;
    for (var i = 0; i < count; i++) {
      final angle = -pi / 2 + (_random.nextDouble() - 0.5) * 2.6;
      final speed = 90 + _random.nextDouble() * (heavy ? 390 : 240);
      add(
        DebrisParticle(
          sprite: _debrisSprites[_random.nextInt(_debrisSprites.length)],
          position:
              at +
              Vector2(
                (_random.nextDouble() - 0.5) * 50,
                (_random.nextDouble() - 0.5) * 54,
              ),
          velocity:
              Vector2(cos(angle), sin(angle)) * speed +
              Vector2(0, effectiveRoadSpeed * 0.52),
          spin: (_random.nextDouble() - 0.5) * 10,
          scale: heavy
              ? 0.48 + _random.nextDouble() * 0.55
              : 0.35 + _random.nextDouble() * 0.34,
        ),
      );
    }
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

class ImpactTargetComponent extends SpriteComponent with CollisionCallbacks {
  ImpactTargetComponent({
    required this.kind,
    required super.sprite,
    required this.crashGame,
    required super.position,
    required this.roadSpeedFactor,
    required this.lateralVelocity,
    this.reverseFacing = false,
  }) : super(anchor: Anchor.center, priority: 10);

  final TargetKind kind;
  final CrashCarGame crashGame;
  final double roadSpeedFactor;
  final double lateralVelocity;
  final bool reverseFacing;
  bool hit = false;

  int get damage {
    return switch (kind) {
      TargetKind.crate => 9,
      TargetKind.redBarrel => 15,
      TargetKind.steelBarrel => 12,
      TargetKind.cone => 5,
      TargetKind.barricade => 18,
      TargetKind.trafficCar => 20,
      TargetKind.truck => 30,
      TargetKind.bus => 28,
      TargetKind.shop => 24,
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
      TargetKind.trafficCar => 1.08,
      TargetKind.truck || TargetKind.bus || TargetKind.shop => 1.36,
    };
  }

  String get impactLabel {
    return switch (kind) {
      TargetKind.trafficCar => 'TRAFFIC HIT',
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

  bool get isHeavy =>
      kind == TargetKind.truck ||
      kind == TargetKind.bus ||
      kind == TargetKind.shop ||
      kind == TargetKind.trafficCar;

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

class DebrisParticle extends SpriteComponent
    with HasGameReference<CrashCarGame> {
  DebrisParticle({
    required super.sprite,
    required super.position,
    required this.velocity,
    required this.spin,
    required double scale,
  }) : super(
         anchor: Anchor.center,
         size: Vector2.all(34 * scale),
         priority: 25,
       );

  Vector2 velocity;
  final double spin;
  double _life = 1.15;

  @override
  void update(double dt) {
    super.update(dt);
    position += velocity * dt;
    velocity.y += 360 * dt;
    angle += spin * dt;
    _life -= dt;
    opacity = _life.clamp(0, 1).toDouble();
    if (_life <= 0 || y > game.size.y + 80) {
      removeFromParent();
    }
  }
}
