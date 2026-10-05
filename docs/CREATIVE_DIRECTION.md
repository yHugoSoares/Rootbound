# Creative Direction - Duatborn

**Duatborn** is the final player-facing title (superseding "Rootbound: Fractured
Realms" and "Dawnkeepers: Gates of Duat"). There is no subtitle. It is an
original Egyptian-inspired fictional setting; all player characters, factions,
powers, and place names below are original fiction inspired by ancient Egyptian
imagery and are **not** claims about historical religious beliefs.

## Pitch and player fantasy

Animal-shaped guardians descend through a fractured Duat - the night passage the
sun must cross - to reopen the sealed gates and restore dawn. This is **not** a
story about escaping the underworld; it is about restoring the passage through
the night, together.

Players embody a guardian order (the Duatborn), each with a distinct movement
and attack identity, and combine their rites to break seals that no single
guardian can.

## Character and ability mapping

Internal identifiers and enum values are unchanged (stable for serialization);
only display text changes.

| Internal (kept) | Display name | Role |
| --- | --- | --- |
| `RootGuardian` | **Dune Warden** | Armored scarab guardian; controls space. |
| `EmberMoth` | **Sunwing** | Solar falcon spirit; mobile ranged attacker. |
| `RootCage` special | **Binding Seal** | Aimed circular seal that restrains ordinary enemies. |
| Cage ignition | **Solar Consecration** | The Sunwing's burst consecrates an active seal. |
| `Blightling` | **Hollow Sentinel** | Basic melee guardian of the gates. |
| `Sporeling` | **Fractured Vessel** | Fast, fragile; bursts when destroyed. |

Ability labels:
- Dune Warden primary: **Sweeping Khopesh** (short arc).
- Dune Warden special: **Binding Seal**.
- Sunwing primary: **Solar Bolt** (ranged).
- Sunwing special: **Solar Consecration**.

Behavior is unchanged: the seal still restrains and still burns (periodically)
only when consecrated; range, cooldown, damage, and invulnerability are the same
rules as before.

### Upgrade names (same effects)

| Effect (unchanged) | Display name |
| --- | --- |
| +30 max health | **Sun's Vitality** |
| +6 primary damage | **Khopesh Edge** |
| +0.6 move speed | **Swift Sandals** |
| +1.0 special radius | **Widened Rite** |
| +1.5 seal duration | **Enduring Seal** (Dune Warden only - see Applicability) |
| heal 6 on kill | **Funerary Offering** |

**Applicability:** a seal-duration upgrade benefits only a creature whose special
uses a duration (Dune Warden's Binding Seal). It is filtered out of the offered
set when no participating creature benefits, so it is never offered as a dead
pick (`UpgradeRules.AppliesTo`).

### Gate/room names

| Internal id | Display name |
| --- | --- |
| `room_1` | Sun Gate |
| `room_2` | Shadow Gate |
| `room_3` (finale) | Horizon Gate |

## Visual palette and effect language

Readable, distinct, and placeholder-friendly (URP unlit/lit primitives):

- Dune Warden: sandstone/bronze `#C2A15A`.
- Sunwing: solar gold `#E8A33D`.
- Hollow Sentinel: slate basalt `#6B6E76`.
- Fractured Vessel: verdigris teal `#3FA7A0`.
- Binding Seal: lapis blue `#2E5FA3`; **Solar Consecration**: gold `#F2B441`.
- Solar projectiles: `#FFD166`; Fractured Vessel death burst: teal `#4FC3B8`.
- Selection/aim marker: sky lapis `#49B6E8`, clamped/out-of-range `#E08A2B`.
- Danger is reserved for hostile states and the clamped marker; friendly rites are
  gold; selection is lapis. No global lighting overhaul is applied.

Effect language: inscribed rings for seals, rays/embers for solar effects, and a
cracked-glass teal burst for the Fractured Vessel - all still placeholder
shapes derived from gameplay state.

## Mythological inspirations (real sources)

Used as *atmosphere and vocabulary only*:

- The **Duat** (netherworld) and the sun's nightly journey, and the **Amduat**
  ("Book of What Is in the Netherworld") with its gates and hours, in New Kingdom
  royal tombs. Hornung, *The Ancient Egyptian Books of the Afterlife* (Cornell UP).
- **Book of the Dead** spells for passing gates/portals, e.g. the Papyrus of Ani
  (c. 1250 BCE, British Museum). Faulkner, *The Ancient Egyptian Book of the Dead*.
  Taylor, *Death and the Afterlife in Ancient Egypt* (British Museum Press).
- **Khepri** (scarab associated with the rising sun) and **Horus/Ra** falcon
  imagery; **Ma'at** (order/truth) as the thing being restored. The Metropolitan
  Museum of Art and the British Museum Egyptian collections.
- Funerary offerings and the idea that the dead are sustained by rites - echoed
  in original mechanics, not presented as doctrine.

Egyptian tradition is plural and changes over millennia; we do not treat any
single account or spell as "the" tradition.

## Explicit fictional departures

- The Duatborn order and its animal guardians (Dune Warden, Sunwing) are
  invented; there is no historical "Duatborn", "Dune Warden", or "Sunwing".
- "Binding Seal", "Solar Consecration", "Hollow Sentinel", "Fractured Vessel",
  and the "fractured" Duat are original game fiction.
- Reopening the sun's route as a co-op objective, and the ignition interaction,
  are invented mechanics.
- No real deity, spell, or funerary practice is re-enacted or asserted.

## Player-facing content requiring migration

1. Menu title/subtitle and mode copy.
2. Creature display names + HUD labels (`Root Guardian`/`Ember Moth` -> new).
3. Ability labels shown in HUD/diagnostics ("Root Cage" -> "Binding Seal").
4. Enemy display names + encounter counter ("Blightlings" -> "Sentinels").
5. Upgrade display names (+ applicability filtering).
6. Result/room copy ("Region Reclaimed" -> "Gate Restored"; "Room" -> "Gate").
7. Placeholder colors (creatures, enemies, seal/consecration, projectiles, burst).
8. One representative gate room: placeholder gate geometry + floor inscription.
9. Branding: logo prompt/image (a Duatborn gate/sun-disc logo is supplied at
   `docs/assets/Duatborn.jpg`).

Internal type names, namespaces, assembly names, enum values, and stable ids are
**not** renamed. The Unity project folder, `Assets/Rootbound` path, and
`Rootbound.*` namespaces keep their names; the GitHub repository is renamed to
`duatborn`.
