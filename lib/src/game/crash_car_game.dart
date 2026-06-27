import 'dart:math';

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
    required this.upgradeLevel,
    required this.onFinished,
  });

  final CarSpec car;
  final int upgradeLevel;
  final void Function(GameResult result) onFinished;

  final score = ValueNotifier<int>(0);
  final coins = ValueNotifier<int>(0);
  final damage = ValueNotifier<int>(0);
  final objectsHit = ValueNotifier<int>(0);
  final speedKmh = ValueNotifier<int>(0);
  final combo = ValueNotifier<int>(1);
  final levelProgress = ValueNotifier<double>(0);

  final Random _random = Random();
  late PlayerCar _player;
  late final Sprite _crateSprite;
  late final Sprite _redBarrelSprite;
  late final Sprite _steelBarrelSprite;
  late final Sprite _coneSprite;
  late final Sprite _barricadeSprite;
  late final List<Sprite> _debrisSprites;

  double _spawnTimer = 0;
  double _elapsed = 0;
  double _roadScroll = 0;
  double _steering = 0;
  bool _boosting = false;
  bool _finished = false;
  int _bestCombo = 1;
  double _comboTimer = 0;

  double get _carBoost => upgradeLevel * 0.04;
  double get _roadSpeed =>
      280 + (car.speed + _carBoost) * 250 + (_boosting ? 130 : 0);
  double get _steerSpeed => 290 + car.handling * 260 + upgradeLevel * 18;
  double get _damageMultiplier =>
      max(0.45, 1.06 - car.durability * 0.42 - upgradeLevel * 0.035);
  double get _levelLength => 60;

  @override
  Color backgroundColor() => const Color(0xFF071014);

  @override
  Future<void> onLoad() async {
    await images.loadAll([
      _assetKey(car.asset),
      'ui/road_lane.png',
      'obstacles/crate.png',
      'obstacles/red_barrel.png',
      'obstacles/steel_barrel.png',
      'obstacles/cone.png',
      'obstacles/barricade.png',
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
    _debrisSprites = [
      Sprite(images.fromCache('debris/wood_1.png')),
      Sprite(images.fromCache('debris/wood_2.png')),
      Sprite(images.fromCache('debris/metal_1.png')),
      Sprite(images.fromCache('debris/glass_1.png')),
    ];

    add(RoadLayer(sprite: Sprite(images.fromCache('ui/road_lane.png'))));
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
    _roadScroll = (_roadScroll + _roadSpeed * dt) % max(1, size.y);
    _spawnTimer -= dt;
    _comboTimer -= dt;

    if (_comboTimer <= 0 && combo.value != 1) {
      combo.value = 1;
    }

    final nextX = (_player.x + _steering * _steerSpeed * dt)
        .clamp(54, size.x - 54)
        .toDouble();
    _player.x = nextX;
    _player.angle = _steering * 0.08;

    final speed = (_roadSpeed * 0.36).round().clamp(0, 245);
    if (speedKmh.value != speed) {
      speedKmh.value = speed;
    }

    final progress = (_elapsed / _levelLength).clamp(0.0, 1.0);
    if ((levelProgress.value - progress).abs() > 0.002) {
      levelProgress.value = progress;
    }

    if (_spawnTimer <= 0) {
      _spawnObstacle();
      _spawnTimer = max(0.28, 0.78 - min(0.34, _elapsed * 0.006));
    }

    if (_elapsed >= _levelLength) {
      _finish(levelComplete: true);
    }
  }

  @override
  void render(Canvas canvas) {
    super.render(canvas);
    final paint = Paint()..color = Colors.white.withValues(alpha: 0.06);
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

  void smash(ObstacleComponent obstacle) {
    if (obstacle.hit || _finished) {
      return;
    }
    obstacle.hit = true;
    obstacle.removeFromParent();

    final activeCombo = (_comboTimer > 0 ? combo.value + 1 : 1).clamp(1, 9);
    combo.value = activeCombo;
    _bestCombo = max(_bestCombo, activeCombo);
    _comboTimer = 1.35;

    objectsHit.value += 1;
    final damageGain = (obstacle.damage * _damageMultiplier).round().clamp(
      3,
      24,
    );
    damage.value = (damage.value + damageGain).clamp(0, 100);

    final points =
        ((obstacle.points + speedKmh.value * 3) * (1 + activeCombo * 0.18))
            .round();
    score.value += points;
    coins.value += max(1, points ~/ 95);

    _burst(obstacle.position.clone(), obstacle.isExplosive);

    if (damage.value >= 100) {
      _finish(levelComplete: false);
    }
  }

  void _spawnObstacle() {
    final roadWidth = min(size.x, 520.0);
    final left = (size.x - roadWidth) / 2 + 54;
    final right = size.x - left;
    final lanes = [
      left,
      left + (right - left) * 0.33,
      left + (right - left) * 0.66,
      right,
    ];
    final roll = _random.nextDouble();
    final ObstacleKind kind;
    if (roll < 0.26) {
      kind = ObstacleKind.crate;
    } else if (roll < 0.47) {
      kind = ObstacleKind.redBarrel;
    } else if (roll < 0.67) {
      kind = ObstacleKind.steelBarrel;
    } else if (roll < 0.86) {
      kind = ObstacleKind.cone;
    } else {
      kind = ObstacleKind.barricade;
    }

    add(
      ObstacleComponent(
        kind: kind,
        sprite: _spriteFor(kind),
        crashGame: this,
        position: Vector2(lanes[_random.nextInt(lanes.length)], -84),
      ),
    );
  }

  Sprite _spriteFor(ObstacleKind kind) {
    return switch (kind) {
      ObstacleKind.crate => _crateSprite,
      ObstacleKind.redBarrel => _redBarrelSprite,
      ObstacleKind.steelBarrel => _steelBarrelSprite,
      ObstacleKind.cone => _coneSprite,
      ObstacleKind.barricade => _barricadeSprite,
    };
  }

  void _burst(Vector2 at, bool explosive) {
    final count = explosive ? 16 : 10;
    for (var i = 0; i < count; i++) {
      final angle = -pi / 2 + (_random.nextDouble() - 0.5) * 2.4;
      final speed = 90 + _random.nextDouble() * (explosive ? 330 : 220);
      add(
        DebrisParticle(
          sprite: _debrisSprites[_random.nextInt(_debrisSprites.length)],
          position:
              at +
              Vector2(
                (_random.nextDouble() - 0.5) * 36,
                (_random.nextDouble() - 0.5) * 42,
              ),
          velocity:
              Vector2(cos(angle), sin(angle)) * speed +
              Vector2(0, _roadSpeed * 0.5),
          spin: (_random.nextDouble() - 0.5) * 10,
          scale: explosive
              ? 0.45 + _random.nextDouble() * 0.45
              : 0.36 + _random.nextDouble() * 0.32,
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
  RoadLayer({required this.sprite});

  final Sprite sprite;

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

    final sidePaint = Paint()..color = const Color(0xFF10191C);
    canvas.drawRect(Rect.fromLTWH(0, 0, left, gameSize.y), sidePaint);
    canvas.drawRect(
      Rect.fromLTWH(left + roadWidth, 0, left, gameSize.y),
      sidePaint,
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
    if (other is ObstacleComponent) {
      game.smash(other);
    }
  }
}

enum ObstacleKind { crate, redBarrel, steelBarrel, cone, barricade }

class ObstacleComponent extends SpriteComponent with CollisionCallbacks {
  ObstacleComponent({
    required this.kind,
    required super.sprite,
    required this.crashGame,
    required super.position,
  }) : super(anchor: Anchor.center, priority: 10);

  final ObstacleKind kind;
  final CrashCarGame crashGame;
  bool hit = false;

  int get damage {
    return switch (kind) {
      ObstacleKind.crate => 9,
      ObstacleKind.redBarrel => 15,
      ObstacleKind.steelBarrel => 12,
      ObstacleKind.cone => 5,
      ObstacleKind.barricade => 18,
    };
  }

  int get points {
    return switch (kind) {
      ObstacleKind.crate => 180,
      ObstacleKind.redBarrel => 260,
      ObstacleKind.steelBarrel => 220,
      ObstacleKind.cone => 90,
      ObstacleKind.barricade => 340,
    };
  }

  bool get isExplosive => kind == ObstacleKind.redBarrel;

  @override
  Future<void> onLoad() async {
    final base = switch (kind) {
      ObstacleKind.crate => Vector2(64, 64),
      ObstacleKind.redBarrel => Vector2(48, 62),
      ObstacleKind.steelBarrel => Vector2(48, 62),
      ObstacleKind.cone => Vector2(42, 52),
      ObstacleKind.barricade => Vector2(98, 70),
    };
    size = base * (0.88 + crashGame._random.nextDouble() * 0.28);
    angle = (crashGame._random.nextDouble() - 0.5) * 0.36;
    add(
      RectangleHitbox.relative(
        Vector2(0.72, 0.72),
        parentSize: size,
        anchor: Anchor.center,
      ),
    );
  }

  @override
  void update(double dt) {
    super.update(dt);
    y += crashGame._roadSpeed * dt;
    angle += (crashGame._random.nextDouble() - 0.5) * dt * 0.08;
    if (y > crashGame.size.y + 120) {
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
