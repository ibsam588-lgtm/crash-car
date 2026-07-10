import 'dart:math';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';

enum ProceduralVehicleStyle { muscle, sport, rally, sedan, truck, bus }

ProceduralVehicleStyle proceduralStyleForCarId(String carId) {
  return switch (carId) {
    'interceptor_blue' => ProceduralVehicleStyle.sport,
    'rally_green' => ProceduralVehicleStyle.rally,
    'stunt_red' => ProceduralVehicleStyle.muscle,
    _ => ProceduralVehicleStyle.muscle,
  };
}

double proceduralVehicleHeightRatio(ProceduralVehicleStyle style) {
  return switch (style) {
    ProceduralVehicleStyle.truck => 0.74,
    ProceduralVehicleStyle.bus => 0.78,
    _ => 0.72,
  };
}

class ProceduralVehiclePainter extends CustomPainter {
  const ProceduralVehiclePainter({
    required this.style,
    required this.color,
    this.wheelSpin = 0,
    this.damage = 0,
    this.angle = 0,
    this.steer = 0,
    this.opacity = 1,
    this.brakeLights = false,
  });

  final ProceduralVehicleStyle style;
  final Color color;
  final double wheelSpin;
  final double damage;
  final double angle;
  final double steer;
  final double opacity;
  final bool brakeLights;

  @override
  void paint(Canvas canvas, Size size) {
    final ratio = proceduralVehicleHeightRatio(style);
    final width = min(size.width * 0.9, size.height * 0.92 / ratio);
    final height = width * ratio;
    drawProceduralVehicle(
      canvas,
      contact: Offset(size.width * 0.5, size.height * 0.86),
      size: Size(width, height),
      style: style,
      color: color,
      wheelSpin: wheelSpin,
      damage: damage,
      angle: angle,
      steer: steer,
      opacity: opacity,
      brakeLights: brakeLights,
      shadowAlpha: 0.34,
    );
  }

  @override
  bool shouldRepaint(covariant ProceduralVehiclePainter oldDelegate) {
    return oldDelegate.style != style ||
        oldDelegate.color != color ||
        oldDelegate.wheelSpin != wheelSpin ||
        oldDelegate.damage != damage ||
        oldDelegate.angle != angle ||
        oldDelegate.steer != steer ||
        oldDelegate.opacity != opacity ||
        oldDelegate.brakeLights != brakeLights;
  }
}

void drawProceduralVehicle(
  Canvas canvas, {
  required Offset contact,
  required Size size,
  required ProceduralVehicleStyle style,
  required Color color,
  double angle = 0,
  double wheelSpin = 0,
  double damage = 0,
  double steer = 0,
  double lift = 0,
  double opacity = 1,
  double shadowAlpha = 0.36,
  bool brakeLights = false,
}) {
  final alpha = opacity.clamp(0.0, 1.0).toDouble();
  if (alpha <= 0) {
    return;
  }
  final safeWidth = max(1.0, size.width);
  final safeHeight = max(1.0, size.height);
  _drawVehicleShadow(
    canvas,
    contact,
    safeWidth,
    safeHeight,
    shadowAlpha * alpha * (1 - (lift / 72).clamp(0.0, 0.55)),
  );

  canvas.save();
  canvas.translate(contact.dx, contact.dy - lift);
  canvas.rotate(angle);
  canvas.scale(safeWidth / 220, safeHeight / 132);
  if (style == ProceduralVehicleStyle.truck ||
      style == ProceduralVehicleStyle.bus) {
    _drawHeavyVehicleLocal(
      canvas,
      style: style,
      color: color,
      wheelSpin: wheelSpin,
      damage: damage,
      opacity: alpha,
      brakeLights: brakeLights,
    );
  } else {
    _drawCarLocal(
      canvas,
      style: style,
      color: color,
      wheelSpin: wheelSpin,
      damage: damage,
      steer: steer,
      opacity: alpha,
      brakeLights: brakeLights,
    );
  }
  canvas.restore();
}

void _drawVehicleShadow(
  Canvas canvas,
  Offset contact,
  double width,
  double height,
  double alpha,
) {
  final shadowPaint = Paint()
    ..color = Colors.black.withValues(alpha: alpha.clamp(0.0, 0.6).toDouble())
    ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 10);
  canvas.drawOval(
    Rect.fromCenter(
      center: Offset(contact.dx, contact.dy + height * 0.03),
      width: width * 0.74,
      height: max(8, height * 0.14),
    ),
    shadowPaint,
  );
}

void _drawCarLocal(
  Canvas canvas, {
  required ProceduralVehicleStyle style,
  required Color color,
  required double wheelSpin,
  required double damage,
  required double steer,
  required double opacity,
  required bool brakeLights,
}) {
  final bodyTop = _mix(color, Colors.white, 0.28);
  final bodySide = _mix(color, Colors.black, 0.2);
  final bodyDark = _mix(color, Colors.black, 0.48);
  final rimAccent = style == ProceduralVehicleStyle.rally
      ? const Color(0xFFE8FFF1)
      : const Color(0xFFECEFF2);
  final wheelY = style == ProceduralVehicleStyle.sport ? -20.0 : -18.0;
  _drawWheel(
    canvas,
    center: const Offset(-72, -20),
    size: const Size(44, 54),
    spin: wheelSpin,
    opacity: opacity,
    accent: rimAccent,
  );
  _drawWheel(
    canvas,
    center: Offset(70 + steer * 3, wheelY),
    size: const Size(42, 52),
    spin: wheelSpin + pi * 0.2,
    opacity: opacity,
    accent: rimAccent,
  );
  _drawWheel(
    canvas,
    center: const Offset(55, -55),
    size: const Size(26, 34),
    spin: wheelSpin + pi * 0.4,
    opacity: opacity * 0.7,
    accent: rimAccent,
  );

  final lowerShadow = Path()
    ..moveTo(-92, -16)
    ..lineTo(-66, -8)
    ..lineTo(72, -8)
    ..lineTo(96, -20)
    ..quadraticBezierTo(82, -5, 48, 2)
    ..lineTo(-50, 2)
    ..quadraticBezierTo(-78, -2, -92, -16)
    ..close();
  canvas.drawPath(
    lowerShadow,
    Paint()..color = Colors.black.withValues(alpha: 0.44 * opacity),
  );

  final hull = Path()
    ..moveTo(-96, -22)
    ..lineTo(-88, -58)
    ..quadraticBezierTo(-79, -83, -56, -97)
    ..lineTo(-23, -118)
    ..quadraticBezierTo(4, -128, 44, -121)
    ..lineTo(70, -103)
    ..quadraticBezierTo(91, -83, 98, -30)
    ..lineTo(81, -12)
    ..lineTo(-72, -12)
    ..close();
  canvas.drawPath(
    hull,
    Paint()
      ..shader = ui.Gradient.linear(
        const Offset(-84, -112),
        const Offset(98, -12),
        [
          _withAlpha(bodyTop, opacity),
          _withAlpha(color, opacity),
          _withAlpha(bodyDark, opacity),
        ],
        const [0, 0.54, 1],
      ),
  );

  final roofMass = Path()
    ..moveTo(-39, -111)
    ..quadraticBezierTo(-12, -125, 35, -116)
    ..lineTo(58, -96)
    ..lineTo(43, -75)
    ..lineTo(-45, -75)
    ..lineTo(-57, -95)
    ..close();
  canvas.drawPath(
    roofMass,
    Paint()
      ..shader = ui.Gradient.linear(
        const Offset(-54, -116),
        const Offset(59, -76),
        [
          Colors.black.withValues(alpha: 0.74 * opacity),
          _withAlpha(_mix(color, Colors.black, 0.52), opacity),
        ],
      ),
  );

  final sidePanel = Path()
    ..moveTo(8, -103)
    ..cubicTo(34, -102, 64, -87, 78, -66)
    ..lineTo(92, -25)
    ..lineTo(52, -16)
    ..lineTo(32, -65)
    ..close();
  canvas.drawPath(
    sidePanel,
    Paint()
      ..shader = ui.Gradient.linear(
        const Offset(18, -98),
        const Offset(95, -20),
        [
          _withAlpha(_mix(color, Colors.white, 0.14), opacity),
          _withAlpha(bodySide, opacity),
        ],
      ),
  );

  final rearQuarter = Path()
    ..moveTo(-82, -58)
    ..quadraticBezierTo(-74, -82, -52, -92)
    ..lineTo(-30, -73)
    ..lineTo(-52, -28)
    ..lineTo(-88, -25)
    ..close();
  canvas.drawPath(
    rearQuarter,
    Paint()
      ..shader = ui.Gradient.linear(
        const Offset(-86, -91),
        const Offset(-36, -24),
        [
          _withAlpha(_mix(color, Colors.white, 0.18), opacity),
          _withAlpha(_mix(color, Colors.black, 0.34), opacity),
        ],
      ),
  );

  final deck = RRect.fromRectAndRadius(
    const Rect.fromLTRB(-64, -72, 73, -29),
    const Radius.circular(12),
  );
  canvas.drawRRect(
    deck,
    Paint()..color = _withAlpha(_mix(color, Colors.black, 0.12), opacity),
  );

  final leftWell = Rect.fromCenter(
    center: const Offset(-67, -21),
    width: 49,
    height: 55,
  );
  final rightWell = Rect.fromCenter(
    center: const Offset(69, -21),
    width: 47,
    height: 54,
  );
  final wellPaint = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = 8
    ..strokeCap = StrokeCap.round
    ..color = Colors.black.withValues(alpha: 0.62 * opacity);
  canvas.drawArc(leftWell, pi * 0.08, pi * 0.84, false, wellPaint);
  canvas.drawArc(rightWell, pi * 0.08, pi * 0.84, false, wellPaint);

  final rearGlass = RRect.fromRectAndRadius(
    const Rect.fromLTRB(-45, -108, 43, -73),
    const Radius.circular(10),
  );
  final cabinTrim = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = 3
    ..strokeJoin = StrokeJoin.round
    ..color = Colors.black.withValues(alpha: 0.58 * opacity);
  canvas.drawRRect(rearGlass.inflate(2), cabinTrim);
  _drawGlass(canvas, rearGlass, opacity);
  _drawGlassCracks(canvas, rearGlass.outerRect, damage, opacity);

  final sideGlass = Path()
    ..moveTo(42, -104)
    ..quadraticBezierTo(66, -96, 76, -78)
    ..lineTo(51, -72)
    ..lineTo(22, -95)
    ..close();
  _drawGlassPath(canvas, sideGlass, opacity * 0.88);

  final panelStroke = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = 2.2
    ..strokeCap = StrokeCap.round
    ..color = Colors.white.withValues(alpha: 0.24 * opacity);
  canvas.drawPath(hull, panelStroke);
  canvas.drawLine(
    const Offset(-55, -70),
    const Offset(-62, -22),
    panelStroke..color = Colors.black.withValues(alpha: 0.22 * opacity),
  );
  canvas.drawLine(
    const Offset(36, -66),
    const Offset(55, -18),
    panelStroke,
  );

  if (style == ProceduralVehicleStyle.muscle ||
      style == ProceduralVehicleStyle.sport) {
    final stripePaint = Paint()
      ..color = Colors.black.withValues(alpha: 0.42 * opacity)
      ..strokeWidth = style == ProceduralVehicleStyle.sport ? 5 : 7
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(
        const Offset(-15, -116), const Offset(-14, -18), stripePaint);
    canvas.drawLine(const Offset(7, -116), const Offset(10, -18), stripePaint);
    final clearCoat = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3
      ..strokeCap = StrokeCap.round
      ..color = Colors.white.withValues(alpha: 0.24 * opacity);
    canvas.drawLine(const Offset(-73, -67), const Offset(-28, -95), clearCoat);
    canvas.drawLine(const Offset(30, -95), const Offset(79, -58), clearCoat);
  }

  if (style == ProceduralVehicleStyle.rally) {
    final stripePaint = Paint()
      ..color = Colors.white.withValues(alpha: 0.72 * opacity)
      ..strokeWidth = 5
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(const Offset(-72, -61), const Offset(80, -42), stripePaint);
    canvas.drawLine(
      const Offset(-78, -50),
      const Offset(72, -31),
      stripePaint
        ..color = const Color(0xFF1A2026).withValues(alpha: 0.48 * opacity),
    );
  }

  final bumper = RRect.fromRectAndRadius(
    const Rect.fromLTRB(-74, -34, 79, -14),
    const Radius.circular(8),
  );
  canvas.drawRRect(
    bumper,
    Paint()..color = Colors.black.withValues(alpha: 0.42 * opacity),
  );
  final diffuserPaint = Paint()
    ..color = const Color(0xFF050607).withValues(alpha: 0.72 * opacity);
  canvas.drawRRect(
    RRect.fromRectAndRadius(
      const Rect.fromLTRB(-60, -19, 68, -8),
      const Radius.circular(5),
    ),
    diffuserPaint,
  );
  canvas.drawRRect(
    RRect.fromRectAndRadius(
      const Rect.fromLTRB(-22, -31, 22, -18),
      const Radius.circular(4),
    ),
    Paint()..color = const Color(0xFFE8E2C9).withValues(alpha: 0.76 * opacity),
  );

  final lightAlpha = brakeLights ? 1.0 : 0.72;
  _drawTailLight(
      canvas, const Rect.fromLTRB(-68, -47, -45, -34), opacity * lightAlpha);
  _drawTailLight(
      canvas, const Rect.fromLTRB(48, -47, 72, -34), opacity * lightAlpha);
  final exhaustPaint = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = 4
    ..strokeCap = StrokeCap.round
    ..color = const Color(0xFFD3D6D8).withValues(alpha: 0.8 * opacity);
  canvas.drawLine(const Offset(-56, -12), const Offset(-42, -12), exhaustPaint);
  canvas.drawLine(const Offset(46, -12), const Offset(61, -12), exhaustPaint);
  if (brakeLights) {
    _drawGlow(
        canvas, const Offset(-56, -40), 25, const Color(0xFFFF2D30), opacity);
    _drawGlow(
        canvas, const Offset(60, -40), 25, const Color(0xFFFF2D30), opacity);
  }

  if (style == ProceduralVehicleStyle.sport) {
    final wing = Paint()
      ..color = Colors.black.withValues(alpha: 0.5 * opacity)
      ..strokeWidth = 6
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(const Offset(-72, -83), const Offset(74, -82), wing);
  }

  _drawDamage(canvas, damage, opacity, heavy: false);
}

void _drawHeavyVehicleLocal(
  Canvas canvas, {
  required ProceduralVehicleStyle style,
  required Color color,
  required double wheelSpin,
  required double damage,
  required double opacity,
  required bool brakeLights,
}) {
  final isBus = style == ProceduralVehicleStyle.bus;
  final body = isBus
      ? const Rect.fromLTRB(-99, -116, 99, -18)
      : const Rect.fromLTRB(-94, -120, 96, -20);
  final baseColor = isBus ? color : const Color(0xFFF1F3F0);
  final sideColor =
      isBus ? _mix(color, Colors.black, 0.18) : const Color(0xFFC9D0D0);

  for (final wheel in [
    const Offset(-72, -18),
    const Offset(-36, -17),
    const Offset(44, -17),
    const Offset(78, -18),
  ]) {
    _drawWheel(
      canvas,
      center: wheel,
      size: const Size(34, 44),
      spin: wheelSpin + wheel.dx * 0.01,
      opacity: opacity,
      accent: const Color(0xFFEDEFF2),
    );
  }

  final chassis = RRect.fromRectAndRadius(
    Rect.fromLTRB(body.left - 3, -30, body.right + 2, -10),
    const Radius.circular(5),
  );
  canvas.drawRRect(
    chassis,
    Paint()..color = Colors.black.withValues(alpha: 0.54 * opacity),
  );

  final bodyPath = Path()
    ..moveTo(body.left, body.top + 10)
    ..quadraticBezierTo(body.left + 5, body.top, body.left + 24, body.top)
    ..lineTo(body.right - 20, body.top + (isBus ? 4 : 0))
    ..quadraticBezierTo(body.right, body.top + 8, body.right, body.top + 31)
    ..lineTo(body.right - 7, body.bottom)
    ..lineTo(body.left + 7, body.bottom)
    ..quadraticBezierTo(body.left, body.bottom - 4, body.left, body.bottom - 18)
    ..close();
  canvas.drawPath(
    bodyPath,
    Paint()
      ..shader = ui.Gradient.linear(
        Offset(body.left, body.top),
        Offset(body.right, body.bottom),
        [
          _withAlpha(_mix(baseColor, Colors.white, 0.28), opacity),
          _withAlpha(baseColor, opacity),
          _withAlpha(sideColor, opacity),
        ],
        const [0, 0.5, 1],
      ),
  );

  if (isBus) {
    final windowPaint = Paint()
      ..shader = ui.Gradient.linear(
        const Offset(-85, -102),
        const Offset(82, -58),
        [
          Colors.white.withValues(alpha: 0.76 * opacity),
          const Color(0xFF154D5F).withValues(alpha: 0.88 * opacity),
        ],
      );
    for (var i = 0; i < 5; i++) {
      final x = -79 + i * 35;
      canvas.drawRRect(
        RRect.fromRectAndRadius(
          Rect.fromLTWH(x.toDouble(), -96, 26, 28),
          const Radius.circular(4),
        ),
        windowPaint,
      );
    }
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        const Rect.fromLTRB(-88, -58, 88, -42),
        const Radius.circular(5),
      ),
      Paint()..color = Colors.black.withValues(alpha: 0.28 * opacity),
    );
  } else {
    final cargoLines = Paint()
      ..color = Colors.black.withValues(alpha: 0.18 * opacity)
      ..strokeWidth = 2;
    for (var i = 0; i < 5; i++) {
      final y = -106 + i * 16;
      canvas.drawLine(
          Offset(-84, y.toDouble()), Offset(86, y.toDouble()), cargoLines);
    }
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        const Rect.fromLTRB(-82, -112, 85, -44),
        const Radius.circular(6),
      ),
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2
        ..color = Colors.black.withValues(alpha: 0.22 * opacity),
    );
  }

  final glassRect = isBus
      ? const Rect.fromLTRB(-39, -108, 38, -66)
      : const Rect.fromLTRB(-45, -118, 44, -82);
  _drawGlass(
    canvas,
    RRect.fromRectAndRadius(glassRect, const Radius.circular(8)),
    opacity,
  );
  _drawGlassCracks(canvas, glassRect, damage, opacity);

  final trim = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = 2.4
    ..color = Colors.white.withValues(alpha: 0.22 * opacity);
  canvas.drawPath(bodyPath, trim);

  _drawTailLight(
      canvas, const Rect.fromLTRB(-87, -39, -70, -27), opacity * 0.78);
  _drawTailLight(canvas, const Rect.fromLTRB(70, -39, 88, -27), opacity * 0.78);
  if (brakeLights) {
    _drawGlow(
        canvas, const Offset(-78, -33), 24, const Color(0xFFFF2D30), opacity);
    _drawGlow(
        canvas, const Offset(79, -33), 24, const Color(0xFFFF2D30), opacity);
  }

  _drawDamage(canvas, damage, opacity, heavy: true);
}

void _drawWheel(
  Canvas canvas, {
  required Offset center,
  required Size size,
  required double spin,
  required double opacity,
  required Color accent,
}) {
  canvas.save();
  canvas.translate(center.dx, center.dy);
  canvas.scale(size.width / size.height, 1);
  final radius = size.height * 0.5;
  canvas.drawCircle(
    Offset.zero,
    radius,
    Paint()..color = const Color(0xFF080A0B).withValues(alpha: 0.96 * opacity),
  );
  canvas.drawCircle(
    Offset.zero,
    radius * 0.72,
    Paint()
      ..shader = ui.Gradient.radial(
        Offset.zero,
        radius * 0.72,
        [
          accent.withValues(alpha: 0.88 * opacity),
          const Color(0xFF515A60).withValues(alpha: 0.92 * opacity),
          const Color(0xFF171C20).withValues(alpha: 0.92 * opacity),
        ],
        const [0, 0.48, 1],
      ),
  );
  canvas.drawCircle(
    Offset.zero,
    radius * 0.27,
    Paint()..color = const Color(0xFF11171C).withValues(alpha: opacity),
  );
  final spokePaint = Paint()
    ..color = Colors.white.withValues(alpha: 0.72 * opacity)
    ..strokeWidth = 2.1
    ..strokeCap = StrokeCap.round;
  for (var i = 0; i < 6; i++) {
    final a = spin + i * pi / 3;
    final p0 = Offset(cos(a), sin(a)) * radius * 0.26;
    final p1 = Offset(cos(a), sin(a)) * radius * 0.68;
    canvas.drawLine(p0, p1, spokePaint);
  }
  final blurPaint = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = 2.7
    ..color = Colors.white.withValues(alpha: 0.12 * opacity);
  canvas.drawArc(
    Rect.fromCircle(center: Offset.zero, radius: radius * 0.83),
    spin,
    pi * 1.45,
    false,
    blurPaint,
  );
  canvas.restore();
}

void _drawGlass(Canvas canvas, RRect rect, double opacity) {
  canvas.drawRRect(
    rect,
    Paint()
      ..shader = ui.Gradient.linear(
        rect.outerRect.topLeft,
        rect.outerRect.bottomRight,
        [
          Colors.white.withValues(alpha: 0.72 * opacity),
          const Color(0xFF7EDBFF).withValues(alpha: 0.5 * opacity),
          const Color(0xFF0D2C38).withValues(alpha: 0.82 * opacity),
        ],
        const [0, 0.42, 1],
      ),
  );
  canvas.drawRRect(
    rect,
    Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2
      ..color = Colors.white.withValues(alpha: 0.34 * opacity),
  );
}

void _drawGlassPath(Canvas canvas, Path path, double opacity) {
  canvas.drawPath(
    path,
    Paint()
      ..shader = ui.Gradient.linear(
        const Offset(20, -108),
        const Offset(78, -70),
        [
          Colors.white.withValues(alpha: 0.54 * opacity),
          const Color(0xFF0E3441).withValues(alpha: 0.82 * opacity),
        ],
      ),
  );
  canvas.drawPath(
    path,
    Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.5
      ..color = Colors.white.withValues(alpha: 0.24 * opacity),
  );
}

void _drawGlassCracks(
  Canvas canvas,
  Rect rect,
  double damage,
  double opacity,
) {
  if (damage < 0.16) {
    return;
  }
  final crack = Paint()
    ..color = Colors.white.withValues(alpha: min(0.78, damage) * opacity)
    ..strokeWidth = 1.25
    ..strokeCap = StrokeCap.round;
  final center = Offset(
    rect.left + rect.width * (0.44 + damage * 0.12),
    rect.top + rect.height * 0.48,
  );
  canvas.save();
  canvas.clipRRect(RRect.fromRectAndRadius(rect, const Radius.circular(7)));
  for (var i = 0; i < 7; i++) {
    final angle = -2.6 + i * 0.86 + sin(i * 2.1) * 0.18;
    final length = rect.shortestSide * (0.18 + damage * (0.34 + i * 0.035));
    final end = center + Offset(cos(angle), sin(angle)) * length;
    canvas.drawLine(center, end, crack);
    if (i.isEven) {
      final branch = end -
          Offset(
                cos(angle + 0.74),
                sin(angle + 0.74),
              ) *
              length *
              0.34;
      canvas.drawLine(end, branch, crack..strokeWidth = 0.9);
      crack.strokeWidth = 1.25;
    }
  }
  canvas.drawCircle(
    center,
    3.2 + damage * 2.8,
    Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.2
      ..color = Colors.white.withValues(alpha: 0.45 * opacity),
  );
  canvas.restore();
}

void _drawTailLight(Canvas canvas, Rect rect, double opacity) {
  canvas.drawRRect(
    RRect.fromRectAndRadius(rect, const Radius.circular(5)),
    Paint()
      ..shader = ui.Gradient.linear(
        rect.topLeft,
        rect.bottomRight,
        [
          const Color(0xFFFFF0A3).withValues(alpha: 0.65 * opacity),
          const Color(0xFFFF2D30).withValues(alpha: 0.95 * opacity),
          const Color(0xFF6B0608).withValues(alpha: 0.9 * opacity),
        ],
        const [0, 0.48, 1],
      ),
  );
}

void _drawGlow(
  Canvas canvas,
  Offset center,
  double radius,
  Color color,
  double opacity,
) {
  canvas.drawCircle(
    center,
    radius,
    Paint()
      ..shader = ui.Gradient.radial(
        center,
        radius,
        [
          color.withValues(alpha: 0.32 * opacity),
          color.withValues(alpha: 0),
        ],
      ),
  );
}

void _drawDamage(
  Canvas canvas,
  double damage,
  double opacity, {
  required bool heavy,
}) {
  final d = damage.clamp(0.0, 1.0).toDouble();
  if (d <= 0.02) {
    return;
  }
  final scrapePaint = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = heavy ? 2.3 : 1.8
    ..strokeCap = StrokeCap.round
    ..color = Colors.white.withValues(alpha: 0.22 * d * opacity);
  final darkPaint = Paint()
    ..style = PaintingStyle.stroke
    ..strokeWidth = heavy ? 3.2 : 2.5
    ..strokeCap = StrokeCap.round
    ..color = Colors.black.withValues(alpha: 0.38 * d * opacity);

  final scrapeCount = 2 + (d * 7).round();
  for (var i = 0; i < scrapeCount; i++) {
    final x = -70 + ((i * 37) % 145).toDouble();
    final y = -75 + ((i * 23) % 54).toDouble();
    final drift = 12 + d * 25 + (i.isEven ? 6 : -3);
    canvas.drawLine(Offset(x, y), Offset(x + drift, y + 5 + i % 3), darkPaint);
    canvas.drawLine(
      Offset(x + 1, y - 1),
      Offset(x + drift * 0.82, y + 3 + i % 3),
      scrapePaint,
    );
  }

  if (d > 0.38) {
    final dent = Path()
      ..moveTo(40, -67)
      ..lineTo(70 + d * 16, -58)
      ..lineTo(78 + d * 18, -32)
      ..lineTo(55, -22)
      ..lineTo(42 - d * 12, -42)
      ..close();
    canvas.drawPath(
      dent,
      Paint()..color = Colors.black.withValues(alpha: 0.24 * d * opacity),
    );
    canvas.drawPath(
      dent,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2
        ..color = Colors.white.withValues(alpha: 0.18 * d * opacity),
    );
  }

  if (d > 0.58) {
    final crumple = Path()
      ..moveTo(-78, -31)
      ..lineTo(-60, -40 - d * 10)
      ..lineTo(-38, -29 + d * 4)
      ..lineTo(-18, -39 - d * 7)
      ..lineTo(8, -27)
      ..lineTo(28, -35 + d * 3)
      ..lineTo(51, -24)
      ..lineTo(64, -14)
      ..lineTo(-66, -13)
      ..close();
    canvas.drawPath(
      crumple,
      Paint()..color = Colors.black.withValues(alpha: 0.3 * d * opacity),
    );
    canvas.drawPath(
      crumple,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2.4
        ..color = Colors.white.withValues(alpha: 0.2 * d * opacity),
    );
  }
}

Color _withAlpha(Color color, double opacity) {
  return color.withValues(alpha: opacity.clamp(0.0, 1.0).toDouble());
}

Color _mix(Color a, Color b, double t) {
  return Color.lerp(a, b, t.clamp(0.0, 1.0).toDouble())!;
}
