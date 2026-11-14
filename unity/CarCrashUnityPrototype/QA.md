# Living city validation — 14 November 2026

All six native Unity suites passed, with 673 checks in total:

| Suite | Checks | Coverage |
| --- | ---: | --- |
| Graphics | 70 | Real rendered vehicle/district/shop frames, shader support, lighting, geometry budgets, eight building styles, furnished interiors, attendants and instanced breakaway fixtures |
| UI | 169 | Home, garage, readable responsive layouts, driving input, map, pause/resume, sustained free drive and pedestrian routes |
| Physical driving | 286 | Left/right steering, reverse, all pedestrian bodies/routes, high-speed scenery contacts, streetlight/bin/signal toppling and camera clearance |
| Fleet | 52 | All six vehicles, dimensions, wheels and driving behavior |
| World | 84 | Road connections, destinations, recovery and blocked-car escape |
| Traversal | 12 | Six vehicles climb and reverse off beveled sidewalks |

The scene contains 120 walking pedestrians and 42 shop attendants. The generated
city has 671 static material batches and 2,066,712 static vertices, plus 14,259
shared palm-frond instances. Native rendering was checked on an NVIDIA RTX 5080
Laptop GPU. These figures are budgets and regression results, not a promise of
the same frame rate on every phone.

The WebGL release builds successfully and is checked in the local browser. Browser
QA also found and corrected an unsupported sRGB R8 contact-shadow texture request;
the numerical occlusion buffer now explicitly uses linear storage.

The release tooling unit tests and Actionlint workflow checks pass. Android's
custom build method compiles in the editor. A signed AAB, physical Android device
test and successful Play internal upload must still be verified after the Unity
account credentials, approved signing-secret upload and Play Console setup are
available. No Play upload has been completed at this point.

Ignored local evidence is saved under `build/living-city/`, including native
captures, six JSON reports and build logs. See [the release guide](../../android/playstore/README.md)
for the exact signing and publication process.
