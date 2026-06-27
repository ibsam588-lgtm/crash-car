import 'package:flutter/material.dart';

class ArenaSpec {
  const ArenaSpec({
    required this.id,
    required this.name,
    required this.subtitle,
    required this.description,
    required this.icon,
    required this.primary,
    required this.roadTint,
    required this.sideTint,
    required this.trafficDensity,
    required this.sceneryDensity,
    required this.speedBonus,
    required this.scoreBonus,
  });

  final String id;
  final String name;
  final String subtitle;
  final String description;
  final IconData icon;
  final Color primary;
  final Color roadTint;
  final Color sideTint;
  final double trafficDensity;
  final double sceneryDensity;
  final double speedBonus;
  final double scoreBonus;
}

const arenaSpecs = <ArenaSpec>[
  ArenaSpec(
    id: 'construction_yard',
    name: 'Construction Yard',
    subtitle: 'Crates, barrels, cones',
    description:
        'Balanced arena with breakable work-zone props and medium traffic.',
    icon: Icons.construction_rounded,
    primary: Color(0xFFFFC533),
    roadTint: Color(0xFF565A5D),
    sideTint: Color(0xFF10191C),
    trafficDensity: 0.72,
    sceneryDensity: 0.52,
    speedBonus: 0,
    scoreBonus: 1,
  ),
  ArenaSpec(
    id: 'downtown_strip',
    name: 'Downtown Strip',
    subtitle: 'Cars, trucks, shops',
    description:
        'Busy storefronts and aggressive traffic. Great for combo crashes.',
    icon: Icons.store_mall_directory_rounded,
    primary: Color(0xFF42C7FF),
    roadTint: Color(0xFF46535E),
    sideTint: Color(0xFF111920),
    trafficDensity: 1.15,
    sceneryDensity: 0.95,
    speedBonus: 0.04,
    scoreBonus: 1.18,
  ),
  ArenaSpec(
    id: 'industrial_docks',
    name: 'Industrial Docks',
    subtitle: 'Heavy trucks, hard hits',
    description:
        'Heavy vehicles and tougher impacts. Fewer targets, bigger payouts.',
    icon: Icons.local_shipping_rounded,
    primary: Color(0xFFFF7A30),
    roadTint: Color(0xFF4A4D4E),
    sideTint: Color(0xFF151719),
    trafficDensity: 0.86,
    sceneryDensity: 0.68,
    speedBonus: 0.02,
    scoreBonus: 1.32,
  ),
  ArenaSpec(
    id: 'night_market',
    name: 'Night Market',
    subtitle: 'Dense stalls, narrow lanes',
    description:
        'Shop stalls and moving traffic pack the road for quick collision chains.',
    icon: Icons.nightlife_rounded,
    primary: Color(0xFF7CD957),
    roadTint: Color(0xFF3E4C47),
    sideTint: Color(0xFF0E1A16),
    trafficDensity: 1.02,
    sceneryDensity: 1.18,
    speedBonus: -0.02,
    scoreBonus: 1.24,
  ),
];

ArenaSpec arenaById(String id) => arenaSpecs.firstWhere(
  (arena) => arena.id == id,
  orElse: () => arenaSpecs.first,
);
