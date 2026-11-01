<!-- api/math.md -->

# Mathematik

Vektoren, Matrizen und Quaternionen.

Jeder Typ besteht aus einem globalen Fabrikobjekt (VECTOR2, MATRIX4, QUATERNION, ...), das Werte erzeugt, und einem Handle-Typ mit den Operationen. Konventionen, die für jeden Typ auf dieser Seite gelten:

- Lesen und schreiben Sie Komponentenfelder (X, Y, Z, W) direkt. Indizierter Zugriff (`Get`, `Set`) ist 1-basiert.
- Arithmetische Methoden geben einen neuen Wert zurück und lassen ihre Operanden unverändert. Nur `Set` verändert einen Wert an Ort und Stelle.
- Winkel sind in Bogenmaß.
- Methoden, die mehrere Zahlen zurückgeben (`Unpack`, `RotateVec`), geben sie als mehrere Lua-Werte zurück.

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## Vektoren

### VECTOR

Fabrik für Vektoren beliebiger Länge.

<div class="callout warn">
Als globales Objekt VECTOR verfügbar. Operationen zwischen zwei Vektoren erfordern gleiche Größen; eine Größenabweichung gibt einen Vektor der Länge null (oder 0 bei skalaren Ergebnissen) zurück und löst keinen Fehler aus.
</div>

| Methode | Beschreibung |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | Erzeugt einen mit Nullen gefüllten Vektor der Länge n. |

### Vector-Handle

Ein Vektor beliebiger Länge mit 1-indizierten Komponenten.

| Methode | Beschreibung |
| --- | --- |
| `vector:Size()  -> number` | Gibt die Anzahl der Komponenten zurück. |
| `vector:Get(i)  -> number` | Gibt die i-te Komponente zurück, oder 0, wenn i außerhalb des Bereichs liegt. |
| `vector:Set(i, value)  -> nil` | Setzt die i-te Komponente; Indizes außerhalb des Bereichs ignoriert die Methode. |
| `vector:Add(o)  -> vector` | Gibt die komponentenweise Summe zurück. |
| `vector:Sub(o)  -> vector` | Gibt die komponentenweise Differenz zurück. |
| `vector:Mul(o)  -> vector` | Gibt das komponentenweise Produkt zurück. |
| `vector:Scale(s)  -> vector` | Gibt den Vektor mit jeder Komponente mal s zurück. |
| `vector:Dot(o)  -> number` | Gibt das Skalarprodukt zurück. |
| `vector:Length()  -> number` | Gibt die euklidische Länge zurück. |
| `vector:LengthSq()  -> number` | Gibt die quadrierte Länge zurück. |
| `vector:Distance(o)  -> number` | Gibt den euklidischen Abstand zu o zurück. |
| `vector:Normalized()  -> vector` | Gibt eine Kopie mit Einheitslänge zurück, oder einen Nullvektor, wenn die Länge nahe null ist. |
| `vector:Lerp(o, t)  -> vector` | Gibt die lineare Interpolation in Richtung o um den Anteil t zurück. |
| `vector:Clone()  -> vector` | Gibt eine unabhängige Kopie zurück. |

### VECTOR2

Fabrik für 2D-Vektoren.

<div class="callout warn">
Als globales Objekt VECTOR2 verfügbar.
</div>

| Methode | Beschreibung |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | Erzeugt einen 2D-Vektor. |
| `VECTOR2:Zero()  -> vector2` | Gibt (0, 0) zurück. |
| `VECTOR2:One()  -> vector2` | Gibt (1, 1) zurück. |

### Vector2-Handle

Ein 2D-Vektor mit den Feldern X und Y.

| Methode | Beschreibung |
| --- | --- |
| `vector2.X  -> number` | Die x-Komponente (les- und schreibbares Feld). |
| `vector2.Y  -> number` | Die y-Komponente (les- und schreibbares Feld). |
| `vector2:Add(o)  -> vector2` | Gibt die komponentenweise Summe zurück. |
| `vector2:Sub(o)  -> vector2` | Gibt die komponentenweise Differenz zurück. |
| `vector2:Mul(o)  -> vector2` | Gibt das komponentenweise Produkt zurück. |
| `vector2:Scale(s)  -> vector2` | Gibt den mit s multiplizierten Vektor zurück. |
| `vector2:Negate()  -> vector2` | Gibt (-X, -Y) zurück. |
| `vector2:Dot(o)  -> number` | Gibt das Skalarprodukt zurück. |
| `vector2:Cross(o)  -> number` | Gibt das skalare Kreuzprodukt zurück (X * o.Y - Y * o.X). |
| `vector2:Length()  -> number` | Gibt die Länge zurück. |
| `vector2:LengthSq()  -> number` | Gibt die quadrierte Länge zurück. |
| `vector2:Distance(o)  -> number` | Gibt den Abstand zu o zurück. |
| `vector2:Normalized()  -> vector2` | Gibt eine Kopie mit Einheitslänge zurück, oder (0, 0), wenn die Länge nahe null ist. |
| `vector2:Lerp(o, t)  -> vector2` | Gibt die lineare Interpolation in Richtung o um den Anteil t zurück. |
| `vector2:Rotate(radians)  -> vector2` | Gibt den um den angegebenen Winkel gegen den Uhrzeigersinn gedrehten Vektor zurück. |
| `vector2:Clone()  -> vector2` | Gibt eine unabhängige Kopie zurück. |
| `vector2:Set(x, y)  -> nil` | Setzt X und Y an Ort und Stelle. |
| `vector2:Unpack()  -> number, number` | Gibt X und Y als zwei Werte zurück. |
| `vector2:ToString()  -> string` | Gibt "(X, Y)" zurück. |

### VECTOR3

Fabrik für 3D-Vektoren.

<div class="callout warn">
Als globales Objekt VECTOR3 verfügbar.
</div>

| Methode | Beschreibung |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | Erzeugt einen 3D-Vektor. |
| `VECTOR3:Zero()  -> vector3` | Gibt (0, 0, 0) zurück. |
| `VECTOR3:One()  -> vector3` | Gibt (1, 1, 1) zurück. |

### Vector3-Handle

Ein 3D-Vektor mit den Feldern X, Y und Z.

| Methode | Beschreibung |
| --- | --- |
| `vector3.X  -> number` | Die x-Komponente (les- und schreibbares Feld). |
| `vector3.Y  -> number` | Die y-Komponente (les- und schreibbares Feld). |
| `vector3.Z  -> number` | Die z-Komponente (les- und schreibbares Feld). |
| `vector3:Add(o)  -> vector3` | Gibt die komponentenweise Summe zurück. |
| `vector3:Sub(o)  -> vector3` | Gibt die komponentenweise Differenz zurück. |
| `vector3:Mul(o)  -> vector3` | Gibt das komponentenweise Produkt zurück. |
| `vector3:Scale(s)  -> vector3` | Gibt den mit s multiplizierten Vektor zurück. |
| `vector3:Negate()  -> vector3` | Gibt (-X, -Y, -Z) zurück. |
| `vector3:Dot(o)  -> number` | Gibt das Skalarprodukt zurück. |
| `vector3:Cross(o)  -> vector3` | Gibt das Kreuzprodukt zurück. |
| `vector3:Length()  -> number` | Gibt die Länge zurück. |
| `vector3:LengthSq()  -> number` | Gibt die quadrierte Länge zurück. |
| `vector3:Distance(o)  -> number` | Gibt den Abstand zu o zurück. |
| `vector3:Normalized()  -> vector3` | Gibt eine Kopie mit Einheitslänge zurück, oder (0, 0, 0), wenn die Länge nahe null ist. |
| `vector3:Lerp(o, t)  -> vector3` | Gibt die lineare Interpolation in Richtung o um den Anteil t zurück. |
| `vector3:Reflect(n)  -> vector3` | Gibt den an der Normalen n gespiegelten Vektor zurück (übergeben Sie ein n mit Einheitslänge). |
| `vector3:Clone()  -> vector3` | Gibt eine unabhängige Kopie zurück. |
| `vector3:Set(x, y, z)  -> nil` | Setzt X, Y und Z an Ort und Stelle. |
| `vector3:Unpack()  -> number, number, number` | Gibt X, Y und Z als drei Werte zurück. |
| `vector3:ToString()  -> string` | Gibt "(X, Y, Z)" zurück. |

### VECTOR4

Fabrik für 4D-Vektoren.

<div class="callout warn">
Als globales Objekt VECTOR4 verfügbar.
</div>

| Methode | Beschreibung |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | Erzeugt einen 4D-Vektor. |
| `VECTOR4:Zero()  -> vector4` | Gibt (0, 0, 0, 0) zurück. |
| `VECTOR4:One()  -> vector4` | Gibt (1, 1, 1, 1) zurück. |

### Vector4-Handle

Ein 4D-Vektor mit den Feldern X, Y, Z und W.

| Methode | Beschreibung |
| --- | --- |
| `vector4.X  -> number` | Die x-Komponente (les- und schreibbares Feld). |
| `vector4.Y  -> number` | Die y-Komponente (les- und schreibbares Feld). |
| `vector4.Z  -> number` | Die z-Komponente (les- und schreibbares Feld). |
| `vector4.W  -> number` | Die w-Komponente (les- und schreibbares Feld). |
| `vector4:Add(o)  -> vector4` | Gibt die komponentenweise Summe zurück. |
| `vector4:Sub(o)  -> vector4` | Gibt die komponentenweise Differenz zurück. |
| `vector4:Mul(o)  -> vector4` | Gibt das komponentenweise Produkt zurück. |
| `vector4:Scale(s)  -> vector4` | Gibt den mit s multiplizierten Vektor zurück. |
| `vector4:Negate()  -> vector4` | Gibt (-X, -Y, -Z, -W) zurück. |
| `vector4:Dot(o)  -> number` | Gibt das Skalarprodukt zurück. |
| `vector4:Length()  -> number` | Gibt die Länge zurück. |
| `vector4:LengthSq()  -> number` | Gibt die quadrierte Länge zurück. |
| `vector4:Distance(o)  -> number` | Gibt den Abstand zu o zurück. |
| `vector4:Normalized()  -> vector4` | Gibt eine Kopie mit Einheitslänge zurück, oder (0, 0, 0, 0), wenn die Länge nahe null ist. |
| `vector4:Lerp(o, t)  -> vector4` | Gibt die lineare Interpolation in Richtung o um den Anteil t zurück. |
| `vector4:Clone()  -> vector4` | Gibt eine unabhängige Kopie zurück. |
| `vector4:Set(x, y, z, w)  -> nil` | Setzt X, Y, Z und W an Ort und Stelle. |
| `vector4:Unpack()  -> number, number, number, number` | Gibt X, Y, Z und W als vier Werte zurück. |
| `vector4:ToString()  -> string` | Gibt "(X, Y, Z, W)" zurück. |

## Matrizen

Alle Matrizen sind zeilenorientiert (row-major): `Get(r, c)` liest Zeile r, Spalte c, beide 1-basiert. `MulVec` behandelt den Vektor als Spaltenvektor (Ergebnis = M * v).

### MATRIX

Fabrik für Matrizen beliebiger Größe.

<div class="callout warn">
Als globales Objekt MATRIX verfügbar. Größenmäßig inkompatible Operationen geben eine leere 0-mal-0-Matrix (oder einen Vektor der Länge null) zurück und lösen keinen Fehler aus.
</div>

| Methode | Beschreibung |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | Erzeugt eine mit Nullen gefüllte Matrix. |
| `MATRIX:Identity(n)  -> matrix` | Erzeugt eine n-mal-n-Einheitsmatrix. |

### Matrix-Handle

Eine Matrix beliebiger Größe.

<div class="callout warn">
Add und Sub erfordern übereinstimmende Dimensionen; Mul erfordert, dass die Spaltenzahl dieser Matrix der Zeilenzahl der anderen entspricht; MulVec erfordert, dass die Vektorgröße der Spaltenzahl entspricht.
</div>

| Methode | Beschreibung |
| --- | --- |
| `matrix:RowCount()  -> number` | Gibt die Anzahl der Zeilen zurück. |
| `matrix:ColCount()  -> number` | Gibt die Anzahl der Spalten zurück. |
| `matrix:Get(r, c)  -> number` | Gibt das Element in Zeile r, Spalte c zurück, oder 0 außerhalb des Bereichs. |
| `matrix:Set(r, c, v)  -> nil` | Setzt das Element in Zeile r, Spalte c; Indizes außerhalb des Bereichs ignoriert die Methode. |
| `matrix:Add(o)  -> matrix` | Gibt die elementweise Summe zurück. |
| `matrix:Sub(o)  -> matrix` | Gibt die elementweise Differenz zurück. |
| `matrix:Scale(s)  -> matrix` | Gibt die Matrix mit jedem Element mal s zurück. |
| `matrix:Mul(o)  -> matrix` | Gibt das Matrixprodukt zurück. |
| `matrix:MulVec(v)  -> vector` | Gibt das Produkt mit einem Vektor zurück (siehe VECTOR). |
| `matrix:Transpose()  -> matrix` | Gibt die transponierte Matrix zurück. |
| `matrix:Clone()  -> matrix` | Gibt eine unabhängige Kopie zurück. |

### MATRIX2

Fabrik für 2-mal-2-Matrizen.

<div class="callout warn">
Als globales Objekt MATRIX2 verfügbar. Get und Set prüfen ihre Indizes nicht; halten Sie sie im Bereich 1 bis 2.
</div>

| Methode | Beschreibung |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | Gibt die Einheitsmatrix zurück. |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | Erzeugt eine Matrix aus ihren vier Elementen in Zeilenreihenfolge. |
| `MATRIX2:Rotation(radians)  -> matrix2` | Gibt eine Rotationsmatrix gegen den Uhrzeigersinn zurück. |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | Gibt eine Skalierungsmatrix zurück. |

### Matrix2-Handle

Eine 2-mal-2-Matrix.

| Methode | Beschreibung |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | Gibt das Element in Zeile r, Spalte c zurück. |
| `matrix2:Set(r, c, v)  -> nil` | Setzt das Element in Zeile r, Spalte c. |
| `matrix2:Mul(o)  -> matrix2` | Gibt das Matrixprodukt zurück. |
| `matrix2:MulVec(v)  -> vector2` | Gibt das Produkt mit einem 2D-Vektor zurück. |
| `matrix2:Scale(s)  -> matrix2` | Gibt die Matrix mit jedem Element mal s zurück. |
| `matrix2:Transpose()  -> matrix2` | Gibt die transponierte Matrix zurück. |
| `matrix2:Determinant()  -> number` | Gibt die Determinante zurück. |
| `matrix2:Clone()  -> matrix2` | Gibt eine unabhängige Kopie zurück. |

### MATRIX3

Fabrik für 3-mal-3-Matrizen.

<div class="callout warn">
Als globales Objekt MATRIX3 verfügbar. Get und Set prüfen ihre Indizes nicht; halten Sie sie im Bereich 1 bis 3.
</div>

| Methode | Beschreibung |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | Gibt die Einheitsmatrix zurück. |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | Erzeugt eine Matrix aus ihren neun Elementen in Zeilenreihenfolge. |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | Gibt eine aus den Winkeln Yaw, Pitch und Roll gebildete Rotationsmatrix zurück. |

### Matrix3-Handle

Eine 3-mal-3-Matrix.

| Methode | Beschreibung |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | Gibt das Element in Zeile r, Spalte c zurück. |
| `matrix3:Set(r, c, v)  -> nil` | Setzt das Element in Zeile r, Spalte c. |
| `matrix3:Mul(o)  -> matrix3` | Gibt das Matrixprodukt zurück. |
| `matrix3:MulVec(v)  -> vector3` | Gibt das Produkt mit einem 3D-Vektor zurück. |
| `matrix3:Scale(s)  -> matrix3` | Gibt die Matrix mit jedem Element mal s zurück. |
| `matrix3:Transpose()  -> matrix3` | Gibt die transponierte Matrix zurück. |
| `matrix3:Determinant()  -> number` | Gibt die Determinante zurück. |
| `matrix3:Clone()  -> matrix3` | Gibt eine unabhängige Kopie zurück. |

### MATRIX4

Fabrik für 4-mal-4-Matrizen.

<div class="callout warn">
Als globales Objekt MATRIX4 verfügbar. Get und Set prüfen ihre Indizes nicht; halten Sie sie im Bereich 1 bis 4. Translation speichert die Verschiebungen in der vierten Spalte, sodass sie über MulVec auf einen vector4 mit W = 1 wirkt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | Gibt die Einheitsmatrix zurück. |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | Gibt eine Translationsmatrix zurück. |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | Gibt eine Skalierungsmatrix zurück. |

### Matrix4-Handle

Eine 4-mal-4-Matrix.

| Methode | Beschreibung |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | Gibt das Element in Zeile r, Spalte c zurück. |
| `matrix4:Set(r, c, v)  -> nil` | Setzt das Element in Zeile r, Spalte c. |
| `matrix4:Mul(o)  -> matrix4` | Gibt das Matrixprodukt zurück. |
| `matrix4:MulVec(v)  -> vector4` | Gibt das Produkt mit einem 4D-Vektor zurück. |
| `matrix4:Scale(s)  -> matrix4` | Gibt die Matrix mit jedem Element mal s zurück. |
| `matrix4:Transpose()  -> matrix4` | Gibt die transponierte Matrix zurück. |
| `matrix4:Clone()  -> matrix4` | Gibt eine unabhängige Kopie zurück. |

## Quaternionen

### QUATERNION

Fabrik für Quaternionen.

<div class="callout warn">
Als globales Objekt QUATERNION verfügbar. Die Identität ist (0, 0, 0, 1). Rotationsmethoden setzen Einheitsquaternionen voraus; rufen Sie Normalized auf, nachdem Sie eine von Hand gebildet haben.
</div>

| Methode | Beschreibung |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | Gibt die Identitätsquaternion zurück. |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | Erzeugt eine Quaternion aus ihren vier Komponenten. |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | Gibt die Rotation um `angle` Bogenmaß um die Achse (ax, ay, az) zurück. Die Methode normalisiert die Achse; eine Nullachse ergibt die Identität. |

### Quaternion-Handle

Eine Quaternion mit den Feldern X, Y, Z und W.

| Methode | Beschreibung |
| --- | --- |
| `quaternion.X  -> number` | Die x-Komponente (les- und schreibbares Feld). |
| `quaternion.Y  -> number` | Die y-Komponente (les- und schreibbares Feld). |
| `quaternion.Z  -> number` | Die z-Komponente (les- und schreibbares Feld). |
| `quaternion.W  -> number` | Die w-Komponente (les- und schreibbares Feld). |
| `quaternion:Mul(o)  -> quaternion` | Gibt das Hamilton-Produkt this * o zurück. Das Anwenden des Ergebnisses rotiert zuerst um o, dann um this. |
| `quaternion:Dot(o)  -> number` | Gibt das Skalarprodukt zurück. |
| `quaternion:Length()  -> number` | Gibt die Länge zurück. |
| `quaternion:Normalized()  -> quaternion` | Gibt eine Kopie mit Einheitslänge zurück, oder die Identität, wenn die Länge nahe null ist. |
| `quaternion:Conjugate()  -> quaternion` | Gibt (-X, -Y, -Z, W) zurück, die inverse Rotation einer Einheitsquaternion. |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | Rotiert den Vektor (vx, vy, vz) und gibt die resultierenden x, y und z zurück. |
| `quaternion:Slerp(o, t)  -> quaternion` | Gibt die sphärische lineare Interpolation in Richtung o um den Anteil t zurück, entlang des kürzesten Bogens. |
| `quaternion:Clone()  -> quaternion` | Gibt eine unabhängige Kopie zurück. |
| `quaternion:Unpack()  -> number, number, number, number` | Gibt X, Y, Z und W als vier Werte zurück. |
