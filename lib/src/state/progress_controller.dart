import 'dart:convert';
import 'dart:math';

import 'package:crash_car/src/models/car_spec.dart';
import 'package:crash_car/src/models/game_result.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

final progressProvider = NotifierProvider<GameProgressController, GameProgress>(
  GameProgressController.new,
);

class GameProgress {
  const GameProgress({
    required this.coins,
    required this.highScore,
    required this.selectedCarId,
    required this.unlockedCarIds,
    required this.upgradeLevels,
    required this.loaded,
    this.lastResult,
  });

  factory GameProgress.initial() => const GameProgress(
    coins: 250,
    highScore: 0,
    selectedCarId: 'muscle_orange',
    unlockedCarIds: {'muscle_orange'},
    upgradeLevels: {},
    loaded: false,
  );

  final int coins;
  final int highScore;
  final String selectedCarId;
  final Set<String> unlockedCarIds;
  final Map<String, int> upgradeLevels;
  final bool loaded;
  final GameResult? lastResult;

  CarSpec get selectedCar => carById(selectedCarId);

  int upgradeLevel(String carId) => upgradeLevels[carId] ?? 0;

  GameProgress copyWith({
    int? coins,
    int? highScore,
    String? selectedCarId,
    Set<String>? unlockedCarIds,
    Map<String, int>? upgradeLevels,
    bool? loaded,
    GameResult? lastResult,
  }) {
    return GameProgress(
      coins: coins ?? this.coins,
      highScore: highScore ?? this.highScore,
      selectedCarId: selectedCarId ?? this.selectedCarId,
      unlockedCarIds: unlockedCarIds ?? this.unlockedCarIds,
      upgradeLevels: upgradeLevels ?? this.upgradeLevels,
      loaded: loaded ?? this.loaded,
      lastResult: lastResult ?? this.lastResult,
    );
  }
}

class GameProgressController extends Notifier<GameProgress> {
  SharedPreferences? _prefs;

  @override
  GameProgress build() => GameProgress.initial();

  Future<void> load() async {
    _prefs = await SharedPreferences.getInstance();
    final unlocked =
        _prefs!.getStringList('unlockedCarIds')?.toSet() ?? {'muscle_orange'};
    unlocked.add('muscle_orange');

    final upgradeJson = _prefs!.getString('upgradeLevels');
    final upgrades = <String, int>{};
    if (upgradeJson != null) {
      final decoded = jsonDecode(upgradeJson) as Map<String, dynamic>;
      for (final entry in decoded.entries) {
        upgrades[entry.key] = (entry.value as num).toInt();
      }
    }

    final selected = _prefs!.getString('selectedCarId') ?? 'muscle_orange';
    state = GameProgress(
      coins: _prefs!.getInt('coins') ?? 250,
      highScore: _prefs!.getInt('highScore') ?? 0,
      selectedCarId: unlocked.contains(selected) ? selected : 'muscle_orange',
      unlockedCarIds: unlocked,
      upgradeLevels: upgrades,
      loaded: true,
    );
  }

  Future<void> _save() async {
    final prefs = _prefs ??= await SharedPreferences.getInstance();
    await prefs.setInt('coins', state.coins);
    await prefs.setInt('highScore', state.highScore);
    await prefs.setString('selectedCarId', state.selectedCarId);
    await prefs.setStringList(
      'unlockedCarIds',
      state.unlockedCarIds.toList()..sort(),
    );
    await prefs.setString('upgradeLevels', jsonEncode(state.upgradeLevels));
  }

  Future<void> selectCar(String carId) async {
    if (!state.unlockedCarIds.contains(carId)) {
      return;
    }
    state = state.copyWith(selectedCarId: carId);
    await _save();
  }

  Future<bool> unlockCar(String carId) async {
    final car = carById(carId);
    if (state.unlockedCarIds.contains(carId) || state.coins < car.price) {
      return false;
    }
    state = state.copyWith(
      coins: state.coins - car.price,
      selectedCarId: carId,
      unlockedCarIds: {...state.unlockedCarIds, carId},
    );
    await _save();
    return true;
  }

  int upgradeCost(String carId) {
    final level = state.upgradeLevel(carId);
    return 180 + level * 140;
  }

  Future<bool> upgradeSelectedCar() async {
    final carId = state.selectedCarId;
    final current = state.upgradeLevel(carId);
    if (current >= 5) {
      return false;
    }
    final cost = upgradeCost(carId);
    if (state.coins < cost) {
      return false;
    }
    state = state.copyWith(
      coins: state.coins - cost,
      upgradeLevels: {...state.upgradeLevels, carId: current + 1},
    );
    await _save();
    return true;
  }

  Future<void> claimSponsorBonus() async {
    state = state.copyWith(coins: state.coins + 75);
    await _save();
  }

  Future<void> recordResult(GameResult result) async {
    state = state.copyWith(
      coins: state.coins + result.coins,
      highScore: max(state.highScore, result.score),
      lastResult: result,
    );
    await _save();
  }
}
