class GameResult {
  const GameResult({
    required this.score,
    required this.coins,
    required this.damagePercent,
    required this.objectsHit,
    required this.maxSpeed,
    required this.bestCombo,
    required this.levelComplete,
  });

  final int score;
  final int coins;
  final int damagePercent;
  final int objectsHit;
  final int maxSpeed;
  final int bestCombo;
  final bool levelComplete;
}
