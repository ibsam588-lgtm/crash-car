# GT650 Mid-Engine Supercar Concept

Original vehicle concept for Car Crash. This is a fictional, brand-neutral 2026 mid-engine supercar designed for realistic arcade crash gameplay.

## Design Intent

- Type: modern street-legal mid-engine supercar
- Target performance: 650 hp, rear-wheel drive, 7-speed dual clutch
- Mood: aggressive, premium, believable, manufacturable
- Proportions: low wide coupe, short nose, cab-forward cockpit, muscular rear haunches
- Signature shapes: large side intakes, slim LED headlights, active rear wing, exposed carbon splitter and diffuser

## Reference Sheets

- `assets/images/concepts/gt650_mid_engine/front_3q.png`
- `assets/images/concepts/gt650_mid_engine/rear_3q.png`
- `assets/images/concepts/gt650_mid_engine/side_profile.png`
- `assets/images/concepts/gt650_mid_engine/orthographic_blueprint.png`
- `assets/images/concepts/gt650_mid_engine/interior_cockpit.png`
- `assets/images/concepts/gt650_mid_engine/wheel_headlight_details.png`

## Suggested Dimensions

- Length: 4.55 m
- Width: 2.02 m including body, 2.15 m with mirrors
- Height: 1.18 m
- Wheelbase: 2.70 m
- Front track: 1.70 m
- Rear track: 1.76 m
- Ground clearance: 105 mm street ride height
- Wheels: 20 x 9.5 front, 21 x 12 rear
- Tires: 265/30 R20 front, 325/25 R21 rear

## Model Breakdown

Separate meshes for gameplay and damage:

- Body shell
- Front bumper and carbon splitter
- Hood
- Left and right doors
- Left and right mirrors
- Left and right side intake trim
- Rear bumper and diffuser
- Active rear wing blade and supports
- Engine cover glass/louver panel
- Front and rear light housings
- Four wheels and tires
- Brake rotors and calipers
- Windshield, side glass, rear glass
- Interior cockpit blockout visible through glass

## Materials

- Metallic dark gray paint with clearcoat reflections
- Exposed carbon fiber for splitter, side skirts, intake trim, wing, and diffuser
- Satin black forged wheels
- Red brake calipers
- Carbon ceramic brake rotors
- Dark smoked glass with separate cracked-glass damage material
- Black leather/alcantara cockpit with red stitching

## Damage States

Create at least three model states:

- Clean: showroom body, normal lights, unbroken glass
- Hit: cracked windshield, bent splitter, scraped paint, displaced bumper corners
- Wrecked: crumpled front/rear panels, shattered glass, broken headlights, loose diffuser, missing wheel optional

Recommended crash fragments:

- 8 to 12 painted body shards
- 6 to 10 carbon aero shards
- 10 to 16 glass shards
- 2 detachable mirror pieces
- 2 detachable light lens pieces

## Animation Hooks

- Wheel rotation on all wheels
- Front wheel steering pivots
- Suspension compression on each wheel
- Active rear wing deploy/retract
- Brake caliper remains fixed while wheel spins
- Glass crack overlay toggles on impact
- Optional door/bumper loosened transform for crash replay

## Game Integration Notes

- Use a clean rear 3/4 or top-down gameplay render for mobile readable scale.
- Keep wheels separate from the car body so tire spin and steering can be animated.
- Use simplified collision volumes: one main body capsule/box, four wheel spheres, front impact wedge, rear impact wedge.
- Create LOD0 around 70k to 100k triangles, LOD1 around 25k, LOD2 around 8k, and a billboard impostor for distant traffic.
- Texture budget: 2k body atlas, 1k interior atlas, 1k wheel atlas, 1k damage/debris atlas.
- Keep all logos and badges fictional or blank.
