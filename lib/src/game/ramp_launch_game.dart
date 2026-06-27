import 'dart:math';

import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:flame/collisions.dart';
import 'package:flame/components.dart';
import 'package:flame/events.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';

class RampLaunchGame extends FlameGame with HasCollisionDetection, PanDetector {
  RampLaunchGame({
    required this.car,
    required this.arena,
    required this.upgradeLevel,
    required this.chargeSeconds,
    required this.onFinished,
  });

  final CarSpec car;
  final ArenaSpec arena;
  final int upgradeLevel;
  final int chargeSeconds;
  final void Function(GameResult result) onFinished;

  final score = ValueNotifier<int>(0);
  final coins = ValueNotifier<int>(0);
  final damage = ValueNotifier<int>(0);
  final objectsHit = ValueNotifier<int>(0);
  final speedKmh = ValueNotifier<int>(0);
  final combo = ValueNotifier<int>(1);
  final chargeProgress = ValueNotifier<double>(0);
  final levelProgress = ValueNotifier<double>(0);
  final slowMotion = ValueNotifier<bool>(false);
  final phaseText = ValueNotifier<String>('Ramp Charge');
  final impactText = ValueNotifier<String>('');
  final scorePopupText = ValueNotifier<String>('');

  final Random _random = Random();
  late RampPlayerCar _player;
  late final Sprite _rampSprite;
  late final Sprite _citySprite;
  late final Sprite _barricadeSprite;
  late final List<RampVehicleVisual> _trafficCarVisuals;
  late final List<RampVehicleVisual> _truckVisuals;
  late final RampVehicleVisual _busVisual;
  late final List<Sprite> _shopSprites;
  late final List<Sprite> _debrisSprites;
  late final List<Sprite> _carFragmentSprites;
  late final List<Sprite> _glassShardSprites;
  late final List<Sprite> _metalShardSprites;

  RampPhase _phase = RampPhase.charge;
  bool _accelerating = false;
  bool _finished = false;
  double _steering = 0;
  double _roadScroll = 0;
  double _elapsed = 0;
  double _impactElapsed = 0;
  double _currentRoadSpeed = 0;
  double _trafficTimer = 0.2;
  double _sceneryTimer = 0.58;
  double _comboTimer = 0;
  double _slowMotionTimer = 0;
  double _impactTextTimer = 0;
  double _scorePopupTimer = 0;
  double _playerImpactTimer = 0;
  double _playerImpactDuration = 0.001;
  double _playerImpactLean = 0;
  double _playerImpactDrift = 0;
  double _playerSpinRate = 0;
  int _bestCombo = 1;

  double get _carBoost => upgradeLevel * 0.045;

  double get _speedTune => car.speed + _carBoost + arena.speedBonus;

  double get _maxRoadSpeed => 560 + _speedTune * 280;

  double get _impactMinSpeed => 345 + _speedTune * 105;

  double get _slowFactor => _slowMotionTimer > 0 ? 0.28 : 1;

  double get effectiveRoadSpeed => _currentRoadSpeed * _slowFactor;

  double get _steerSpeed => 280 + car.handling * 265 + upgradeLevel * 18;

  double get _damageMultiplier =>
      max(0.45, 1.04 - car.durability * 0.4 - upgradeLevel * 0.035);

  double get _impactDuration => 42;

  bool get isCharging => _phase == RampPhase.charge;

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
      'ui/ramp_lane.png',
      'ui/city_intersection.png',
      'obstacles/barricade.png',
      'cars/realistic_interceptor_blue.png',
      'cars/wrecked_realistic_interceptor_blue.png',
      'cars/realistic_rally_green.png',
      'cars/wrecked_realistic_rally_green.png',
      'cars/realistic_stunt_red.png',
      'cars/wrecked_realistic_stunt_red.png',
      'traffic/realistic_sedan_blue.png',
      'traffic/wrecked_realistic_sedan_blue.png',
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

    _rampSprite = Sprite(images.fromCache('ui/ramp_lane.png'));
    _citySprite = Sprite(images.fromCache('ui/city_intersection.png'));
    _barricadeSprite = Sprite(images.fromCache('obstacles/barricade.png'));
    _trafficCarVisuals = [
      RampVehicleVisual(
        sprite: Sprite(images.fromCache('traffic/realistic_sedan_blue.png')),
        wreckSprite: Sprite(
          images.fromCache('traffic/wrecked_realistic_sedan_blue.png'),
        ),
      ),
      RampVehicleVisual(
        sprite: Sprite(images.fromCache('cars/realistic_interceptor_blue.png')),
        wreckSprite: Sprite(
          images.fromCache('cars/wrecked_realistic_interceptor_blue.png'),
        ),
      ),
      RampVehicleVisual(
        sprite: Sprite(images.fromCache('cars/realistic_rally_green.png')),
        wreckSprite: Sprite(
          images.fromCache('cars/wrecked_realistic_rally_green.png'),
        ),
      ),
      RampVehicleVisual(
        sprite: Sprite(images.fromCache('cars/realistic_stunt_red.png')),
        wreckSprite: Sprite(
          images.fromCache('cars/wrecked_realistic_stunt_red.png'),
        ),
      ),
    ];
    _truckVisuals = [
      RampVehicleVisual(
        sprite: Sprite(images.fromCache('traffic/box_truck_red.png')),
        wreckSprite: Sprite(
          images.fromCache('traffic/wrecked_box_truck_red.png'),
        ),
      ),
      RampVehicleVisual(
        sprite: Sprite(images.fromCache('traffic/delivery_truck_blue.png')),
        wreckSprite: Sprite(
          images.fromCache('traffic/wrecked_delivery_truck_blue.png'),
        ),
      ),
    ];
    _busVisual = RampVehicleVisual(
      sprite: Sprite(images.fromCache('traffic/city_bus.png')),
      wreckSprite: Sprite(images.fromCache('traffic/wrecked_city_bus.png')),
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

    add(RampRoadLayer(rampSprite: _rampSprite, citySprite: _citySprite));
    _player = RampPlayerCar(
      sprite: Sprite(images.fromCache(_assetKey(car.asset))),
      game: this,
    );
    add(_player);
  }

  @override
  void onGameResize(Vector2 size) {
    super.onGameResize(size);
    if (isLoaded && _player.isMounted) {
      _player.position = Vector2(size.x / 2, size.y - 174);
      _player.size = Vector2(86, 186);
    }
  }

  @override
  void update(double dt) {
    super.update(dt);
    if (_finished || size.x <= 0 || size.y <= 0) {
      return;
    }

    _elapsed += dt;
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

    if (_phase == RampPhase.charge) {
      _updateCharge(dt);
    } else {
      _updateImpactRun(dt);
    }

    _roadScroll = (_roadScroll + effectiveRoadSpeed * dt) % max(1, size.y);
    _updatePlayerMotion(dt);

    final speed = (_currentRoadSpeed * 0.36).round().clamp(0, 325);
    if (speedKmh.value != speed) {
      speedKmh.value = speed;
    }
  }

  @override
  void render(Canvas canvas) {
    super.render(canvas);
    if (_phase == RampPhase.impact) {
      final paint = Paint()..color = arena.primary.withValues(alpha: 0.08);
      canvas.drawRect(Rect.fromLTWH(0, 0, size.x, size.y * 0.2), paint);
    }
  }

  @override
  void onPanUpdate(DragUpdateInfo info) {
    _player.x = (_player.x + info.delta.global.x)
        .clamp(52, size.x - 52)
        .toDouble();
  }

  void setSteering(double value) {
    _steering = value.clamp(-1, 1).toDouble();
  }

  void setAccelerating(bool value) {
    _accelerating = value;
  }

  void pauseOrResume() {
    paused = !paused;
  }

  void forceFinish() {
    _finish(levelComplete: false);
  }

  void smash(RampImpactTarget target) {
    if (target.hit || _finished || _phase == RampPhase.charge) {
      return;
    }
    target.hit = true;
    final hitPosition = target.position.clone();
    final impactSide = _impactSide(hitPosition);
    final activeCombo = (_comboTimer > 0 ? combo.value + 1 : 1).clamp(1, 14);

    target.removeFromParent();
    combo.value = activeCombo;
    _bestCombo = max(_bestCombo, activeCombo);
    _comboTimer = 1.8;
    objectsHit.value += 1;

    final damageGain = (target.damage * _damageMultiplier).round().clamp(2, 22);
    damage.value = min(100, damage.value + damageGain);
    final speedBonus = (speedKmh.value * target.mass * 3.05).round();
    final points =
        ((target.points + speedBonus) *
                arena.scoreBonus *
                (1 + activeCombo * 0.22))
            .round();
    score.value += points;
    coins.value += max(1, points ~/ 85);
    scorePopupText.value = '+$points';
    _scorePopupTimer = 1.05;

    _triggerSlowMotion(target);
    _applyPlayerCrashMotion(target, impactSide);
    _spawnWreck(target, hitPosition, impactSide);
    _burst(target, hitPosition);
    _currentRoadSpeed = max(
      _impactMinSpeed,
      _currentRoadSpeed - (target.mass * 26 + 18),
    );

    if (target.isVehicle && _random.nextDouble() < 0.65) {
      _spawnChainTarget(impactSide);
    }
  }

  void _updateCharge(double dt) {
    final duration = max(1, chargeSeconds).toDouble();
    final progress = (_elapsed / duration).clamp(0.0, 1.0);
    chargeProgress.value = progress;
    levelProgress.value = progress * 0.18;
    phaseText.value = 'Ramp Charge';
    final gasBoost = _accelerating ? 1.18 : 0.96;
    final target = (_maxRoadSpeed * (0.18 + progress * 0.9) * gasBoost)
        .clamp(155, _maxRoadSpeed)
        .toDouble();
    _currentRoadSpeed += (target - _currentRoadSpeed) * min(1, dt * 2.4);
    if (_elapsed >= duration) {
      _enterImpactArena();
    }
  }

  void _updateImpactRun(double dt) {
    _impactElapsed += dt;
    chargeProgress.value = 1;
    phaseText.value = 'Impact Chain';
    final progress = (_impactElapsed / _impactDuration).clamp(0.0, 1.0);
    levelProgress.value = 0.18 + progress * 0.82;
    final target = _accelerating
        ? _maxRoadSpeed
        : max(_impactMinSpeed, _maxRoadSpeed * 0.82);
    _currentRoadSpeed += (target - _currentRoadSpeed) * min(1, dt * 0.72);

    _trafficTimer -= dt;
    _sceneryTimer -= dt;
    if (_trafficTimer <= 0) {
      _spawnTraffic();
      _trafficTimer = max(
        0.36,
        (0.92 - min(0.22, _impactElapsed * 0.006)) /
            max(0.75, arena.trafficDensity),
      );
    }
    if (_sceneryTimer <= 0) {
      _spawnScenery();
      _sceneryTimer = max(
        0.5,
        (1.18 - min(0.25, _impactElapsed * 0.004)) /
            max(0.75, arena.sceneryDensity),
      );
    }
    if (_impactElapsed >= _impactDuration) {
      _finish(levelComplete: true);
    }
  }

  void _updatePlayerMotion(double dt) {
    var playerX = _player.x;
    if (_playerImpactTimer > 0) {
      final motionDt = dt * _slowFactor;
      playerX += _playerImpactDrift * motionDt;
      _playerImpactDrift *= pow(0.2, motionDt).toDouble();
      _player.angle += _playerSpinRate * motionDt;
    }
    final nextX = (playerX + _steering * _steerSpeed * dt)
        .clamp(52, size.x - 52)
        .toDouble();
    _player.x = nextX;
    if (_playerImpactTimer > 0) {
      final lean =
          _playerImpactLean *
          (_playerImpactTimer / _playerImpactDuration).clamp(0, 1);
      _player.angle += lean * dt;
    } else {
      _player.angle = _steering * 0.08;
    }
  }

  void _enterImpactArena() {
    _phase = RampPhase.impact;
    _elapsed = 0;
    _impactElapsed = 0;
    _roadScroll = 0;
    _currentRoadSpeed = max(_currentRoadSpeed, _maxRoadSpeed * 0.92);
    phaseText.value = 'Impact Chain';
    impactText.value = 'CITY ARENA';
    _impactTextTimer = 1.2;
    slowMotion.value = true;
    _slowMotionTimer = 0.72;
    for (var i = 0; i < 5; i++) {
      _spawnTraffic(initial: true, offset: i * 130);
    }
    for (var i = 0; i < 4; i++) {
      _spawnScenery(initial: true, offset: i * 165);
    }
  }

  void _triggerSlowMotion(RampImpactTarget target) {
    _slowMotionTimer = max(_slowMotionTimer, target.slowMotionSeconds);
    slowMotion.value = true;
    impactText.value = target.impactLabel;
    _impactTextTimer = max(_impactTextTimer, target.slowMotionSeconds + 0.35);
  }

  void _applyPlayerCrashMotion(RampImpactTarget target, double side) {
    _playerImpactDuration = target.isVehicle ? 1.05 : 0.66;
    _playerImpactTimer = _playerImpactDuration;
    _playerImpactLean = side * (target.isVehicle ? 0.22 : 0.14);
    _playerImpactDrift = side * (target.isVehicle ? 170 : 86) * target.mass;
    _playerSpinRate =
        side *
        (target.isVehicle ? 1.18 : 0.58) *
        (0.8 + _random.nextDouble() * 0.52);
  }

  double _impactSide(Vector2 hitPosition) {
    final delta = _player.x - hitPosition.x;
    if (delta.abs() < 8) {
      return _random.nextBool() ? 1 : -1;
    }
    return delta.sign.toDouble();
  }

  void _spawnTraffic({bool initial = false, double offset = 0}) {
    final lanes = _lanePositions();
    final roll = _random.nextDouble();
    late RampTargetKind kind;
    late RampVehicleVisual visual;
    if (roll < (arena.id == 'industrial_docks' ? 0.42 : 0.24)) {
      kind = RampTargetKind.truck;
      visual = _truckVisuals[_random.nextInt(_truckVisuals.length)];
    } else if (roll < (arena.id == 'downtown_strip' ? 0.34 : 0.14)) {
      kind = RampTargetKind.bus;
      visual = _busVisual;
    } else {
      kind = RampTargetKind.car;
      visual = _trafficCarVisuals[_random.nextInt(_trafficCarVisuals.length)];
    }

    final y = initial ? -150.0 - offset : -150.0;
    final oncoming = _random.nextDouble() < 0.56;
    add(
      RampImpactTarget(
        kind: kind,
        sprite: visual.sprite,
        wreckSprite: visual.wreckSprite,
        game: this,
        position: Vector2(lanes[_random.nextInt(lanes.length)], y),
        roadSpeedFactor: oncoming ? 1.1 : 0.48 + _random.nextDouble() * 0.18,
        lateralVelocity:
            (_random.nextDouble() - 0.5) *
            (kind == RampTargetKind.truck ? 35 : 62),
        reverseFacing: !oncoming,
      ),
    );
  }

  void _spawnScenery({bool initial = false, double offset = 0}) {
    final bounds = _roadBounds();
    final shop = _random.nextDouble() < 0.68;
    final right = _random.nextBool();
    final x = right ? bounds.right - 54 : bounds.left + 54;
    add(
      RampImpactTarget(
        kind: shop ? RampTargetKind.shop : RampTargetKind.barricade,
        sprite: shop
            ? _shopSprites[_random.nextInt(_shopSprites.length)]
            : _barricadeSprite,
        game: this,
        position: Vector2(x, initial ? -120 - offset : -120),
        roadSpeedFactor: 1,
        lateralVelocity: right ? -12 : 12,
      ),
    );
  }

  void _spawnChainTarget(double impactSide) {
    final bounds = _roadBounds();
    final x = (_player.x + impactSide * (80 + _random.nextDouble() * 72))
        .clamp(bounds.left + 50, bounds.right - 50)
        .toDouble();
    final visual =
        _trafficCarVisuals[_random.nextInt(_trafficCarVisuals.length)];
    add(
      RampImpactTarget(
        kind: RampTargetKind.car,
        sprite: visual.sprite,
        wreckSprite: visual.wreckSprite,
        game: this,
        position: Vector2(x, _player.y - 220 - _random.nextDouble() * 150),
        roadSpeedFactor: 0.72,
        lateralVelocity: -impactSide * (32 + _random.nextDouble() * 38),
        reverseFacing: _random.nextBool(),
      ),
    );
  }

  void _spawnWreck(
    RampImpactTarget target,
    Vector2 at,
    double playerImpactSide,
  ) {
    if (!target.isVehicle) {
      return;
    }
    final wreckSide = -playerImpactSide;
    final large =
        target.kind == RampTargetKind.truck ||
        target.kind == RampTargetKind.bus;
    add(
      RampWreckedVehicle(
        sprite: target.wreckSprite ?? target.sprite,
        position: at,
        size: target.size.clone(),
        angle: target.angle + wreckSide * 0.22,
        crushSide: wreckSide,
        velocity: Vector2(
          wreckSide *
              (large ? 150 : 245) *
              (0.85 + _random.nextDouble() * 0.45),
          effectiveRoadSpeed * (large ? 0.36 : 0.25) + 58,
        ),
        spin:
            wreckSide *
            (large ? 1.08 : 2.55) *
            (0.8 + _random.nextDouble() * 0.55),
        life: large ? 2.6 : 2.05,
      ),
    );
  }

  void _burst(RampImpactTarget target, Vector2 at) {
    if (target.isVehicle) {
      final bodyCount = target.kind == RampTargetKind.car ? 18 : 28;
      final glassCount = target.kind == RampTargetKind.car ? 13 : 18;
      final metalCount = target.kind == RampTargetKind.car ? 10 : 16;
      for (var i = 0; i < bodyCount; i++) {
        _spawnDebris(
          sprites: _carFragmentSprites,
          at: at,
          minSpeed: 150,
          maxSpeed: target.kind == RampTargetKind.car ? 520 : 680,
          spread: 3.45,
          edge: target.kind == RampTargetKind.car ? 48 : 62,
          life: 1.35,
        );
      }
      for (var i = 0; i < glassCount; i++) {
        _spawnDebris(
          sprites: _glassShardSprites,
          at: at + Vector2((_random.nextDouble() - 0.5) * 36, -20),
          minSpeed: 180,
          maxSpeed: 680,
          spread: 3.9,
          edge: 26,
          life: 1.0,
        );
      }
      for (var i = 0; i < metalCount; i++) {
        _spawnDebris(
          sprites: _metalShardSprites,
          at: at,
          minSpeed: 130,
          maxSpeed: 590,
          spread: 3.25,
          edge: 32,
          life: 1.25,
        );
      }
      return;
    }

    for (var i = 0; i < 12; i++) {
      _spawnDebris(
        sprites: _debrisSprites,
        at: at,
        minSpeed: 90,
        maxSpeed: target.kind == RampTargetKind.shop ? 420 : 330,
        spread: 2.8,
        edge: 36,
      );
    }
    if (target.kind == RampTargetKind.shop) {
      for (var i = 0; i < 8; i++) {
        _spawnDebris(
          sprites: _glassShardSprites,
          at: at,
          minSpeed: 130,
          maxSpeed: 470,
          spread: 3.2,
          edge: 24,
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
      RampDebrisParticle(
        sprite: sprite,
        position:
            at +
            Vector2(
              (_random.nextDouble() - 0.5) * 60,
              (_random.nextDouble() - 0.5) * 60,
            ),
        velocity:
            Vector2(cos(angle), sin(angle)) * speed +
            Vector2(0, effectiveRoadSpeed * 0.48),
        spin: (_random.nextDouble() - 0.5) * 13,
        size: _debrisSize(sprite, edge * (0.78 + _random.nextDouble() * 0.6)),
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

  List<double> _lanePositions() {
    final bounds = _roadBounds();
    return [
      bounds.left + 58,
      bounds.left + bounds.width * 0.35,
      bounds.left + bounds.width * 0.65,
      bounds.right - 58,
    ];
  }

  Rect _roadBounds() {
    final roadWidth = min(size.x, 560.0);
    final left = (size.x - roadWidth) / 2;
    return Rect.fromLTWH(left, 0, roadWidth, size.y);
  }

  void _finish({required bool levelComplete}) {
    if (_finished) {
      return;
    }
    _finished = true;
    onFinished(
      GameResult(
        arenaName: '${arena.name} Ramp',
        score: score.value,
        coins: coins.value + (levelComplete ? 90 : 0),
        damagePercent: damage.value,
        objectsHit: objectsHit.value,
        maxSpeed: speedKmh.value,
        bestCombo: _bestCombo,
        levelComplete: levelComplete,
      ),
    );
  }

  double get roadScroll => _roadScroll;

  RampPhase get phase => _phase;

  String _assetKey(String fullPath) =>
      fullPath.replaceFirst('assets/images/', '');
}

enum RampPhase { charge, impact }

enum RampTargetKind { car, truck, bus, shop, barricade }

class RampVehicleVisual {
  const RampVehicleVisual({required this.sprite, required this.wreckSprite});

  final Sprite sprite;
  final Sprite wreckSprite;
}

class RampRoadLayer extends Component with HasGameReference<RampLaunchGame> {
  RampRoadLayer({required this.rampSprite, required this.citySprite})
    : super(priority: -10);

  final Sprite rampSprite;
  final Sprite citySprite;

  @override
  void render(Canvas canvas) {
    final gameSize = game.size;
    if (gameSize.x <= 0 || gameSize.y <= 0) {
      return;
    }
    final bounds = game._roadBounds();
    final sprite = game.phase == RampPhase.charge ? rampSprite : citySprite;
    final segmentHeight = gameSize.y;
    for (
      var y = -segmentHeight + game.roadScroll;
      y < gameSize.y + segmentHeight;
      y += segmentHeight
    ) {
      sprite.render(
        canvas,
        position: Vector2(bounds.left, y),
        size: Vector2(bounds.width, segmentHeight),
      );
    }

    final sidePaint = Paint()..color = game.arena.sideTint;
    canvas.drawRect(Rect.fromLTWH(0, 0, bounds.left, gameSize.y), sidePaint);
    canvas.drawRect(
      Rect.fromLTWH(bounds.right, 0, bounds.left, gameSize.y),
      sidePaint,
    );
    final tint = Paint()
      ..color = game.arena.roadTint.withValues(
        alpha: game.phase == RampPhase.charge ? 0.08 : 0.16,
      );
    canvas.drawRect(bounds, tint);
    final edgePaint = Paint()
      ..color = game.arena.primary.withValues(alpha: 0.2)
      ..strokeWidth = 3;
    canvas.drawLine(
      Offset(bounds.left + 36, 0),
      Offset(bounds.left + 36, gameSize.y),
      edgePaint,
    );
    canvas.drawLine(
      Offset(bounds.right - 36, 0),
      Offset(bounds.right - 36, gameSize.y),
      edgePaint,
    );
  }
}

class RampPlayerCar extends SpriteComponent with CollisionCallbacks {
  RampPlayerCar({required super.sprite, required this.game})
    : super(anchor: Anchor.center, priority: 20);

  final RampLaunchGame game;

  @override
  Future<void> onLoad() async {
    size = Vector2(86, 186);
    position = Vector2(game.size.x / 2, game.size.y - 174);
    add(RectangleHitbox(size: Vector2(58, 142), position: Vector2(14, 22)));
  }

  @override
  void onCollisionStart(
    Set<Vector2> intersectionPoints,
    PositionComponent other,
  ) {
    super.onCollisionStart(intersectionPoints, other);
    if (other is RampImpactTarget) {
      game.smash(other);
    }
  }
}

class RampImpactTarget extends SpriteComponent with CollisionCallbacks {
  RampImpactTarget({
    required this.kind,
    required super.sprite,
    required this.game,
    required super.position,
    required this.roadSpeedFactor,
    required this.lateralVelocity,
    this.wreckSprite,
    this.reverseFacing = false,
  }) : super(anchor: Anchor.center, priority: 10);

  final RampTargetKind kind;
  final RampLaunchGame game;
  final double roadSpeedFactor;
  final double lateralVelocity;
  final Sprite? wreckSprite;
  final bool reverseFacing;
  bool hit = false;

  int get damage {
    return switch (kind) {
      RampTargetKind.car => 12,
      RampTargetKind.truck => 18,
      RampTargetKind.bus => 17,
      RampTargetKind.shop => 13,
      RampTargetKind.barricade => 9,
    };
  }

  int get points {
    return switch (kind) {
      RampTargetKind.car => 680,
      RampTargetKind.truck => 1080,
      RampTargetKind.bus => 980,
      RampTargetKind.shop => 790,
      RampTargetKind.barricade => 360,
    };
  }

  double get mass {
    return switch (kind) {
      RampTargetKind.car => 1.45,
      RampTargetKind.truck => 2.45,
      RampTargetKind.bus => 2.25,
      RampTargetKind.shop => 1.9,
      RampTargetKind.barricade => 1.1,
    };
  }

  double get slowMotionSeconds {
    return switch (kind) {
      RampTargetKind.car => 1.38,
      RampTargetKind.truck => 1.7,
      RampTargetKind.bus => 1.65,
      RampTargetKind.shop => 1.28,
      RampTargetKind.barricade => 0.72,
    };
  }

  String get impactLabel {
    return switch (kind) {
      RampTargetKind.car => 'SIDE IMPACT',
      RampTargetKind.truck => 'HEAVY TRUCK HIT',
      RampTargetKind.bus => 'BUS CRASH',
      RampTargetKind.shop => 'STORE FRONT SMASH',
      RampTargetKind.barricade => 'BARRIER BREAK',
    };
  }

  bool get isVehicle =>
      kind == RampTargetKind.car ||
      kind == RampTargetKind.truck ||
      kind == RampTargetKind.bus;

  @override
  Future<void> onLoad() async {
    final base = switch (kind) {
      RampTargetKind.car => Vector2(66, 142),
      RampTargetKind.truck => Vector2(92, 172),
      RampTargetKind.bus => Vector2(92, 184),
      RampTargetKind.shop => Vector2(132, 88),
      RampTargetKind.barricade => Vector2(104, 74),
    };
    size = base * (0.94 + game._random.nextDouble() * 0.14);
    if (reverseFacing) {
      angle = pi;
    } else if (!isVehicle) {
      angle = (game._random.nextDouble() - 0.5) * 0.18;
    } else {
      angle = (game._random.nextDouble() - 0.5) * 0.08;
    }
    add(
      RectangleHitbox.relative(
        kind == RampTargetKind.shop ? Vector2(0.82, 0.74) : Vector2(0.72, 0.74),
        parentSize: size,
        anchor: Anchor.center,
      ),
    );
  }

  @override
  void update(double dt) {
    super.update(dt);
    final motionDt = dt * game._slowFactor;
    y += game.effectiveRoadSpeed * roadSpeedFactor * dt;
    x += lateralVelocity * motionDt;
    if (isVehicle) {
      angle += lateralVelocity * 0.0005 * game._slowFactor;
    }
    final bounds = game._roadBounds();
    x = x.clamp(bounds.left + 42, bounds.right - 42).toDouble();
    if (y > game.size.y + 170) {
      removeFromParent();
    }
  }
}

class RampWreckedVehicle extends SpriteComponent
    with HasGameReference<RampLaunchGame> {
  RampWreckedVehicle({
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
    final shadowPaint = Paint()..color = Colors.black.withValues(alpha: 0.62);
    final exposedPaint = Paint()
      ..color = const Color(0xFFC2C8C7).withValues(alpha: 0.76);
    final glassPaint = Paint()
      ..color = const Color(0xFFBEEBFF).withValues(alpha: 0.78)
      ..style = PaintingStyle.stroke
      ..strokeWidth = max(1.1, w * 0.022);

    final noseCrush = Path()
      ..moveTo(w * 0.2, h * 0.02)
      ..lineTo(w * 0.8, h * 0.04)
      ..lineTo(w * 0.68, h * 0.2)
      ..lineTo(w * 0.49, h * 0.13)
      ..lineTo(w * 0.3, h * 0.22)
      ..close();
    canvas.drawPath(noseCrush, shadowPaint);

    final sideTear = Path()
      ..moveTo(sideStart, h * 0.28)
      ..lineTo(sideEnd, h * 0.34)
      ..lineTo(sideEnd - crushSide * w * 0.1, h * 0.57)
      ..lineTo(sideStart + crushSide * w * 0.08, h * 0.49)
      ..close();
    canvas.drawPath(sideTear, shadowPaint);
    canvas.drawOval(
      Rect.fromCenter(
        center: Offset(w * 0.5, h * 0.47),
        width: w * 0.35,
        height: h * 0.18,
      ),
      exposedPaint,
    );
    for (final crack in [
      [Offset(w * 0.28, h * 0.31), Offset(w * 0.46, h * 0.41)],
      [Offset(w * 0.48, h * 0.31), Offset(w * 0.35, h * 0.49)],
      [Offset(w * 0.56, h * 0.6), Offset(w * 0.4, h * 0.74)],
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
    if (remainingLife < totalLife * 0.38) {
      opacity = (remainingLife / (totalLife * 0.38)).clamp(0, 1).toDouble();
    }
    if (remainingLife <= 0 ||
        y > game.size.y + 230 ||
        x < -190 ||
        x > game.size.x + 190) {
      removeFromParent();
    }
  }
}

class RampDebrisParticle extends SpriteComponent
    with HasGameReference<RampLaunchGame> {
  RampDebrisParticle({
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
    if (life <= 0 || y > game.size.y + 90) {
      removeFromParent();
    }
  }
}
