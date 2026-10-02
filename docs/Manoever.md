# Bahnmanöver – was ist das?

Ein **Bahnmanöver** ist eine gezielte Änderung der Geschwindigkeit eines Raumfahrzeugs
mit dem Triebwerk, um seine Bahn zu ändern. Ohne Triebwerk folgt der Satellit für immer
derselben Kepler-Bahn – nur ein Manöver bringt ihn auf eine andere.

Die zentrale Größe ist **Δv** („Delta-v“): um wie viel m/s sich die Geschwindigkeit ändert.
In der Raumfahrt ist Δv die eigentliche „Währung“: Jede Mission hat ein Δv-Budget, und
jedes Manöver kostet einen Teil davon.

---

## 1. Warum Geschwindigkeit die Bahn bestimmt

Position **r** und Geschwindigkeit **v** zusammen legen die Bahn eindeutig fest
(genau das ist unser `SpacecraftState`). Ändert man **v** an einem Punkt, ändert sich die
ganze restliche Bahn – aber der Punkt, an dem man gebrannt hat, bleibt auf der neuen Bahn.

Faustregel: **Was man an einer Stelle tut, wirkt sich auf der gegenüberliegenden Seite der
Bahn am stärksten aus.**
- Beschleunigt man nach vorne, wird die Bahn auf der **anderen Seite** höher.
- Bremst man, wird sie auf der anderen Seite niedriger.

Gegenintuitiv: Ein höherer Orbit ist **langsamer**. Wer einen Satelliten vor sich einholen
will, muss also erst *bremsen* (tiefer = schneller), aufholen und dann wieder hoch.

---

## 2. Die sechs Brennrichtungen

Manöver werden nicht in Welt-Koordinaten (x, y, z) angegeben, sondern relativ zur
aktuellen Bewegung des Satelliten:

| Richtung | Zeigt … | Wirkung |
|---|---|---|
| **Prograde** | in Flugrichtung (entlang **v**) | Bahn wird auf der Gegenseite höher, Umlauf länger |
| **Retrograde** | gegen die Flugrichtung | Bahn wird auf der Gegenseite niedriger (z. B. Wiedereintritt) |
| **Normal** | senkrecht zur Bahnebene (entlang **r × v**) | kippt die Bahn → ändert die **Inklination** |
| **Antinormal** | entgegen Normal | kippt in die andere Richtung |
| **Radial out** | vom Planeten weg (entlang **r**) | dreht die Ellipse in der Ebene |
| **Radial in** | zum Planeten hin | dreht die Ellipse andersherum |

Prograde/Retrograde sind am effizientesten, um die Höhe zu ändern. Normal-Manöver
(Bahnebene ändern) sind sehr teuer.

---

## 3. Wichtige Manöver-Typen

### Anheben / Absenken der Bahn
Ein prograder Brennvorgang macht aus einer Kreisbahn eine Ellipse: der Brennpunkt
wird zum tiefsten Punkt (**Perigäum**), die Gegenseite zum höchsten Punkt (**Apogäum**).

### Zirkularisieren
Am Apogäum noch einmal prograde brennen → die Ellipse wird wieder ein Kreis,
diesmal auf der neuen Höhe.

### Hohmann-Transfer
Die klassische, sparsamste Methode, von einer Kreisbahn auf eine höhere zu kommen:
**zwei Brennvorgänge** – einer zum Anheben, einer zum Zirkularisieren, eine halbe
Ellipse dazwischen.

Beispiel (passend zu deiner Frage mit der Höhe): **400 km → 4000 km**
- Brennvorgang 1 (bei 400 km, prograde): **≈ 766 m/s**
- halbe Ellipse fliegen: **≈ 66 min**
- Brennvorgang 2 (bei 4000 km, prograde): **≈ 689 m/s**
- **Summe ≈ 1455 m/s**

Zum Vergleich: 400 km → geostationär (35 786 km) kostet ≈ 2,40 + 1,46 = **≈ 3,85 km/s**
und dauert ≈ 5,3 h.

### Bahnebene ändern (Inklination)
Normal/Antinormal brennen, und zwar an der Stelle, wo sich alte und neue Bahnebene
schneiden (am **Knoten**). Sehr teuer: Schon 10° Ebenenwechsel in 400 km Höhe kosten
≈ 1,3 km/s.

### Phasing (Rendezvous)
Kurz die Bahn ändern, damit man nach ein paar Umläufen an einer bestimmten Stelle
(z. B. bei der ISS) ankommt.

---

## 4. Treibstoff: die Raketengleichung

Δv kostet Treibstoff, und zwar nicht linear:

    Δv = Isp · g₀ · ln(m_voll / m_leer)

- **Isp** (spezifischer Impuls, in Sekunden): wie effizient das Triebwerk ist
  (chemisch ≈ 300 s, Ionentriebwerk ≈ 3000 s)
- **g₀** = 9,80665 m/s²
- **m_voll / m_leer**: Masse vor und nach dem Brennen

Beispiel: 100-kg-Satellit, Isp 300 s, Hohmann 400 → 4000 km (1455 m/s):
m_leer = 100 · e^(−1455 / (300 · 9,81)) ≈ **61 kg** → es werden **≈ 39 kg Treibstoff**
verbraucht.

Darum steht `mass` in unserem `SpacecraftState`: Beim Brennen wird das Raumschiff
leichter, und dasselbe Triebwerk beschleunigt dann stärker (a = F / m).

---

## 5. Impulsiv oder endlich?

- **Impulsives Manöver** (vereinfacht): Δv wird *sofort* auf die Geschwindigkeit addiert.
  Gut für chemische Triebwerke, deren Brennen nur Sekunden bis Minuten dauert.
  Einfach zu rechnen und zu planen.
- **Endliches Manöver** (realistisch): Das Triebwerk erzeugt über eine Zeit eine
  **Kraft**, die als zusätzliche Beschleunigung in die Bewegungsgleichung eingeht
  (genau wie die Gravitation in `Forces.Acceleration`). Nötig für Ionentriebwerke,
  die Tage bis Monate brennen.

---

## 6. Wie das in unseren Code passt

| Begriff | Bei uns |
|---|---|
| Position, Geschwindigkeit, Masse | `SpacecraftState` (relPosition, velocity, mass) |
| Bewegung ohne Triebwerk | `Integrator.RK4Step` mit `Forces.Acceleration` |
| Ausgangsbahn | `KeplerOrbit.Circular(...)` |
| Prograde-Richtung | `velocity` normalisiert |
| Normal-Richtung | `relPosition × velocity` normalisiert |
| Radial-Richtung | `relPosition` normalisiert |
| Zeitpunkt eines Manövers | Sim-Zeit in Sekunden seit J2000 (`SimClock.SimTime`) |

Ein impulsives Manöver ist im Kern nur:
**neue Geschwindigkeit = alte Geschwindigkeit + Δv (in Weltkoordinaten)**
und **neue Masse = alte Masse · e^(−|Δv| / (Isp · g₀))** –
zum richtigen Zeitpunkt, mitten in der Simulation.
