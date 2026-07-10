import 'dart:math';
import 'dart:ui' as ui;

import 'package:crash_car/src/game/procedural_vehicle.dart';
import 'package:crash_car/src/models/arena_spec.dart';
import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:flame/components.dart';
import 'package:flame/events.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';

class ImpactRunGame extends FlameGame with PanDetector {
  ImpactRunGame({
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
  final crashes = ValueNotifier<int>(0);
  final remainingSeconds = ValueNotifier<int>(75);
  final levelProgress = ValueNotifier<double>(0);
  final boost = ValueNotifier<double>(0);
  final slowMotion = ValueNotifier<bool>(false);
  final impactText = ValueNotifier<String>('');
  final stuntText = ValueNotifier<String>('');
  final scorePopupText = ValueNotifier<String>('');

  static const double _levelLength = 75;

  final Random _random = Random();
  final List<_ChaseTarget> _targets = [];
  final List<_ChaseWreck> _wrecks = [];
  final List<_ShardParticle> _particles = [];

  late final List<Sprite> _debrisSprites;

  double _elapsed = 0;
  double _roadPulse = 0;
  double _wheelSpin = 0;
  double _worldDistance = 0;
  double _targetSpawnTimer = 1.05;
  double _shopSpawnTimer = 2.4;
  double _playerLane = 0;
  double _cameraLane = 0;
  double _steering = 0;
  double _currentSpeed = 74;
  double _boostMeter = 0;
  double _comboTimer = 0;
  double _slowMotionTimer = 0;
  double _impactTextTimer = 0;
  double _stuntTextTimer = 0;
  double _scorePopupTimer = 0;
  double _screenShake = 0;
  double _playerCrashTimer = 0;
  double _playerCrashDuration = 0.001;
  double _playerSpinAngle = 0;
  double _playerSpinVelocity = 0;
  double _playerKickLane = 0;
  double _playerDamage = 0;
  double _impactCooldown = 0;
  double _launchGraceTimer = 1.8;
  double _oncomingTick = 0;
  double _driftTick = 0;
  double _airCooldown = 7.5;
  bool _accelerating = false;
  bool _finished = false;
  int _bestCombo = 1;

  double get _carBoost => upgradeLevel * 0.035;

  double get _maxSpeed =>
      218 + car.speed * 96 + arena.speedBonus * 220 + _carBoost * 130;

  double get _cruiseSpeed => 84 + car.speed * 34 + arena.speedBonus * 54;

  double get _steerSpeed => 1.2 + car.handling * 1.05 + upgradeLevel * 0.03;

  double get _slowFactor => _slowMotionTimer > 0 ? 0.52 : 1;

  bool get _isCrashing => _playerCrashTimer > 0;

  @override
  Color backgroundColor() => const Color(0xFF081116);

  @override
  Future<void> onLoad() async {
    final debrisKeys = List.generate(
      20,
      (index) =>
          'debris/car_fragment_${(index + 1).toString().padLeft(2, '0')}.png',
    );
    await images.loadAll(debrisKeys);

    _debrisSprites = [
      for (final key in debrisKeys) Sprite(images.fromCache(key)),
    ];

    for (var i = 0; i < 5; i++) {
      _spawnTraffic(initialDepth: 0.06 + i * 0.1, avoidCenter: true);
    }
  }

  @override
  void update(double dt) {
    super.update(dt);
    if (_finished || size.x <= 0 || size.y <= 0) {
      return;
    }

    final frameDt = dt.clamp(0.0, 1 / 30).toDouble();
    _elapsed += frameDt;
    _comboTimer -= frameDt;
    _slowMotionTimer -= frameDt;
    _impactTextTimer -= frameDt;
    _stuntTextTimer -= frameDt;
    _scorePopupTimer -= frameDt;
    _screenShake = max(0, _screenShake - frameDt * 1.9);
    _playerCrashTimer -= frameDt;
    _impactCooldown -= frameDt;
    _launchGraceTimer -= frameDt;
    _oncomingTick -= frameDt;
    _driftTick -= frameDt;
    _airCooldown -= frameDt;

    if (_slowMotionTimer <= 0 && slowMotion.value) {
      slowMotion.value = false;
    }
    if (_impactTextTimer <= 0 && impactText.value.isNotEmpty) {
      impactText.value = '';
    }
    if (_stuntTextTimer <= 0 && stuntText.value.isNotEmpty) {
      stuntText.value = '';
    }
    if (_scorePopupTimer <= 0 && scorePopupText.value.isNotEmpty) {
      scorePopupText.value = '';
    }
    if (_comboTimer <= 0 && combo.value != 1) {
      combo.value = 1;
    }
    if (!_isCrashing && _playerSpinAngle != 0) {
      _playerSpinAngle *= pow(0.02, frameDt).toDouble();
      if (_playerSpinAngle.abs() < 0.01) {
        _playerSpinAngle = 0;
      }
    }

    final motionDt = frameDt * _slowFactor;
    _updateSpeed(frameDt);
    _worldDistance += _currentSpeed * motionDt;
    _roadPulse = (_roadPulse + _currentSpeed * motionDt * 0.018) % 1;
    _wheelSpin = (_wheelSpin + _currentSpeed * motionDt * 0.095) % (pi * 2);
    _cameraLane += (_playerLane - _cameraLane) * min(1, frameDt * 2.9);
    _updatePlayer(frameDt, motionDt);
    _updateSpawns(frameDt);
    _updateTargets(motionDt);
    _updateParticles(motionDt, frameDt);
    _updateStunts(frameDt);
    _syncNotifiers();

    if (_elapsed >= _levelLength) {
      _finish(levelComplete: true);
    }
  }

  @override
  void render(Canvas canvas) {
    super.render(canvas);
    final shake = _screenShake * (2.8 + _currentSpeed / 95);
    final shakeX = sin(_elapsed * 55.0) * shake;
    final shakeY = cos(_elapsed * 47.0) * shake * 0.48;

    canvas.save();
    canvas.translate(shakeX, shakeY);
    try {
      _drawBackdrop(canvas);
      _drawRoad(canvas);
      _drawAmbientTraffic(canvas);
      _drawRoadsideTargets(canvas);
      _drawTraffic(canvas);
      _drawWrecks(canvas);
      _drawParticles(canvas);
      _drawPlayer(canvas);
      _drawSlowMotionTint(canvas);
    } catch (_) {
      try {
        _drawReliableImpactScene(canvas);
      } catch (_) {
        _drawEmergencyImpactScene(canvas);
      }
    }
    try {
      _drawReliableImpactScene(canvas, overlay: true);
    } catch (_) {
      _drawEmergencyImpactScene(canvas);
    }
    canvas.restore();
  }

  @override
  void onPanUpdate(DragUpdateInfo info) {
    final roadWidth = min(size.x * 1.04, 720);
    _playerLane = (_playerLane + info.delta.global.x / max(260, roadWidth))
        .clamp(-1.18, 1.18)
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

  void _updateSpeed(double dt) {
    final boostAssist = _accelerating && _boostMeter > 0.04 ? 35 : 0;
    final crashDrag = _isCrashing ? 38 : 0;
    final target =
        (_accelerating ? _maxSpeed + boostAssist : _cruiseSpeed) - crashDrag;
    final response = _accelerating ? 1.9 : 1.15;
    _currentSpeed += (target - _currentSpeed) * min(1, dt * response);
    _currentSpeed = _currentSpeed.clamp(82, _maxSpeed + 44).toDouble();

    if (_accelerating && _boostMeter > 0) {
      _boostMeter = max(0, _boostMeter - dt * 0.105);
    }
  }

  void _updatePlayer(double dt, double motionDt) {
    if (_isCrashing) {
      final crashProgress = (_playerCrashTimer / _playerCrashDuration).clamp(
        0.0,
        1.0,
      );
      _playerSpinAngle = (_playerSpinAngle + _playerSpinVelocity * motionDt)
          .clamp(-1.15, 1.15)
          .toDouble();
      _playerLane += _playerKickLane * crashProgress * motionDt;
      _playerKickLane *= pow(0.1, motionDt).toDouble();
    }

    final steeringDamping = _isCrashing ? 0.34 : 1.0;
    _playerLane += _steering * _steerSpeed * dt * steeringDamping;
    _playerLane = _playerLane.clamp(-1.18, 1.18).toDouble();
  }

  void _updateSpawns(double dt) {
    _targetSpawnTimer -= dt;
    _shopSpawnTimer -= dt;
    if (_targetSpawnTimer <= 0) {
      _spawnTraffic();
      final density = max(0.65, arena.trafficDensity);
      final speedPressure = (_currentSpeed / _maxSpeed).clamp(0.45, 1.0);
      _targetSpawnTimer =
          max(0.62, 1.12 / (density * speedPressure) - _elapsed * 0.0012);
    }
    if (_shopSpawnTimer <= 0) {
      _spawnRoadsideShop();
      final density = max(0.7, arena.sceneryDensity);
      _shopSpawnTimer = max(1.25, 2.45 / density - _elapsed * 0.002);
    }
  }

  void _updateTargets(double motionDt) {
    for (final target in List<_ChaseTarget>.of(_targets)) {
      target.depth += (_currentSpeed / 410 + target.approachSpeed) * motionDt;
      target.wobble += motionDt * (0.82 + _currentSpeed / 260);
      final laneWander = sin(target.wobble * 1.55 + target.angle * 17) *
          (target.kind == _TargetKind.car ? 0.035 : 0.014);
      target.lane += (target.lateralVelocity + laneWander) * motionDt;
      target.lane = target.lane.clamp(-1.18, 1.18).toDouble();

      if (!target.hit) {
        _checkNearMiss(target);
        _checkCollision(target);
      }

      if (target.depth > 1.24 || target.hit) {
        _targets.remove(target);
      }
    }
  }

  void _updateParticles(double motionDt, double rawDt) {
    for (final wreck in List<_ChaseWreck>.of(_wrecks)) {
      wreck.life -= rawDt;
      wreck.position += wreck.velocity * motionDt;
      wreck.velocity = Offset(
        wreck.velocity.dx * pow(0.42, motionDt).toDouble(),
        min(235, wreck.velocity.dy + 44 * motionDt),
      );
      wreck.angle += wreck.spin * motionDt;
      if (wreck.life <= 0 ||
          wreck.position.dy > size.y + 220 ||
          wreck.position.dx < -260 ||
          wreck.position.dx > size.x + 260) {
        _wrecks.remove(wreck);
      }
    }

    for (final particle in List<_ShardParticle>.of(_particles)) {
      particle.life -= rawDt;
      particle.position += particle.velocity * motionDt;
      particle.velocity += Offset(0, 540 * motionDt);
      particle.angle += particle.spin * motionDt;
      if (particle.life <= 0 ||
          particle.position.dy > size.y + 90 ||
          particle.position.dx < -90 ||
          particle.position.dx > size.x + 90) {
        _particles.remove(particle);
      }
    }
  }

  void _updateStunts(double dt) {
    if (_currentSpeed > 170 && _playerLane < -0.48 && _oncomingTick <= 0) {
      _award('ONCOMING', 120 + (_currentSpeed * 0.9).round(), boostGain: 0.025);
      _oncomingTick = 0.9;
    }

    if (_currentSpeed > 158 && _steering.abs() > 0.76 && _driftTick <= 0) {
      _award('DRIFT', 140 + (_currentSpeed * 0.78).round(), boostGain: 0.022);
      _driftTick = 0.75;
    }

    if (_airCooldown <= 0 &&
        _currentSpeed > 205 &&
        _random.nextDouble() < 0.7) {
      _award('AIR', 360 + (_currentSpeed * 1.2).round(), boostGain: 0.06);
      _screenShake = max(_screenShake, 0.24);
      _airCooldown = 12 + _random.nextDouble() * 7;
    }
  }

  void _checkNearMiss(_ChaseTarget target) {
    if (target.nearMissAwarded ||
        target.depth < 0.82 ||
        target.depth > 1.03 ||
        target.kind == _TargetKind.shop) {
      return;
    }

    final distance = (target.lane - _playerLane).abs();
    final hitZone = target.hitWidth;
    if (distance > hitZone && distance < hitZone + 0.24) {
      target.nearMissAwarded = true;
      _award(
        'NEAR MISS',
        260 + (_currentSpeed * target.mass).round(),
        boostGain: 0.07,
      );
      _screenShake = max(_screenShake, 0.12);
    }
  }

  void _checkCollision(_ChaseTarget target) {
    if (_launchGraceTimer > 0) {
      return;
    }
    if (_impactCooldown > 0) {
      return;
    }
    if (target.depth < 0.84 || target.depth > 1.08) {
      return;
    }

    final distance = (target.lane - _playerLane).abs();
    final hitZone = target.hitWidth + (_isCrashing ? 0.02 : 0);
    if (distance <= hitZone) {
      _crashInto(target);
    }
  }

  void _crashInto(_ChaseTarget target) {
    target.hit = true;
    final side = (target.lane - _playerLane).abs() < 0.04
        ? (_random.nextBool() ? -1.0 : 1.0)
        : (_playerLane - target.lane).sign.toDouble();
    final projection = _project(target.lane, target.depth);
    final activeCombo = (_comboTimer > 0 ? combo.value + 1 : 1).clamp(1, 18);

    combo.value = activeCombo;
    _bestCombo = max(_bestCombo, activeCombo);
    _comboTimer = 2.2;
    objectsHit.value += 1;
    crashes.value += 1;

    final damageGain = (target.mass * 8.2 + 5).round().clamp(4, 22);
    damage.value = min(100, damage.value + damageGain);
    _playerDamage = max(
      _playerDamage,
      (damage.value / 100 + target.mass * 0.12).clamp(0.0, 0.92).toDouble(),
    );

    final points = ((target.points + _currentSpeed * target.mass * 5.8) *
            arena.scoreBonus *
            (1 + activeCombo * 0.18))
        .round();
    score.value += points;
    coins.value += max(1, points ~/ 105);
    scorePopupText.value = '+$points';
    _scorePopupTimer = 1.05;

    final label = switch (target.kind) {
      _TargetKind.car => 'CAR CRASH',
      _TargetKind.truck => 'TRUCK IMPACT',
      _TargetKind.bus => 'BUS HIT',
      _TargetKind.shop => 'SHOP SMASH',
    };
    impactText.value = label;
    stuntText.value = label;
    _impactTextTimer = 1.85;
    _stuntTextTimer = 1.35;
    _slowMotionTimer = max(_slowMotionTimer, target.mass > 2 ? 0.95 : 0.72);
    slowMotion.value = true;
    _screenShake = max(_screenShake, target.mass * 0.36);
    _boostMeter = min(1, _boostMeter + 0.07);
    boost.value = _boostMeter;

    _impactCooldown = target.mass > 2 ? 0.72 : 0.52;
    _playerCrashDuration = target.mass > 2 ? 1.08 : 0.86;
    _playerCrashTimer = _playerCrashDuration;
    _playerSpinVelocity = side *
        (target.mass > 2 ? 1.85 : 2.45) *
        (0.75 + _random.nextDouble() * 0.5);
    _playerKickLane = side * (0.44 + target.mass * 0.12);
    _currentSpeed = max(72, _currentSpeed - target.mass * 22);

    _spawnWreck(target, projection, side);
    _burst(projection, target);

    if (target.kind != _TargetKind.shop && _random.nextDouble() < 0.28) {
      _spawnTraffic(initialDepth: 0.18 + _random.nextDouble() * 0.16);
    }
  }

  void _award(String label, int basePoints, {double boostGain = 0}) {
    final activeCombo = (_comboTimer > 0 ? combo.value + 1 : 1).clamp(1, 18);
    combo.value = activeCombo;
    _bestCombo = max(_bestCombo, activeCombo);
    _comboTimer = 1.45;
    final points =
        (basePoints * arena.scoreBonus * (1 + activeCombo * 0.08)).round();
    score.value += points;
    coins.value += max(0, points ~/ 180);
    scorePopupText.value = '+$points';
    _scorePopupTimer = 0.85;
    stuntText.value = label;
    _stuntTextTimer = 1.0;
    _boostMeter = min(1, _boostMeter + boostGain);
    boost.value = _boostMeter;
  }

  Color _trafficColor(_TargetKind kind, ProceduralVehicleStyle style) {
    if (kind == _TargetKind.bus) {
      return const Color(0xFFF6C027);
    }
    if (kind == _TargetKind.truck) {
      return [
        const Color(0xFFF4F0E7),
        const Color(0xFFE4EAED),
        const Color(0xFFD9D3C8),
      ][_random.nextInt(3)];
    }
    final palette = switch (style) {
      ProceduralVehicleStyle.sport => [
          const Color(0xFFE64655),
          const Color(0xFF28A8E8),
          const Color(0xFFFFD12E),
        ],
      ProceduralVehicleStyle.rally => [
          const Color(0xFF6DCE3F),
          const Color(0xFFE7EFF2),
          const Color(0xFF2F7AE5),
        ],
      _ => [
          const Color(0xFFCE2B2C),
          const Color(0xFF2A67B8),
          const Color(0xFF3B3F45),
          const Color(0xFFE8E1D6),
        ],
    };
    return palette[_random.nextInt(palette.length)];
  }

  void _spawnTraffic({double initialDepth = 0.02, bool avoidCenter = false}) {
    final roll = _random.nextDouble();
    late _TargetKind kind;
    late ProceduralVehicleStyle style;
    if (roll < (arena.id == 'industrial_docks' ? 0.26 : 0.12)) {
      kind = _TargetKind.truck;
      style = ProceduralVehicleStyle.truck;
    } else if (roll < (arena.id == 'downtown_strip' ? 0.22 : 0.08)) {
      kind = _TargetKind.bus;
      style = ProceduralVehicleStyle.bus;
    } else {
      kind = _TargetKind.car;
      style = [
        ProceduralVehicleStyle.sedan,
        ProceduralVehicleStyle.sport,
        ProceduralVehicleStyle.rally,
      ][_random.nextInt(3)];
    }
    final targetColor = _trafficColor(kind, style);

    final laneChoices = kind == _TargetKind.car
        ? [-0.72, -0.34, 0.02, 0.38, 0.74]
        : [-0.72, -0.42, 0.42, 0.74];
    var lane = laneChoices[_random.nextInt(laneChoices.length)] +
        (_random.nextDouble() - 0.5) * 0.07;
    if (avoidCenter && lane.abs() < 0.28) {
      lane = _random.nextBool() ? -0.58 : 0.58;
    }
    final sameDirection = _random.nextDouble() < 0.58;
    _targets.add(
      _ChaseTarget(
        kind: kind,
        style: style,
        color: targetColor,
        lane: lane,
        depth: initialDepth,
        approachSpeed: sameDirection
            ? 0.12 + _random.nextDouble() * 0.08
            : 0.28 + _random.nextDouble() * 0.12,
        lateralVelocity: (_random.nextDouble() - 0.5) *
            (kind == _TargetKind.car ? 0.09 : 0.04),
        angle: (_random.nextDouble() - 0.5) * 0.1,
      ),
    );
  }

  void _spawnRoadsideShop() {
    final right = _random.nextBool();
    _targets.add(
      _ChaseTarget(
        kind: _TargetKind.shop,
        style: ProceduralVehicleStyle.sedan,
        color: const Color(0xFF8B6B35),
        lane: right ? 1.08 : -1.08,
        depth: 0.04,
        approachSpeed: 0.22,
        lateralVelocity: right ? -0.02 : 0.02,
        angle: right ? -0.05 : 0.05,
      ),
    );
  }

  void _spawnWreck(_ChaseTarget target, _Projection projection, double side) {
    if (target.kind == _TargetKind.shop) {
      return;
    }
    final width =
        projection.scale * (target.kind == _TargetKind.car ? 180 : 230);
    final height = width * proceduralVehicleHeightRatio(target.style);
    _wrecks.add(
      _ChaseWreck(
        style: target.style,
        color: target.color,
        position: projection.offset,
        velocity: Offset(
          -side * (145 + _random.nextDouble() * 105),
          54 + _random.nextDouble() * 68,
        ),
        angle: target.angle + side * 0.2,
        spin: -side * (1.55 + _random.nextDouble() * 1.75),
        size: Size(width, height),
        damage: 0.7 + _random.nextDouble() * 0.25,
        life: target.kind == _TargetKind.car ? 2.0 : 2.55,
      ),
    );
  }

  void _burst(_Projection projection, _ChaseTarget target) {
    final count = target.kind == _TargetKind.shop
        ? 18
        : target.kind == _TargetKind.car
            ? 24
            : 34;
    final baseSpeed = target.kind == _TargetKind.shop ? 280 : 430;
    for (var i = 0; i < count; i++) {
      final direction = -pi / 2 + (_random.nextDouble() - 0.5) * 3.2;
      final speed = 90 + _random.nextDouble() * baseSpeed;
      _particles.add(
        _ShardParticle(
          sprite: _debrisSprites[_random.nextInt(_debrisSprites.length)],
          position: projection.offset +
              Offset(
                (_random.nextDouble() - 0.5) * 54,
                (_random.nextDouble() - 0.5) * 34,
              ),
          velocity: Offset(cos(direction) * speed, sin(direction) * speed),
          spin: (_random.nextDouble() - 0.5) * 13,
          angle: _random.nextDouble() * pi,
          size: 18 +
              _random.nextDouble() * (target.kind == _TargetKind.car ? 25 : 38),
          life: 0.85 + _random.nextDouble() * 0.75,
        ),
      );
    }
  }

  void _syncNotifiers() {
    final speed = _currentSpeed.round();
    if (speedKmh.value != speed) {
      speedKmh.value = speed;
    }
    final remaining = max(0, (_levelLength - _elapsed).ceil());
    if (remainingSeconds.value != remaining) {
      remainingSeconds.value = remaining;
    }
    final progress = (_elapsed / _levelLength).clamp(0.0, 1.0);
    if ((levelProgress.value - progress).abs() > 0.002) {
      levelProgress.value = progress;
    }
    if ((boost.value - _boostMeter).abs() > 0.002) {
      boost.value = _boostMeter;
    }
  }

  void _drawBackdrop(Canvas canvas) {
    final speedRatio = (_currentSpeed / _maxSpeed).clamp(0.0, 1.0).toDouble();
    final rect = Rect.fromLTWH(0, 0, size.x, size.y);
    canvas.drawRect(
      rect,
      Paint()
        ..shader = ui.Gradient.linear(
          Offset(size.x * 0.5, 0),
          Offset(size.x * 0.5, size.y),
          [
            const Color(0xFF4FC6F6),
            const Color(0xFFCFF7FF),
            const Color(0xFFE7ECE6),
            const Color(0xFF49575E),
          ],
          const [0, 0.36, 0.56, 1],
        ),
    );

    _drawSunAndAtmosphere(canvas, speedRatio);
    _drawMountainLayer(
      canvas,
      baseY: _horizonY + 20,
      height: size.y * 0.19,
      color: const Color(0xFF9CA77E),
      alpha: 0.52,
      drift: 0.004,
      seed: 2.4,
    );
    _drawMountainLayer(
      canvas,
      baseY: _horizonY + 48,
      height: size.y * 0.13,
      color: const Color(0xFF6E735A),
      alpha: 0.44,
      drift: 0.007,
      seed: 7.7,
    );
    _drawDistantSkyline(canvas);
    _drawRoadsideDistrict(canvas);
    _drawOverpasses(canvas);
    _drawTrafficGantries(canvas);

    canvas.drawRect(
      rect,
      Paint()
        ..shader = ui.Gradient.radial(
          Offset(size.x * 0.5, size.y * 0.48),
          max(size.x, size.y) * 0.78,
          [
            Colors.transparent,
            const Color(0xFF061014).withValues(alpha: 0.2),
          ],
          const [0.58, 1],
        ),
    );
  }

  void _drawReliableImpactScene(Canvas canvas, {bool overlay = false}) {
    if (size.x <= 0 || size.y <= 0) {
      return;
    }
    final alpha = overlay ? 0.78 : 1.0;
    final horizon = size.y * 0.45;
    final center = size.x * 0.5;
    final bottomY = size.y + 70;
    final topHalf = max(34.0, size.x * 0.08);
    final bottomHalf = min(size.x * 0.68, 470.0);

    final road = Path()
      ..moveTo(center - topHalf, horizon)
      ..lineTo(center + topHalf, horizon)
      ..lineTo(center + bottomHalf, bottomY)
      ..lineTo(center - bottomHalf, bottomY)
      ..close();

    if (!overlay) {
      canvas.drawRect(
        Offset.zero & Size(size.x, size.y),
        Paint()
          ..shader = ui.Gradient.linear(
            Offset(center, 0),
            Offset(center, size.y),
            const [
              Color(0xFF64D9FF),
              Color(0xFFD8F8FF),
              Color(0xFF45545A),
            ],
            const [0, 0.46, 1],
          ),
      );
    }

    _drawReliableCity(canvas, horizon, alpha);

    canvas.drawPath(
      road,
      Paint()
        ..shader = ui.Gradient.linear(
          Offset(center, horizon),
          Offset(center, bottomY),
          [
            const Color(0xFF6D7375).withValues(alpha: 0.92 * alpha),
            const Color(0xFF303538).withValues(alpha: 0.98 * alpha),
            const Color(0xFF121417).withValues(alpha: alpha),
          ],
          const [0, 0.5, 1],
        ),
    );

    canvas.save();
    canvas.clipPath(road);
    _drawReliableLaneMarkers(canvas, horizon, bottomY, alpha);
    _drawReliableSpeedStreaks(canvas, horizon, alpha);
    _drawReliableTraffic(canvas, alpha);
    canvas.restore();

    canvas.drawPath(
      road,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2
        ..color = Colors.white.withValues(alpha: 0.16 * alpha),
    );
    _drawReliablePlayer(canvas, alpha);
  }

  void _drawEmergencyImpactScene(Canvas canvas) {
    if (size.x <= 0 || size.y <= 0) {
      return;
    }
    final center = size.x * 0.5;
    final horizon = size.y * 0.42;
    final bottom = size.y + 30;
    canvas.drawRect(
      Offset.zero & Size(size.x, size.y),
      Paint()..color = const Color(0xFF8EEAFF),
    );
    canvas.drawRect(
      Rect.fromLTWH(0, horizon, size.x, size.y - horizon),
      Paint()..color = const Color(0xFF86969A),
    );

    for (var i = 0; i < 10; i++) {
      final w = 42.0 + (i % 4) * 20;
      final h = 72.0 + (i % 5) * 34;
      final left = i.isEven ? i * 36.0 - 20 : size.x - i * 38.0 - w;
      final top = horizon + 8 + (i % 3) * 24;
      canvas.drawRect(
        Rect.fromLTWH(left, top, w, h),
        Paint()..color = const Color(0xFF26343A),
      );
      canvas.drawRect(
        Rect.fromLTWH(left + 5, top + 10, max(8, w - 10), 8),
        Paint()..color = const Color(0xFF88D8E8),
      );
    }

    final road = Path()
      ..moveTo(center - size.x * 0.08, horizon)
      ..lineTo(center + size.x * 0.08, horizon)
      ..lineTo(center + size.x * 0.58, bottom)
      ..lineTo(center - size.x * 0.58, bottom)
      ..close();
    canvas.drawPath(road, Paint()..color = const Color(0xFF24292D));
    canvas.drawLine(
      Offset(center, horizon),
      Offset(center, bottom),
      Paint()
        ..color = const Color(0xFFFFD84A)
        ..strokeWidth = 3,
    );
    for (var i = 0; i < 16; i++) {
      final y = horizon + i * (size.y - horizon) / 12 + _roadPulse * 36;
      final width = 14 + i * 7;
      canvas.drawRect(
        Rect.fromCenter(
            center: Offset(center - width * 0.7, y), width: 5, height: 28),
        Paint()..color = Colors.white,
      );
      canvas.drawRect(
        Rect.fromCenter(
            center: Offset(center + width * 0.7, y), width: 5, height: 28),
        Paint()..color = Colors.white,
      );
    }

    for (var i = 0; i < 6; i++) {
      final d = 0.25 + i * 0.09;
      final p = _project([-0.7, -0.35, 0.32, 0.72, -0.05, 0.5][i], d);
      final w = 34.0 + d * 70;
      final h = w * 0.48;
      final rect = Rect.fromCenter(
        center: p.offset.translate(0, -h * 0.35),
        width: w,
        height: h,
      );
      canvas.drawRRect(
        RRect.fromRectAndRadius(rect, const Radius.circular(5)),
        Paint()
          ..color = [
            const Color(0xFFFFC533),
            const Color(0xFF2C79D8),
            const Color(0xFFC73836),
            const Color(0xFFE9ECE8),
            const Color(0xFF5F6870),
            const Color(0xFF24303A),
          ][i],
      );
      canvas.drawRect(
        Rect.fromLTWH(
            rect.left + w * 0.22, rect.top + h * 0.18, w * 0.42, h * 0.26),
        Paint()..color = const Color(0xFFBFEFFF),
      );
      canvas.drawCircle(
        Offset(rect.left + w * 0.22, rect.bottom),
        max(3, w * 0.08),
        Paint()..color = Colors.black,
      );
      canvas.drawCircle(
        Offset(rect.right - w * 0.22, rect.bottom),
        max(3, w * 0.08),
        Paint()..color = Colors.black,
      );
    }

    final carW = min(size.x * 0.52, 260.0);
    final carH = carW * 0.45;
    final carCenter =
        Offset(center + _playerLane * size.x * 0.18, size.y * 0.83);
    final carRect = Rect.fromCenter(
      center: carCenter.translate(0, -carH * 0.35),
      width: carW,
      height: carH,
    );
    canvas.drawOval(
      Rect.fromCenter(
          center: carCenter.translate(0, 8),
          width: carW * 0.72,
          height: carH * 0.2),
      Paint()..color = Colors.black.withValues(alpha: 0.34),
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(carRect, Radius.circular(carW * 0.08)),
      Paint()..color = car.color,
    );
    canvas.drawRect(
      Rect.fromLTWH(
          carRect.center.dx - carW * 0.05, carRect.top, carW * 0.035, carH),
      Paint()..color = Colors.black,
    );
    canvas.drawRect(
      Rect.fromLTWH(
          carRect.center.dx + carW * 0.02, carRect.top, carW * 0.035, carH),
      Paint()..color = Colors.black,
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(carRect.left + carW * 0.27, carRect.top + carH * 0.12,
            carW * 0.46, carH * 0.32),
        const Radius.circular(8),
      ),
      Paint()..color = const Color(0xFFBFEFFF),
    );
    for (final dx in [-0.32, 0.32]) {
      final wheel = Offset(carRect.center.dx + dx * carW, carRect.bottom);
      canvas.drawCircle(wheel, carW * 0.08, Paint()..color = Colors.black);
      canvas.drawCircle(
          wheel, carW * 0.04, Paint()..color = const Color(0xFFECEFF2));
    }
  }

  void _drawReliableCity(Canvas canvas, double horizon, double alpha) {
    final sunCenter = Offset(size.x * 0.1, size.y * 0.14);
    canvas.drawCircle(
      sunCenter,
      min(size.x, size.y) * 0.18,
      Paint()
        ..shader = ui.Gradient.radial(
          sunCenter,
          min(size.x, size.y) * 0.18,
          [
            Colors.white.withValues(alpha: 0.34 * alpha),
            const Color(0xFFFFD884).withValues(alpha: 0.16 * alpha),
            Colors.transparent,
          ],
        ),
    );

    for (var side in [-1.0, 1.0]) {
      for (var i = 0; i < 8; i++) {
        final depth = 0.16 + i * 0.08;
        final scale = _lerp(0.24, 1.0, depth);
        final w = _lerp(34, 118, depth) * (0.8 + _noiseUnit(i + 19) * 0.6);
        final h = _lerp(54, 220, depth) * (0.72 + _noiseUnit(i + 31) * 0.5);
        final x = side < 0
            ? size.x * (0.08 + i * 0.018) - w * 0.6
            : size.x * (0.92 - i * 0.018) - w * 0.4;
        final y = horizon + _lerp(10, 260, depth) - h * 0.24;
        final rect = Rect.fromLTWH(x, y, w * scale, h * scale);
        canvas.drawRRect(
          RRect.fromRectAndRadius(rect, const Radius.circular(3)),
          Paint()
            ..shader = ui.Gradient.linear(
              rect.topLeft,
              rect.bottomRight,
              [
                const Color(0xFFC6D0D2).withValues(alpha: 0.22 * alpha),
                const Color(0xFF1D2A30).withValues(alpha: 0.64 * alpha),
              ],
            ),
        );
        if (i.isEven) {
          final pole = Paint()
            ..color = const Color(0xFF152027).withValues(alpha: 0.72 * alpha)
            ..strokeWidth = max(1.2, 2.4 * scale)
            ..strokeCap = StrokeCap.round;
          final base = Offset(
            side < 0 ? rect.right + 12 * scale : rect.left - 12 * scale,
            rect.bottom + 14 * scale,
          );
          final top = base.translate(0, -78 * scale);
          canvas.drawLine(base, top, pole);
          canvas.drawLine(
              top, top.translate(-side * 36 * scale, -10 * scale), pole);
        }
      }
    }

    final bridgeY = horizon + size.y * 0.11;
    canvas.drawRect(
      Rect.fromLTWH(0, bridgeY, size.x, max(8, size.y * 0.018)),
      Paint()..color = const Color(0xFF5A6468).withValues(alpha: 0.38 * alpha),
    );
    for (var x = -30.0; x < size.x + 50; x += 74) {
      canvas.drawLine(
        Offset(x, bridgeY + 4),
        Offset(x + 22, bridgeY + 76),
        Paint()
          ..color = const Color(0xFF343D42).withValues(alpha: 0.34 * alpha)
          ..strokeWidth = 4,
      );
    }
  }

  void _drawReliableLaneMarkers(
    Canvas canvas,
    double horizon,
    double bottomY,
    double alpha,
  ) {
    final center = size.x * 0.5;
    for (final lane in [-0.52, -0.18, 0.18, 0.52]) {
      final paint = Paint()
        ..color = Colors.white.withValues(alpha: 0.38 * alpha)
        ..strokeCap = StrokeCap.round;
      for (var i = 0; i < 18; i++) {
        final depth = ((i / 18) + _roadPulse) % 1;
        if (depth < 0.06 || depth > 0.98) {
          continue;
        }
        final p0 = _project(lane, depth);
        final p1 = _project(lane, min(1, depth + 0.045));
        paint.strokeWidth = _lerp(1.2, 7.0, depth);
        canvas.drawLine(p0.offset, p1.offset, paint);
      }
    }

    final yellow = Paint()
      ..color = const Color(0xFFFFD84A).withValues(alpha: 0.42 * alpha)
      ..strokeWidth = 2;
    canvas.drawLine(
        Offset(center - 6, horizon), Offset(center - 34, bottomY), yellow);
    canvas.drawLine(
        Offset(center + 6, horizon), Offset(center + 34, bottomY), yellow);
  }

  void _drawReliableSpeedStreaks(Canvas canvas, double horizon, double alpha) {
    final streakPaint = Paint()
      ..strokeCap = StrokeCap.round
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 1.4);
    for (var i = 0; i < 48; i++) {
      final depth = ((i * 0.061 + _roadPulse * 1.8) % 1).toDouble();
      if (depth < 0.12) {
        continue;
      }
      final lane = -1.08 + _noiseUnit(i + 77) * 2.16;
      final p0 = _project(lane, depth);
      final p1 = _project(lane, min(1, depth + 0.03 + _currentSpeed / 7000));
      streakPaint
        ..strokeWidth = _lerp(0.8, 3.4, depth)
        ..color =
            Colors.white.withValues(alpha: _lerp(0.04, 0.18, depth) * alpha);
      canvas.drawLine(p0.offset, p1.offset, streakPaint);
    }
  }

  void _drawReliableTraffic(Canvas canvas, double alpha) {
    for (var i = 0; i < 8; i++) {
      final phase = (i * 0.13 + _roadPulse * 0.72) % 1;
      final depth = 0.24 + phase * 0.54;
      final lane = [-0.84, -0.48, -0.16, 0.18, 0.52, 0.84][i % 6];
      final p = _project(lane, depth);
      final isBus = i % 5 == 0;
      final isTruck = i % 4 == 0 && !isBus;
      final w = (isBus
              ? 124.0
              : isTruck
                  ? 108.0
                  : 72.0) *
          p.scale;
      final h = (isBus
              ? 76.0
              : isTruck
                  ? 68.0
                  : 48.0) *
          p.scale;
      final color = switch (i % 6) {
        0 => const Color(0xFFFFC533),
        1 => const Color(0xFFE9ECE8),
        2 => const Color(0xFF2C79D8),
        3 => const Color(0xFFC73836),
        4 => const Color(0xFF59616A),
        _ => const Color(0xFF222A31),
      };
      _drawReliableVehicleShape(
        canvas,
        p.offset,
        Size(w, h),
        color.withValues(alpha: alpha),
        heavy: isBus || isTruck,
      );
    }

    for (final target in _targets.where((target) => target.depth > 0.18)) {
      final p = _project(target.lane, target.depth);
      final w = switch (target.kind) {
            _TargetKind.car => 82.0,
            _TargetKind.truck => 116.0,
            _TargetKind.bus => 132.0,
            _TargetKind.shop => 100.0,
          } *
          p.scale;
      final h = w * 0.58;
      _drawReliableVehicleShape(
        canvas,
        p.offset,
        Size(w, h),
        target.color.withValues(alpha: alpha),
        heavy:
            target.kind == _TargetKind.bus || target.kind == _TargetKind.truck,
      );
    }
  }

  void _drawReliableVehicleShape(
    Canvas canvas,
    Offset contact,
    Size size,
    Color color, {
    required bool heavy,
  }) {
    if (size.width < 4 || size.height < 4) {
      return;
    }
    canvas.drawOval(
      Rect.fromCenter(
        center: contact.translate(0, size.height * 0.08),
        width: size.width * 0.78,
        height: max(4, size.height * 0.16),
      ),
      Paint()
        ..color = Colors.black.withValues(alpha: 0.22)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 4),
    );
    final rect = Rect.fromCenter(
      center: contact.translate(0, -size.height * 0.48),
      width: size.width,
      height: size.height,
    );
    final body = heavy
        ? Path()
        : (Path()
          ..moveTo(rect.left + size.width * 0.08, rect.bottom)
          ..lineTo(rect.left + size.width * 0.2, rect.top + size.height * 0.18)
          ..lineTo(rect.right - size.width * 0.18, rect.top)
          ..lineTo(rect.right - size.width * 0.06, rect.bottom)
          ..close());
    if (heavy) {
      body.addRRect(
        RRect.fromRectAndRadius(
          rect,
          Radius.circular(max(2, size.width * 0.04)),
        ),
      );
    }
    canvas.drawPath(
      body,
      Paint()
        ..shader = ui.Gradient.linear(
          rect.topLeft,
          rect.bottomRight,
          [
            Color.lerp(color, Colors.white, 0.32)!,
            color,
            Color.lerp(color, Colors.black, 0.4)!,
          ],
        ),
    );
    final glass = Rect.fromLTWH(
      rect.left + size.width * 0.22,
      rect.top + size.height * 0.16,
      size.width * 0.48,
      size.height * 0.28,
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(
          glass, Radius.circular(max(1, size.width * 0.03))),
      Paint()..color = const Color(0xFFBFEFFF).withValues(alpha: 0.52),
    );
    final wheelPaint = Paint()
      ..color = const Color(0xFF080A0B).withValues(alpha: 0.92);
    for (final dx in [-0.32, 0.32]) {
      canvas.drawCircle(
        Offset(
            rect.center.dx + dx * size.width, rect.bottom - size.height * 0.06),
        max(2.5, size.width * 0.075),
        wheelPaint,
      );
    }
  }

  void _drawReliablePlayer(Canvas canvas, double alpha) {
    final lane = _playerLane.clamp(-1.18, 1.18).toDouble();
    final x = size.x * 0.5 + lane * min(size.x * 0.24, 148.0);
    final y = size.y * 0.88;
    final w = min(size.x * 0.5, 260.0);
    final h = w * 0.48;
    final rect =
        Rect.fromCenter(center: Offset(x, y - h * 0.42), width: w, height: h);

    canvas.save();
    canvas.translate(rect.center.dx, rect.center.dy);
    canvas.rotate((_playerSpinAngle + _steering * 0.08).clamp(-0.45, 0.45));
    canvas.translate(-rect.center.dx, -rect.center.dy);
    canvas.drawOval(
      Rect.fromCenter(
        center: Offset(rect.center.dx, rect.bottom + h * 0.12),
        width: w * 0.78,
        height: h * 0.18,
      ),
      Paint()
        ..color = Colors.black.withValues(alpha: 0.32 * alpha)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 8),
    );

    final body = Path()
      ..moveTo(rect.left + w * 0.04, rect.bottom - h * 0.18)
      ..quadraticBezierTo(rect.left + w * 0.12, rect.top + h * 0.22,
          rect.left + w * 0.36, rect.top)
      ..lineTo(rect.right - w * 0.34, rect.top)
      ..quadraticBezierTo(rect.right - w * 0.08, rect.top + h * 0.2,
          rect.right - w * 0.03, rect.bottom - h * 0.2)
      ..lineTo(rect.right - w * 0.14, rect.bottom)
      ..lineTo(rect.left + w * 0.14, rect.bottom)
      ..close();
    canvas.drawPath(
      body,
      Paint()
        ..shader = ui.Gradient.linear(
          rect.topLeft,
          rect.bottomRight,
          [
            Color.lerp(car.color, Colors.white, 0.34)!.withValues(alpha: alpha),
            car.color.withValues(alpha: alpha),
            Color.lerp(car.color, Colors.black, 0.42)!.withValues(alpha: alpha),
          ],
        ),
    );

    final stripe = Paint()
      ..color = const Color(0xFF080A0B).withValues(alpha: 0.74 * alpha);
    canvas.drawRect(
      Rect.fromLTWH(
          rect.center.dx - w * 0.055, rect.top + h * 0.02, w * 0.035, h * 0.92),
      stripe,
    );
    canvas.drawRect(
      Rect.fromLTWH(
          rect.center.dx + w * 0.02, rect.top + h * 0.02, w * 0.035, h * 0.92),
      stripe,
    );

    final glass = RRect.fromRectAndRadius(
      Rect.fromLTWH(
          rect.left + w * 0.27, rect.top + h * 0.12, w * 0.46, h * 0.32),
      Radius.circular(max(4, w * 0.03)),
    );
    canvas.drawRRect(
      glass,
      Paint()
        ..shader = ui.Gradient.linear(
          glass.outerRect.topLeft,
          glass.outerRect.bottomRight,
          [
            Colors.white.withValues(alpha: 0.72 * alpha),
            const Color(0xFF2D6F7A).withValues(alpha: 0.74 * alpha),
          ],
        ),
    );

    final wheelPaint = Paint()
      ..color = const Color(0xFF050607).withValues(alpha: 0.96 * alpha);
    final rimPaint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = max(2, w * 0.012)
      ..color = Colors.white.withValues(alpha: 0.36 * alpha);
    for (final dx in [-0.34, 0.34]) {
      final c = Offset(rect.center.dx + dx * w, rect.bottom - h * 0.02);
      canvas.drawCircle(c, w * 0.085, wheelPaint);
      canvas.drawCircle(c, w * 0.045, rimPaint);
      canvas.drawLine(
        c.translate(cos(_wheelSpin) * w * 0.04, sin(_wheelSpin) * w * 0.04),
        c.translate(-cos(_wheelSpin) * w * 0.04, -sin(_wheelSpin) * w * 0.04),
        rimPaint,
      );
    }

    final lightPaint = Paint()
      ..color = const Color(0xFFFF332E)
          .withValues(alpha: (_isCrashing ? 1.0 : 0.72) * alpha)
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 2);
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(
            rect.left + w * 0.11, rect.bottom - h * 0.25, w * 0.14, h * 0.08),
        Radius.circular(max(2, w * 0.015)),
      ),
      lightPaint,
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(
            rect.right - w * 0.25, rect.bottom - h * 0.25, w * 0.14, h * 0.08),
        Radius.circular(max(2, w * 0.015)),
      ),
      lightPaint,
    );
    canvas.restore();
  }

  void _drawSunAndAtmosphere(Canvas canvas, double speedRatio) {
    final shortestSide = min(size.x, size.y);
    final sunCenter = Offset(size.x * 0.06, size.y * 0.12);
    canvas.drawCircle(
      sunCenter,
      shortestSide * 0.23,
      Paint()
        ..shader = ui.Gradient.radial(
          sunCenter,
          shortestSide * 0.24,
          [
            Colors.white.withValues(alpha: 0.86),
            const Color(0xFFFFF0B0).withValues(alpha: 0.32),
            Colors.white.withValues(alpha: 0),
          ],
        ),
    );

    final cloudPaint = Paint()
      ..strokeCap = StrokeCap.round
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 8);
    for (var i = 0; i < 10; i++) {
      final y = size.y * (0.14 + _noiseUnit(i + 40) * 0.22);
      final x = ((i * 98 -
                  (_worldDistance * (0.012 + speedRatio * 0.006)) +
                  _cameraLane * 18) %
              (size.x + 220)) -
          110;
      cloudPaint
        ..strokeWidth = 10 + _noiseUnit(i + 50) * 18
        ..color = Colors.white.withValues(alpha: 0.08 + _noiseUnit(i) * 0.09);
      canvas.drawLine(
        Offset(x, y),
        Offset(x + 72 + _noiseUnit(i + 60) * 150, y - 8 + _noiseUnit(i) * 18),
        cloudPaint,
      );
    }
  }

  void _drawMountainLayer(
    Canvas canvas, {
    required double baseY,
    required double height,
    required Color color,
    required double alpha,
    required double drift,
    required double seed,
  }) {
    final offset =
        (_worldDistance * drift + _cameraLane * 10 + seed * 100) % 120;
    final path = Path()..moveTo(-140, baseY);
    for (double x = -140; x <= size.x + 160; x += 54) {
      final local = x + offset;
      final peak = baseY -
          height *
              (0.46 +
                  sin(local * 0.017 + seed) * 0.19 +
                  sin(local * 0.043 + seed * 0.7) * 0.12);
      path.lineTo(x, peak);
    }
    path
      ..lineTo(size.x + 160, baseY + height)
      ..lineTo(-140, baseY + height)
      ..close();
    canvas.drawPath(
      path,
      Paint()..color = color.withValues(alpha: alpha),
    );

    final ridgePaint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.2
      ..color = Colors.white.withValues(alpha: alpha * 0.18);
    canvas.drawPath(path, ridgePaint);
  }

  void _drawDistantSkyline(Canvas canvas) {
    final baseY = _horizonY + 72;
    final shift = (_worldDistance * 0.02 + _cameraLane * 30) % 92;
    for (var i = 0; i < 18; i++) {
      final rawX = i * 92 - shift - 80;
      final x = rawX % (size.x + 180) - 70;
      final width = 34 + _noiseUnit(i + 100) * 66;
      final height = 58 + _noiseUnit(i + 112) * 118;
      final top = baseY - height;
      final rect = Rect.fromLTWH(x, top, width, height);
      final buildingPaint = Paint()
        ..shader = ui.Gradient.linear(
          rect.topLeft,
          rect.bottomRight,
          [
            const Color(0xFFCAD5D9).withValues(alpha: 0.36),
            const Color(0xFF32444D).withValues(alpha: 0.58),
          ],
        );
      canvas.drawRRect(
        RRect.fromRectAndRadius(rect, const Radius.circular(3)),
        buildingPaint,
      );
      final windowPaint = Paint()..color = Colors.white.withValues(alpha: 0.11);
      for (double wx = x + 7; wx < x + width - 5; wx += 13) {
        for (double wy = top + 10; wy < baseY - 9; wy += 18) {
          if (_noiseUnit((wx + wy).round()) > 0.48) {
            canvas.drawRect(Rect.fromLTWH(wx, wy, 4, 7), windowPaint);
          }
        }
      }
    }
  }

  void _drawRoadsideDistrict(Canvas canvas) {
    for (final side in [-1.0, 1.0]) {
      for (var i = 0; i < 11; i++) {
        final phase =
            ((i / 11) + _roadPulse * 0.22 + (side > 0 ? 0.18 : 0.02)) % 1;
        final depth = 0.12 + phase * 0.76;
        if (depth < 0.18 || depth > 0.86) {
          continue;
        }
        final projection = _project(side * 1.33, depth);
        final scale = projection.scale;
        final width = _lerp(58, 260, scale) *
            (0.74 + _noiseUnit(i + (side > 0 ? 300 : 360)) * 0.48);
        final height = _lerp(54, 250, scale) *
            (0.75 + _noiseUnit(i + (side > 0 ? 400 : 460)) * 0.62);
        final x = projection.offset.dx + side * width * 0.35;
        final y = projection.offset.dy;
        if (x < -width * 1.2 || x > size.x + width * 1.2) {
          continue;
        }
        _drawStorefrontBlock(
          canvas,
          Rect.fromLTWH(x - width / 2, y - height, width, height),
          side,
          depth,
          i,
        );
        if (i.isEven) {
          _drawStreetLight(canvas, side, depth, i);
        }
      }
    }
  }

  void _drawStorefrontBlock(
    Canvas canvas,
    Rect rect,
    double side,
    double depth,
    int index,
  ) {
    final lean = side * rect.width * 0.11;
    final facade = Path()
      ..moveTo(rect.left - lean, rect.top + rect.height * 0.08)
      ..lineTo(rect.right - lean * 0.5, rect.top)
      ..lineTo(rect.right + lean * 0.35, rect.bottom)
      ..lineTo(rect.left + lean * 0.55, rect.bottom)
      ..close();
    final tint = Color.lerp(
      arena.sideTint,
      const Color(0xFF667782),
      0.28 + _noiseUnit(index + 220) * 0.28,
    )!;
    canvas.drawPath(
      facade,
      Paint()
        ..shader = ui.Gradient.linear(
          rect.topLeft,
          rect.bottomRight,
          [
            tint.withValues(alpha: _lerp(0.2, 0.68, depth)),
            const Color(0xFF071014).withValues(alpha: _lerp(0.38, 0.82, depth)),
          ],
        ),
    );
    canvas.drawPath(
      facade,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = max(1, rect.width * 0.012)
        ..color = Colors.white.withValues(alpha: _lerp(0.06, 0.2, depth)),
    );

    final awningColor = [
      const Color(0xFFC25F3A),
      const Color(0xFF2A83A6),
      const Color(0xFFDBB53A),
      const Color(0xFF2F9856),
    ][index % 4];
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(
          rect.left + rect.width * 0.08,
          rect.top + rect.height * 0.18,
          rect.width * 0.72,
          max(5, rect.height * 0.08),
        ),
        Radius.circular(max(2, rect.width * 0.02)),
      ),
      Paint()..color = awningColor.withValues(alpha: _lerp(0.28, 0.78, depth)),
    );

    final glass = Paint()
      ..shader = ui.Gradient.linear(
        rect.topLeft,
        rect.bottomRight,
        [
          const Color(0xFFE4FBFF).withValues(alpha: _lerp(0.12, 0.46, depth)),
          const Color(0xFF12313A).withValues(alpha: _lerp(0.28, 0.8, depth)),
        ],
      );
    final rows = depth > 0.5 ? 2 : 1;
    for (var row = 0; row < rows; row++) {
      for (var col = 0; col < 3; col++) {
        final w = rect.width * 0.14;
        final h = rect.height * 0.17;
        final wx = rect.left + rect.width * (0.16 + col * 0.22);
        final wy = rect.top + rect.height * (0.38 + row * 0.24);
        canvas.drawRRect(
          RRect.fromRectAndRadius(
            Rect.fromLTWH(wx, wy, w, h),
            Radius.circular(max(1.5, rect.width * 0.014)),
          ),
          glass,
        );
      }
    }
  }

  void _drawStreetLight(Canvas canvas, double side, double depth, int index) {
    final base = _project(side * 1.08, depth).offset;
    final scale = _project(side * 1.08, depth).scale;
    final poleHeight = _lerp(34, 190, scale);
    final polePaint = Paint()
      ..color =
          const Color(0xFF1A252C).withValues(alpha: _lerp(0.28, 0.9, depth))
      ..strokeWidth = max(1.4, 4.2 * scale)
      ..strokeCap = StrokeCap.round;
    final top = base.translate(side * 12 * scale, -poleHeight);
    canvas.drawLine(base, top, polePaint);
    canvas.drawLine(
      top,
      top.translate(-side * 44 * scale, -14 * scale),
      polePaint,
    );
    canvas.drawCircle(
      top.translate(-side * 52 * scale, -16 * scale),
      max(2, 7 * scale),
      Paint()
        ..shader = ui.Gradient.radial(
          top.translate(-side * 52 * scale, -16 * scale),
          max(3, 16 * scale),
          [
            const Color(0xFFFFE299).withValues(alpha: 0.42),
            const Color(0xFFFFE299).withValues(alpha: 0),
          ],
        ),
    );
  }

  void _drawOverpasses(Canvas canvas) {
    for (var i = 0; i < 3; i++) {
      final depth = 0.32 + ((i * 0.41 + _roadPulse * 0.12) % 1) * 0.3;
      if (depth > 0.61) {
        continue;
      }
      final center = _project(0, depth).offset;
      final roadLeft = _project(-1.08, depth).offset;
      final roadRight = _project(1.08, depth).offset;
      final y = center.dy - _lerp(42, 102, depth);
      final thickness = _lerp(8, 26, depth);
      final deck = Path()
        ..moveTo(-20, y - thickness)
        ..lineTo(size.x + 20, y - thickness * 1.25)
        ..lineTo(size.x + 35, y + thickness)
        ..lineTo(-35, y + thickness * 1.15)
        ..close();
      canvas.drawPath(
        deck,
        Paint()
          ..shader = ui.Gradient.linear(
            Offset(0, y - thickness),
            Offset(size.x, y + thickness),
            [
              const Color(0xFFE3D7C1).withValues(alpha: 0.48),
              const Color(0xFF52606B).withValues(alpha: 0.56),
            ],
          ),
      );
      canvas.drawPath(
        deck,
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = 1.2
          ..color = Colors.white.withValues(alpha: 0.16),
      );

      final pillarPaint = Paint()
        ..color = const Color(0xFF555E61).withValues(alpha: 0.42)
        ..strokeWidth = max(4, 15 * depth)
        ..strokeCap = StrokeCap.round;
      canvas.drawLine(
        Offset(roadLeft.dx - 36 * depth, y + thickness),
        Offset(roadLeft.dx - 26 * depth, roadLeft.dy + 44 * depth),
        pillarPaint,
      );
      canvas.drawLine(
        Offset(roadRight.dx + 36 * depth, y + thickness),
        Offset(roadRight.dx + 26 * depth, roadRight.dy + 44 * depth),
        pillarPaint,
      );

      if (i == 1) {
        final signRect = Rect.fromCenter(
          center:
              Offset(center.dx + 74 * sin(_elapsed * 0.3), y + thickness * 1.2),
          width: 84 * depth,
          height: 28 * depth,
        );
        canvas.drawRRect(
          RRect.fromRectAndRadius(signRect, Radius.circular(max(2, 4 * depth))),
          Paint()..color = const Color(0xFF197462).withValues(alpha: 0.7),
        );
        canvas.drawLine(
          signRect.centerLeft.translate(signRect.width * 0.22, 0),
          signRect.centerRight.translate(-signRect.width * 0.22, 0),
          Paint()
            ..color = Colors.white.withValues(alpha: 0.4)
            ..strokeWidth = max(1, 2 * depth),
        );
      }
    }
  }

  void _drawTrafficGantries(Canvas canvas) {
    for (var i = 0; i < 2; i++) {
      final depth = 0.38 + ((i * 0.54 + _roadPulse * 0.18) % 1) * 0.22;
      final left = _project(-1.08, depth).offset;
      final right = _project(1.08, depth).offset;
      final y = left.dy - _lerp(62, 138, depth);
      final paint = Paint()
        ..color =
            const Color(0xFF1F2B31).withValues(alpha: _lerp(0.42, 0.86, depth))
        ..strokeWidth = max(2, 5 * depth)
        ..strokeCap = StrokeCap.round;
      final start = Offset(left.dx - 34 * depth, y);
      final end = Offset(right.dx + 34 * depth, y - 5 * depth);
      canvas.drawLine(start, end, paint);
      canvas.drawLine(
        start,
        Offset(start.dx - 8 * depth, left.dy + 20 * depth),
        paint,
      );
      canvas.drawLine(
        end,
        Offset(end.dx + 8 * depth, right.dy + 20 * depth),
        paint,
      );

      for (final lane in [-0.58, 0.0, 0.58]) {
        final signalBase = _project(lane, depth).offset;
        final box = Rect.fromCenter(
          center: Offset(signalBase.dx, y + 12 * depth),
          width: 18 * depth,
          height: 38 * depth,
        );
        canvas.drawRRect(
          RRect.fromRectAndRadius(box, Radius.circular(max(2, 3 * depth))),
          Paint()..color = const Color(0xFF253132).withValues(alpha: 0.82),
        );
        for (var light = 0; light < 3; light++) {
          final active = (light + i) % 3 == 2;
          final color = light == 2
              ? const Color(0xFF46F277)
              : light == 1
                  ? const Color(0xFFFFD14B)
                  : const Color(0xFFFF453A);
          canvas.drawCircle(
            Offset(box.center.dx, box.top + box.height * (0.22 + light * 0.28)),
            max(1.8, 4.2 * depth),
            Paint()..color = color.withValues(alpha: active ? 0.9 : 0.26),
          );
        }
      }
    }
  }

  void _drawRoad(Canvas canvas) {
    final horizon = _horizonY;
    final center = size.x * 0.5;
    final bottomY = size.y + 80;
    final bottomHalf = min(size.x * 0.62, 450.0);
    final topHalf = max(46.0, size.x * 0.08);
    final road = Path()
      ..moveTo(center - topHalf, horizon)
      ..lineTo(center + topHalf, horizon)
      ..lineTo(center + bottomHalf, bottomY)
      ..lineTo(center - bottomHalf, bottomY)
      ..close();
    canvas.drawPath(
      road,
      Paint()
        ..shader = ui.Gradient.linear(
          Offset(center, horizon),
          Offset(center, bottomY),
          [
            Color.lerp(arena.roadTint, Colors.white, 0.16)!
                .withValues(alpha: 0.82),
            Color.lerp(arena.roadTint, Colors.black, 0.22)!
                .withValues(alpha: 0.94),
            const Color(0xFF111417),
          ],
          const [0, 0.54, 1],
        ),
    );
    _drawRoadMotion(canvas, road);

    final edgePaint = Paint()
      ..color = Colors.white.withValues(alpha: 0.12)
      ..strokeWidth = 2;
    canvas.drawLine(
      Offset(center - topHalf, horizon),
      Offset(center - bottomHalf, bottomY),
      edgePaint,
    );
    canvas.drawLine(
      Offset(center + topHalf, horizon),
      Offset(center + bottomHalf, bottomY),
      edgePaint,
    );

    for (final lane in [-0.5, -0.18, 0.18, 0.5]) {
      final paint = Paint()
        ..color = Colors.white.withValues(alpha: lane.abs() < 0.2 ? 0.2 : 0.14)
        ..strokeCap = StrokeCap.round;
      for (var i = 0; i < 24; i++) {
        final d0 = ((i + _roadPulse) / 24).clamp(0.0, 1.0).toDouble();
        final d1 = min(1.0, d0 + 0.044);
        if (d0 < 0.02 || d0 > 0.98) {
          continue;
        }
        final p0 = _project(lane, d0);
        final p1 = _project(lane, d1);
        paint.strokeWidth = _lerp(1.2, 6.2, pow(d0, 1.6).toDouble());
        canvas.drawLine(p0.offset, p1.offset, paint);
      }
    }
  }

  void _drawAmbientTraffic(Canvas canvas) {
    final speedRatio = (_currentSpeed / _maxSpeed).clamp(0.0, 1.0).toDouble();
    for (var i = 0; i < 9; i++) {
      final phase = (i * 0.137 + _roadPulse * (0.55 + speedRatio * 0.42)) % 1;
      final depth = 0.26 + phase * 0.34;
      if (depth > 0.68) {
        continue;
      }
      final lane = [-0.84, -0.58, -0.28, 0.26, 0.54, 0.82][i % 6] +
          sin(_elapsed * 0.7 + i) * 0.025;
      final projection = _project(lane, depth);
      final style = i % 5 == 0
          ? ProceduralVehicleStyle.truck
          : i % 3 == 0
              ? ProceduralVehicleStyle.sport
              : ProceduralVehicleStyle.sedan;
      final color = switch (i % 6) {
        0 => const Color(0xFFE9ECE8),
        1 => const Color(0xFF2C79D8),
        2 => const Color(0xFFC73836),
        3 => const Color(0xFFFFC533),
        4 => const Color(0xFF323942),
        _ => const Color(0xFFE7E1D4),
      };
      final width = (style == ProceduralVehicleStyle.truck ? 132.0 : 96.0) *
          projection.scale;
      final opacity = _lerp(0.22, 0.72, phase).clamp(0.0, 0.72).toDouble();
      drawProceduralVehicle(
        canvas,
        contact: projection.offset,
        size: Size(width, width * proceduralVehicleHeightRatio(style)),
        style: style,
        color: color,
        angle: sin(_elapsed + i) * 0.018,
        wheelSpin: _wheelSpin * (0.6 + phase),
        opacity: opacity,
        shadowAlpha: 0.18 * opacity,
      );
    }

    final glowPaint = Paint()
      ..strokeCap = StrokeCap.round
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3);
    for (var i = 0; i < 18; i++) {
      final d = ((i * 0.071) + _roadPulse * 1.4) % 1;
      if (d < 0.1 || d > 0.88) {
        continue;
      }
      final lane = -1.08 + _noiseUnit(i + 300) * 2.16;
      final p0 = _project(lane, d);
      final p1 = _project(lane * 0.92, min(1.0, d + 0.045));
      glowPaint
        ..strokeWidth = _lerp(0.8, 3.4, d)
        ..color = const Color(0xFFBFEFFF).withValues(
          alpha: _lerp(0.02, 0.11, d) * (0.8 + speedRatio * 0.6),
        );
      canvas.drawLine(p0.offset, p1.offset, glowPaint);
    }
  }

  void _drawRoadMotion(Canvas canvas, Path road) {
    canvas.save();
    canvas.clipPath(road);
    final asphaltShade = Paint()
      ..shader = ui.Gradient.linear(
        Offset(size.x * 0.5, _horizonY),
        Offset(size.x * 0.5, size.y),
        [
          Colors.transparent,
          Colors.black.withValues(alpha: 0.1),
          Colors.white.withValues(alpha: 0.05),
        ],
        const [0, 0.62, 1],
      );
    canvas.drawRect(Rect.fromLTWH(0, _horizonY, size.x, size.y), asphaltShade);

    for (var i = 0; i < 18; i++) {
      final depth = ((i / 18) + _roadPulse * 1.22) % 1;
      if (depth < 0.1 || depth > 0.98) {
        continue;
      }
      final nextDepth = min(1.0, depth + 0.024 + depth * 0.012);
      final left0 = _project(-1.15, depth).offset;
      final right0 = _project(1.15, depth).offset;
      final left1 = _project(-1.12, nextDepth).offset;
      final right1 = _project(1.12, nextDepth).offset;
      final band = Path()
        ..moveTo(left0.dx, left0.dy)
        ..lineTo(right0.dx, right0.dy)
        ..lineTo(right1.dx, right1.dy)
        ..lineTo(left1.dx, left1.dy)
        ..close();
      final shade = i.isEven ? Colors.black : Colors.white;
      canvas.drawPath(
        band,
        Paint()
          ..color = shade.withValues(
            alpha: _lerp(0.015, 0.07, depth) * (_accelerating ? 1.28 : 0.82),
          ),
      );
    }

    for (var i = 0; i < 76; i++) {
      final phase = (i * 0.037 + _roadPulse * 1.85) % 1;
      final depth = pow(phase, 0.82).toDouble();
      if (depth < 0.08 || depth > 0.99) {
        continue;
      }
      final lane = -1.05 + _noiseUnit(i) * 2.1;
      final length = 0.016 + _currentSpeed / 9000 + depth * 0.022;
      final p0 = _project(lane, depth);
      final p1 =
          _project(lane + (_noiseUnit(i + 71) - 0.5) * 0.05, depth + length);
      final alpha = _lerp(0.025, 0.18, depth) *
          (_accelerating ? 1.25 : 0.85) *
          (_isCrashing ? 1.35 : 1.0);
      canvas.drawLine(
        p0.offset,
        p1.offset,
        Paint()
          ..color =
              Colors.white.withValues(alpha: alpha.clamp(0.0, 0.22).toDouble())
          ..strokeWidth = _lerp(0.7, 3.0, depth)
          ..strokeCap = StrokeCap.round,
      );
    }

    for (var side in [-1.0, 1.0]) {
      final curbPaint = Paint()
        ..color = Colors.black.withValues(alpha: 0.22)
        ..strokeWidth = 3
        ..strokeCap = StrokeCap.round;
      for (var i = 0; i < 18; i++) {
        final depth = ((i / 18) + _roadPulse * 0.85) % 1;
        if (depth < 0.12) {
          continue;
        }
        final p0 = _project(side * 1.1, depth);
        final p1 = _project(side * 1.04, min(1, depth + 0.03));
        curbPaint.strokeWidth = _lerp(1.0, 5.0, depth);
        canvas.drawLine(p0.offset, p1.offset, curbPaint);
      }
    }
    canvas.restore();
  }

  void _drawRoadsideTargets(Canvas canvas) {
    final shops = _targets.where((target) => target.kind == _TargetKind.shop);
    for (final target in shops.toList()
      ..sort((a, b) => a.depth.compareTo(b.depth))) {
      final projection = _project(target.lane, target.depth);
      _drawShop(canvas, projection, target.lane.sign);
    }
  }

  void _drawTraffic(Canvas canvas) {
    final traffic = _targets
        .where((target) => target.kind != _TargetKind.shop)
        .toList()
      ..sort((a, b) => a.depth.compareTo(b.depth));
    for (final target in traffic) {
      final projection = _project(target.lane, target.depth);
      final baseWidth = switch (target.kind) {
        _TargetKind.car => 142.0,
        _TargetKind.truck => 176.0,
        _TargetKind.bus => 190.0,
        _TargetKind.shop => 160.0,
      };
      final width = baseWidth * projection.scale;
      final height = width * proceduralVehicleHeightRatio(target.style);
      final angle = target.angle +
          sin(target.wobble * 3.2) * 0.018 +
          target.lateralVelocity * 0.45;
      drawProceduralVehicle(
        canvas,
        contact: projection.offset,
        size: Size(width, height),
        style: target.style,
        color: target.color,
        angle: angle,
        wheelSpin: _wheelSpin * (0.8 + projection.scale),
        steer: target.lateralVelocity * 2.8,
        damage: 0.02,
        brakeLights: target.depth > 0.75 && target.approachSpeed < 0.18,
        shadowAlpha: target.kind == _TargetKind.car ? 0.34 : 0.4,
      );
    }
  }

  void _drawWrecks(Canvas canvas) {
    for (final wreck in _wrecks) {
      final opacity = (wreck.life / 0.75).clamp(0.0, 1.0).toDouble();
      final trail = wreck.velocity.distance.clamp(0.0, 260.0).toDouble();
      if (trail > 18) {
        final skidPaint = Paint()
          ..color = Colors.black.withValues(alpha: 0.16 * opacity)
          ..strokeWidth = max(2, wreck.size.width * 0.018)
          ..strokeCap = StrokeCap.round
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3);
        final drag = wreck.velocity / max(1, wreck.velocity.distance);
        canvas.drawLine(
          wreck.position - drag * trail * 0.34,
          wreck.position - drag * trail * 0.08,
          skidPaint,
        );
      }
      drawProceduralVehicle(
        canvas,
        contact: wreck.position,
        size: wreck.size,
        style: wreck.style,
        color: wreck.color,
        angle: wreck.angle,
        wheelSpin: _wheelSpin + wreck.angle * 3,
        damage: wreck.damage,
        steer: wreck.spin.sign * 0.35,
        opacity: opacity,
        brakeLights: true,
        shadowAlpha: 0.3 * opacity,
      );
    }
  }

  void _drawParticles(Canvas canvas) {
    for (final particle in _particles) {
      final opacity = particle.life.clamp(0.0, 1.0).toDouble();
      _drawSpriteCentered(
        canvas,
        particle.sprite,
        particle.position,
        Size.square(particle.size),
        particle.angle,
        Paint()..color = Colors.white.withValues(alpha: opacity),
      );
    }
  }

  void _drawPlayer(Canvas canvas) {
    final lane = (_playerLane + _playerKickLane * 0.12).clamp(-1.18, 1.18);
    final bottomHalf = min(size.x * 0.62, 450.0);
    final center = size.x * 0.5;
    final x = center + lane * bottomHalf * 0.36;
    final y = size.y * 0.895 + (_isCrashing ? sin(_elapsed * 18) * 2 : 0);
    final width = min(size.x * 0.38, 190.0) * (size.x < 430 ? 0.9 : 1);
    final style = proceduralStyleForCarId(car.id);
    final height = width * proceduralVehicleHeightRatio(style);
    final steerLean = _steering * 0.08;
    final crashLift = _isCrashing
        ? max(0.0, sin((_playerCrashDuration - _playerCrashTimer) * 4.8)) * 7
        : 0.0;
    _drawTireSpeedTrails(canvas, Offset(x, y), Size(width, height),
        _playerSpinAngle + steerLean);
    final crashProgress = _isCrashing
        ? (1 - (_playerCrashTimer / _playerCrashDuration).clamp(0.0, 1.0))
            .toDouble()
        : 0.0;
    drawProceduralVehicle(
      canvas,
      contact: Offset(x, y),
      size: Size(width, height),
      style: style,
      color: car.color,
      angle: _playerSpinAngle + steerLean,
      wheelSpin: _wheelSpin,
      steer: _steering,
      damage: max(_playerDamage, crashProgress * 0.85),
      lift: crashLift,
      brakeLights: _isCrashing,
      shadowAlpha: _isCrashing ? 0.34 : 0.55,
    );
  }

  void _drawShop(Canvas canvas, _Projection projection, double side) {
    if (projection.scale > 0.82) {
      return;
    }
    final width = 160 * projection.scale;
    final height = 96 * projection.scale;
    final fade =
        (1 - ((projection.scale - 0.52) / 0.3).clamp(0.0, 1.0)).toDouble();
    final contact = projection.offset + Offset(side * width * 0.42, 0);
    _drawContactShadow(
      canvas,
      contact,
      width * 0.92,
      height,
      0.18 * fade,
    );
    final center = Offset(contact.dx, contact.dy - height * 0.46);
    final rect = Rect.fromCenter(center: center, width: width, height: height);
    final lean = side * width * 0.1;
    final facadePath = Path()
      ..moveTo(rect.left - lean, rect.top)
      ..lineTo(rect.right - lean, rect.top + height * 0.04)
      ..lineTo(rect.right + lean * 0.42, rect.bottom)
      ..lineTo(rect.left + lean * 0.42, rect.bottom - height * 0.03)
      ..close();
    final facade = Paint()
      ..shader = ui.Gradient.linear(
        rect.topLeft,
        rect.bottomRight,
        [
          const Color(0xFF56636B).withValues(alpha: 0.38 * fade),
          const Color(0xFF172226).withValues(alpha: 0.58 * fade),
        ],
      );
    final trim = Paint()
      ..color = Colors.white.withValues(alpha: 0.2 * fade)
      ..strokeWidth = max(1.2, 3.2 * projection.scale)
      ..style = PaintingStyle.stroke;
    canvas.save();
    canvas.translate(center.dx, center.dy);
    canvas.rotate(side * -0.05);
    canvas.translate(-center.dx, -center.dy);
    canvas.drawPath(
      facadePath,
      Paint()..color = const Color(0xFF05080A).withValues(alpha: 0.14 * fade),
    );
    canvas.drawPath(facadePath, facade);
    canvas.drawPath(facadePath, trim);
    final awning = Paint()
      ..shader = ui.Gradient.linear(
        rect.topLeft,
        rect.topRight,
        [
          const Color(0xFFF0C15A).withValues(alpha: 0.34 * fade),
          arena.primary.withValues(alpha: 0.28 * fade),
          const Color(0xFFC06B46).withValues(alpha: 0.36 * fade),
        ],
      );
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(
          rect.left - lean * 0.75,
          rect.top - height * 0.08,
          width,
          height * 0.12,
        ),
        Radius.circular(max(2, width * 0.02)),
      ),
      awning,
    );
    final signRect = Rect.fromLTWH(
      rect.left + width * 0.2 - lean * 0.38,
      rect.top + height * 0.08,
      width * 0.56,
      height * 0.11,
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        signRect,
        Radius.circular(max(1.5, width * 0.015)),
      ),
      Paint()..color = const Color(0xFF121B1F).withValues(alpha: 0.46 * fade),
    );
    canvas.drawLine(
      signRect.centerLeft.translate(signRect.width * 0.16, 0),
      signRect.centerRight.translate(-signRect.width * 0.16, 0),
      Paint()
        ..color = arena.primary.withValues(alpha: 0.44 * fade)
        ..strokeWidth = max(1, 2.5 * projection.scale)
        ..strokeCap = StrokeCap.round,
    );
    canvas.drawRect(
      Rect.fromLTWH(
        rect.left - lean * 0.75,
        rect.bottom - height * 0.12,
        width,
        height * 0.1,
      ),
      Paint()..color = Colors.black.withValues(alpha: 0.24 * fade),
    );
    final glass = Paint()
      ..shader = ui.Gradient.linear(
        rect.topLeft,
        rect.bottomRight,
        [
          const Color(0xFFD8F5FF).withValues(alpha: 0.12 * fade),
          const Color(0xFF0A2630).withValues(alpha: 0.18 * fade),
        ],
      );
    for (var i = 0; i < 3; i++) {
      canvas.drawRect(
        Rect.fromLTWH(
          rect.left + width * (0.13 + i * 0.25),
          rect.top + height * 0.34,
          width * 0.14,
          height * 0.34,
        ),
        glass,
      );
    }
    canvas.restore();
  }

  void _drawSlowMotionTint(Canvas canvas) {
    if (_slowMotionTimer <= 0) {
      return;
    }
    final alpha = (_slowMotionTimer / 1.65).clamp(0.0, 1.0).toDouble();
    canvas.drawRect(
      Rect.fromLTWH(0, 0, size.x, size.y),
      Paint()..color = const Color(0xFF8BD8FF).withValues(alpha: 0.07 * alpha),
    );
    final vignette = Paint()
      ..shader = ui.Gradient.radial(
        Offset(size.x * 0.5, size.y * 0.54),
        max(size.x, size.y) * 0.72,
        [Colors.transparent, Colors.black.withValues(alpha: 0.32 * alpha)],
        const [0.55, 1],
      );
    canvas.drawRect(Rect.fromLTWH(0, 0, size.x, size.y), vignette);
  }

  void _drawSpriteCentered(
    Canvas canvas,
    Sprite sprite,
    Offset center,
    Size renderSize,
    double angle, [
    Paint? paint,
  ]) {
    canvas.save();
    canvas.translate(center.dx, center.dy);
    canvas.rotate(angle);
    sprite.render(
      canvas,
      position: Vector2(-renderSize.width / 2, -renderSize.height / 2),
      size: Vector2(renderSize.width, renderSize.height),
      overridePaint: paint,
    );
    canvas.restore();
  }

  void _drawTireSpeedTrails(
    Canvas canvas,
    Offset contact,
    Size renderSize,
    double angle,
  ) {
    if (_currentSpeed < 96 && !_accelerating) {
      return;
    }
    final opacity = ((_currentSpeed - 82) / 170).clamp(0.0, 1.0).toDouble();
    final trailPaint = Paint()
      ..color = Colors.black.withValues(alpha: 0.2 * opacity)
      ..strokeWidth = max(2, renderSize.width * 0.018)
      ..strokeCap = StrokeCap.round
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 4);
    canvas.save();
    canvas.translate(contact.dx, contact.dy - renderSize.height * 0.48);
    canvas.rotate(angle);
    for (final wheelX in [-0.25, 0.33]) {
      final y = renderSize.height * 0.28;
      final start = Offset(renderSize.width * wheelX, y);
      final end = start.translate(
        -_steering * renderSize.width * 0.09,
        renderSize.height * (0.32 + opacity * 0.18),
      );
      canvas.drawLine(start, end, trailPaint);
    }
    canvas.restore();
  }

  void _drawContactShadow(
    Canvas canvas,
    Offset contact,
    double width,
    double height,
    double alpha,
  ) {
    final shadowPaint = Paint()
      ..color = Colors.black.withValues(alpha: alpha.clamp(0.0, 0.6).toDouble())
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 9);
    canvas.drawOval(
      Rect.fromCenter(
        center: Offset(contact.dx, contact.dy + height * 0.03),
        width: width * 0.68,
        height: max(8, height * 0.13),
      ),
      shadowPaint,
    );
  }

  _Projection _project(double lane, double depth) {
    final d = depth.clamp(0.0, 1.12).toDouble();
    final roadEase = pow(d.clamp(0.0, 1.0), 1.76).toDouble();
    final y = _lerp(_horizonY, size.y * 0.92, roadEase);
    final roadWidth = _lerp(
      max(86, size.x * 0.16),
      min(size.x * 1.18, 780),
      pow(d.clamp(0.0, 1.0), 1.12).toDouble(),
    );
    final curve = sin(_elapsed * 0.24 + d * 2.4) * 22 * d;
    final x = size.x * 0.5 + curve + lane * roadWidth * 0.34;
    final scale = _lerp(0.08, 1.05, pow(d.clamp(0.0, 1.0), 1.85).toDouble());
    return _Projection(offset: Offset(x, y), scale: scale);
  }

  double get _horizonY => size.y * (size.x < 560 ? 0.48 : 0.5);

  double _lerp(double a, double b, double t) => a + (b - a) * t;

  double _noiseUnit(int seed) {
    final value = sin(seed * 12.9898) * 43758.5453;
    return value - value.floorToDouble();
  }

  void _finish({required bool levelComplete}) {
    if (_finished) {
      return;
    }
    _finished = true;
    onFinished(
      GameResult(
        arenaName: '${arena.name} Impact Run',
        score: score.value,
        coins: coins.value + (levelComplete ? 110 : 0),
        damagePercent: damage.value,
        objectsHit: objectsHit.value,
        maxSpeed: speedKmh.value,
        bestCombo: _bestCombo,
        levelComplete: levelComplete,
      ),
    );
  }
}

enum _TargetKind { car, truck, bus, shop }

class _ChaseTarget {
  _ChaseTarget({
    required this.kind,
    required this.style,
    required this.color,
    required this.lane,
    required this.depth,
    required this.approachSpeed,
    required this.lateralVelocity,
    required this.angle,
  });

  final _TargetKind kind;
  final ProceduralVehicleStyle style;
  final Color color;
  double lane;
  double depth;
  final double approachSpeed;
  final double lateralVelocity;
  final double angle;
  double wobble = 0;
  bool hit = false;
  bool nearMissAwarded = false;

  double get mass {
    return switch (kind) {
      _TargetKind.car => 1.25,
      _TargetKind.truck => 2.45,
      _TargetKind.bus => 2.35,
      _TargetKind.shop => 1.75,
    };
  }

  int get points {
    return switch (kind) {
      _TargetKind.car => 780,
      _TargetKind.truck => 1380,
      _TargetKind.bus => 1260,
      _TargetKind.shop => 920,
    };
  }

  double get hitWidth {
    return switch (kind) {
      _TargetKind.car => 0.16,
      _TargetKind.truck => 0.23,
      _TargetKind.bus => 0.25,
      _TargetKind.shop => 0.2,
    };
  }
}

class _ChaseWreck {
  _ChaseWreck({
    required this.style,
    required this.color,
    required this.position,
    required this.velocity,
    required this.angle,
    required this.spin,
    required this.size,
    required this.damage,
    required this.life,
  });

  final ProceduralVehicleStyle style;
  final Color color;
  Offset position;
  Offset velocity;
  double angle;
  final double spin;
  final Size size;
  final double damage;
  double life;
}

class _ShardParticle {
  _ShardParticle({
    required this.sprite,
    required this.position,
    required this.velocity,
    required this.spin,
    required this.angle,
    required this.size,
    required this.life,
  });

  final Sprite sprite;
  Offset position;
  Offset velocity;
  final double spin;
  double angle;
  final double size;
  double life;
}

class _Projection {
  const _Projection({required this.offset, required this.scale});

  final Offset offset;
  final double scale;
}
