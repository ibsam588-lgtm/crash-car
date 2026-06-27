import 'package:flutter/material.dart';

class CarSpec {
  const CarSpec({
    required this.id,
    required this.name,
    required this.asset,
    required this.price,
    required this.speed,
    required this.handling,
    required this.durability,
    required this.color,
  });

  final String id;
  final String name;
  final String asset;
  final int price;
  final double speed;
  final double handling;
  final double durability;
  final Color color;
}

const carSpecs = <CarSpec>[
  CarSpec(
    id: 'muscle_orange',
    name: 'Roadbreaker',
    asset: 'assets/images/cars/realistic_muscle_orange.png',
    price: 0,
    speed: 0.82,
    handling: 0.72,
    durability: 0.74,
    color: Color(0xFFFF6B1A),
  ),
  CarSpec(
    id: 'interceptor_blue',
    name: 'Interceptor',
    asset: 'assets/images/cars/realistic_interceptor_blue.png',
    price: 650,
    speed: 0.9,
    handling: 0.86,
    durability: 0.6,
    color: Color(0xFF28A8E8),
  ),
  CarSpec(
    id: 'rally_green',
    name: 'Rally Fang',
    asset: 'assets/images/cars/realistic_rally_green.png',
    price: 900,
    speed: 0.78,
    handling: 0.94,
    durability: 0.72,
    color: Color(0xFF6DCE3F),
  ),
  CarSpec(
    id: 'stunt_red',
    name: 'Stuntline',
    asset: 'assets/images/cars/realistic_stunt_red.png',
    price: 1200,
    speed: 0.96,
    handling: 0.68,
    durability: 0.88,
    color: Color(0xFFE64655),
  ),
];

CarSpec carById(String id) =>
    carSpecs.firstWhere((car) => car.id == id, orElse: () => carSpecs.first);
