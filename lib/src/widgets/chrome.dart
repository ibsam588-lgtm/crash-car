import 'dart:ui';

import 'package:flutter/material.dart';

class CrashBackground extends StatelessWidget {
  const CrashBackground({
    super.key,
    required this.child,
    this.image,
    this.dim = 0.7,
  });

  final Widget child;
  final String? image;
  final double dim;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          colors: [Color(0xFF071014), Color(0xFF0C1A20), Color(0xFF091013)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      child: Stack(
        fit: StackFit.expand,
        children: [
          if (image != null)
            Image.asset(image!, fit: BoxFit.cover, alignment: Alignment.center),
          if (image != null)
            ColoredBox(color: Colors.black.withValues(alpha: dim)),
          Positioned.fill(child: CustomPaint(painter: _GridPainter())),
          child,
        ],
      ),
    );
  }
}

class GlassPanel extends StatelessWidget {
  const GlassPanel({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(16),
    this.borderColor,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final Color? borderColor;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: const Color(0xFF0C171D).withValues(alpha: 0.76),
            borderRadius: BorderRadius.circular(8),
            border: Border.all(
              color: borderColor ?? Colors.white.withValues(alpha: 0.14),
            ),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.28),
                blurRadius: 24,
                offset: const Offset(0, 12),
              ),
            ],
          ),
          child: Padding(padding: padding, child: child),
        ),
      ),
    );
  }
}

class CrashButton extends StatelessWidget {
  const CrashButton({
    super.key,
    required this.label,
    required this.icon,
    required this.onPressed,
    this.primary = true,
  });

  final String label;
  final IconData icon;
  final VoidCallback? onPressed;
  final bool primary;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return FilledButton.icon(
      style: FilledButton.styleFrom(
        backgroundColor: primary ? colors.primary : const Color(0xFF17252C),
        foregroundColor: primary ? const Color(0xFF171307) : Colors.white,
        disabledBackgroundColor: const Color(0xFF253038),
        disabledForegroundColor: Colors.white38,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      ),
      onPressed: onPressed,
      icon: Icon(icon),
      label: Text(label),
    );
  }
}

class StatBar extends StatelessWidget {
  const StatBar({
    super.key,
    required this.label,
    required this.value,
    this.color = const Color(0xFFFFC533),
  });

  final String label;
  final double value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                color: Colors.white70,
                fontWeight: FontWeight.w700,
              ),
            ),
            Text(
              '${(value * 100).round()}%',
              style: const TextStyle(fontSize: 12, color: Colors.white70),
            ),
          ],
        ),
        const SizedBox(height: 6),
        ClipRRect(
          borderRadius: BorderRadius.circular(999),
          child: LinearProgressIndicator(
            minHeight: 8,
            value: value.clamp(0, 1),
            color: color,
            backgroundColor: Colors.white.withValues(alpha: 0.12),
          ),
        ),
      ],
    );
  }
}

class AppTopBar extends StatelessWidget {
  const AppTopBar({
    super.key,
    required this.title,
    required this.coins,
    this.onBack,
  });

  final String title;
  final int coins;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
        child: Row(
          children: [
            if (onBack != null)
              IconButton.filledTonal(
                tooltip: 'Back',
                onPressed: onBack,
                icon: const Icon(Icons.arrow_back),
              ),
            if (onBack != null) const SizedBox(width: 8),
            Expanded(
              child: Text(
                title,
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            GlassPanel(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              borderColor: const Color(0xFFFFC533).withValues(alpha: 0.5),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Image.asset(
                    'assets/images/icons/coin.png',
                    width: 22,
                    height: 22,
                  ),
                  const SizedBox(width: 6),
                  Text(
                    '$coins',
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _GridPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white.withValues(alpha: 0.035)
      ..strokeWidth = 1;
    for (double x = 0; x < size.width; x += 48) {
      canvas.drawLine(Offset(x, 0), Offset(x, size.height), paint);
    }
    for (double y = 0; y < size.height; y += 48) {
      canvas.drawLine(Offset(0, y), Offset(size.width, y), paint);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
