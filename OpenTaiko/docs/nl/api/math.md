<!-- api/math.md -->

# Wiskunde

Vectoren, matrices en quaternions.

Elk type bestaat uit een factory-global (VECTOR2, MATRIX4, QUATERNION, ...) die waarden maakt, en een handle-type met de bewerkingen. Conventies die elk type op deze pagina deelt:

- Lees en schrijf componentvelden (X, Y, Z, W) rechtstreeks. Geïndexeerde toegang (`Get`, `Set`) begint bij 1.
- Rekenkundige methoden geven een nieuwe waarde terug en laten hun operanden onaangeroerd. Alleen `Set` wijzigt een waarde ter plekke.
- Hoeken zijn in radialen.
- Methoden die meerdere getallen teruggeven (`Unpack`, `RotateVec`) geven ze terug als meerdere Lua-waarden.

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## Vectoren

### VECTOR

Factory voor vectoren van willekeurige lengte.

<div class="callout warn">
Beschikbaar als de global VECTOR. Bewerkingen tussen twee vectoren vereisen gelijke groottes; een groottemismatch geeft een vector van lengte nul terug (of 0 voor scalaire resultaten) en werpt geen fout op.
</div>

| Methode | Beschrijving |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | Maakt een met nullen gevulde vector van lengte n. |

### Vector-handle

Een vector van willekeurige lengte met 1-geïndexeerde componenten.

| Methode | Beschrijving |
| --- | --- |
| `vector:Size()  -> number` | Geeft het aantal componenten terug. |
| `vector:Get(i)  -> number` | Geeft de i-de component terug, of 0 als i buiten bereik is. |
| `vector:Set(i, value)  -> nil` | Stelt de i-de component in; de methode negeert indices buiten bereik. |
| `vector:Add(o)  -> vector` | Geeft de componentsgewijze som terug. |
| `vector:Sub(o)  -> vector` | Geeft het componentsgewijze verschil terug. |
| `vector:Mul(o)  -> vector` | Geeft het componentsgewijze product terug. |
| `vector:Scale(s)  -> vector` | Geeft de vector terug met elke component vermenigvuldigd met s. |
| `vector:Dot(o)  -> number` | Geeft het inproduct terug. |
| `vector:Length()  -> number` | Geeft de Euclidische lengte terug. |
| `vector:LengthSq()  -> number` | Geeft de gekwadrateerde lengte terug. |
| `vector:Distance(o)  -> number` | Geeft de Euclidische afstand tot o terug. |
| `vector:Normalized()  -> vector` | Geeft een kopie met eenheidslengte terug, of een nulvector als de lengte bijna nul is. |
| `vector:Lerp(o, t)  -> vector` | Geeft de lineaire interpolatie richting o met fractie t terug. |
| `vector:Clone()  -> vector` | Geeft een onafhankelijke kopie terug. |

### VECTOR2

Factory voor 2D-vectoren.

<div class="callout warn">
Beschikbaar als de global VECTOR2.
</div>

| Methode | Beschrijving |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | Maakt een 2D-vector. |
| `VECTOR2:Zero()  -> vector2` | Geeft (0, 0) terug. |
| `VECTOR2:One()  -> vector2` | Geeft (1, 1) terug. |

### Vector2-handle

Een 2D-vector met de velden X en Y.

| Methode | Beschrijving |
| --- | --- |
| `vector2.X  -> number` | De x-component (leesbaar en schrijfbaar veld). |
| `vector2.Y  -> number` | De y-component (leesbaar en schrijfbaar veld). |
| `vector2:Add(o)  -> vector2` | Geeft de componentsgewijze som terug. |
| `vector2:Sub(o)  -> vector2` | Geeft het componentsgewijze verschil terug. |
| `vector2:Mul(o)  -> vector2` | Geeft het componentsgewijze product terug. |
| `vector2:Scale(s)  -> vector2` | Geeft de vector vermenigvuldigd met s terug. |
| `vector2:Negate()  -> vector2` | Geeft (-X, -Y) terug. |
| `vector2:Dot(o)  -> number` | Geeft het inproduct terug. |
| `vector2:Cross(o)  -> number` | Geeft het scalaire kruisproduct terug (X * o.Y - Y * o.X). |
| `vector2:Length()  -> number` | Geeft de lengte terug. |
| `vector2:LengthSq()  -> number` | Geeft de gekwadrateerde lengte terug. |
| `vector2:Distance(o)  -> number` | Geeft de afstand tot o terug. |
| `vector2:Normalized()  -> vector2` | Geeft een kopie met eenheidslengte terug, of (0, 0) als de lengte bijna nul is. |
| `vector2:Lerp(o, t)  -> vector2` | Geeft de lineaire interpolatie richting o met fractie t terug. |
| `vector2:Rotate(radians)  -> vector2` | Geeft de vector terug, tegen de klok in geroteerd over de gegeven hoek. |
| `vector2:Clone()  -> vector2` | Geeft een onafhankelijke kopie terug. |
| `vector2:Set(x, y)  -> nil` | Stelt X en Y ter plekke in. |
| `vector2:Unpack()  -> number, number` | Geeft X en Y als twee waarden terug. |
| `vector2:ToString()  -> string` | Geeft "(X, Y)" terug. |

### VECTOR3

Factory voor 3D-vectoren.

<div class="callout warn">
Beschikbaar als de global VECTOR3.
</div>

| Methode | Beschrijving |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | Maakt een 3D-vector. |
| `VECTOR3:Zero()  -> vector3` | Geeft (0, 0, 0) terug. |
| `VECTOR3:One()  -> vector3` | Geeft (1, 1, 1) terug. |

### Vector3-handle

Een 3D-vector met de velden X, Y en Z.

| Methode | Beschrijving |
| --- | --- |
| `vector3.X  -> number` | De x-component (leesbaar en schrijfbaar veld). |
| `vector3.Y  -> number` | De y-component (leesbaar en schrijfbaar veld). |
| `vector3.Z  -> number` | De z-component (leesbaar en schrijfbaar veld). |
| `vector3:Add(o)  -> vector3` | Geeft de componentsgewijze som terug. |
| `vector3:Sub(o)  -> vector3` | Geeft het componentsgewijze verschil terug. |
| `vector3:Mul(o)  -> vector3` | Geeft het componentsgewijze product terug. |
| `vector3:Scale(s)  -> vector3` | Geeft de vector vermenigvuldigd met s terug. |
| `vector3:Negate()  -> vector3` | Geeft (-X, -Y, -Z) terug. |
| `vector3:Dot(o)  -> number` | Geeft het inproduct terug. |
| `vector3:Cross(o)  -> vector3` | Geeft het kruisproduct terug. |
| `vector3:Length()  -> number` | Geeft de lengte terug. |
| `vector3:LengthSq()  -> number` | Geeft de gekwadrateerde lengte terug. |
| `vector3:Distance(o)  -> number` | Geeft de afstand tot o terug. |
| `vector3:Normalized()  -> vector3` | Geeft een kopie met eenheidslengte terug, of (0, 0, 0) als de lengte bijna nul is. |
| `vector3:Lerp(o, t)  -> vector3` | Geeft de lineaire interpolatie richting o met fractie t terug. |
| `vector3:Reflect(n)  -> vector3` | Geeft de vector gespiegeld om de normaal n terug (geef een n met eenheidslengte door). |
| `vector3:Clone()  -> vector3` | Geeft een onafhankelijke kopie terug. |
| `vector3:Set(x, y, z)  -> nil` | Stelt X, Y en Z ter plekke in. |
| `vector3:Unpack()  -> number, number, number` | Geeft X, Y en Z als drie waarden terug. |
| `vector3:ToString()  -> string` | Geeft "(X, Y, Z)" terug. |

### VECTOR4

Factory voor 4D-vectoren.

<div class="callout warn">
Beschikbaar als de global VECTOR4.
</div>

| Methode | Beschrijving |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | Maakt een 4D-vector. |
| `VECTOR4:Zero()  -> vector4` | Geeft (0, 0, 0, 0) terug. |
| `VECTOR4:One()  -> vector4` | Geeft (1, 1, 1, 1) terug. |

### Vector4-handle

Een 4D-vector met de velden X, Y, Z en W.

| Methode | Beschrijving |
| --- | --- |
| `vector4.X  -> number` | De x-component (leesbaar en schrijfbaar veld). |
| `vector4.Y  -> number` | De y-component (leesbaar en schrijfbaar veld). |
| `vector4.Z  -> number` | De z-component (leesbaar en schrijfbaar veld). |
| `vector4.W  -> number` | De w-component (leesbaar en schrijfbaar veld). |
| `vector4:Add(o)  -> vector4` | Geeft de componentsgewijze som terug. |
| `vector4:Sub(o)  -> vector4` | Geeft het componentsgewijze verschil terug. |
| `vector4:Mul(o)  -> vector4` | Geeft het componentsgewijze product terug. |
| `vector4:Scale(s)  -> vector4` | Geeft de vector vermenigvuldigd met s terug. |
| `vector4:Negate()  -> vector4` | Geeft (-X, -Y, -Z, -W) terug. |
| `vector4:Dot(o)  -> number` | Geeft het inproduct terug. |
| `vector4:Length()  -> number` | Geeft de lengte terug. |
| `vector4:LengthSq()  -> number` | Geeft de gekwadrateerde lengte terug. |
| `vector4:Distance(o)  -> number` | Geeft de afstand tot o terug. |
| `vector4:Normalized()  -> vector4` | Geeft een kopie met eenheidslengte terug, of (0, 0, 0, 0) als de lengte bijna nul is. |
| `vector4:Lerp(o, t)  -> vector4` | Geeft de lineaire interpolatie richting o met fractie t terug. |
| `vector4:Clone()  -> vector4` | Geeft een onafhankelijke kopie terug. |
| `vector4:Set(x, y, z, w)  -> nil` | Stelt X, Y, Z en W ter plekke in. |
| `vector4:Unpack()  -> number, number, number, number` | Geeft X, Y, Z en W als vier waarden terug. |
| `vector4:ToString()  -> string` | Geeft "(X, Y, Z, W)" terug. |

## Matrices

Alle matrices zijn row-major: `Get(r, c)` leest rij r, kolom c, beide vanaf 1. `MulVec` behandelt de vector als kolomvector (resultaat = M * v).

### MATRIX

Factory voor matrices van willekeurige grootte.

<div class="callout warn">
Beschikbaar als de global MATRIX. Bewerkingen met onverenigbare groottes geven een lege matrix van 0 bij 0 terug (of een vector van lengte nul) en werpen geen fout op.
</div>

| Methode | Beschrijving |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | Maakt een met nullen gevulde matrix. |
| `MATRIX:Identity(n)  -> matrix` | Maakt een identiteitsmatrix van n bij n. |

### Matrix-handle

Een matrix van willekeurige grootte.

<div class="callout warn">
Add en Sub vereisen overeenkomende afmetingen; Mul vereist dat het aantal kolommen van deze matrix gelijk is aan het aantal rijen van de andere; MulVec vereist dat de vectorgrootte gelijk is aan het aantal kolommen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `matrix:RowCount()  -> number` | Geeft het aantal rijen terug. |
| `matrix:ColCount()  -> number` | Geeft het aantal kolommen terug. |
| `matrix:Get(r, c)  -> number` | Geeft het element op rij r, kolom c terug, of 0 indien buiten bereik. |
| `matrix:Set(r, c, v)  -> nil` | Stelt het element op rij r, kolom c in; de methode negeert indices buiten bereik. |
| `matrix:Add(o)  -> matrix` | Geeft de elementsgewijze som terug. |
| `matrix:Sub(o)  -> matrix` | Geeft het elementsgewijze verschil terug. |
| `matrix:Scale(s)  -> matrix` | Geeft de matrix terug met elk element vermenigvuldigd met s. |
| `matrix:Mul(o)  -> matrix` | Geeft het matrixproduct terug. |
| `matrix:MulVec(v)  -> vector` | Geeft het product met een vector terug (zie VECTOR). |
| `matrix:Transpose()  -> matrix` | Geeft de getransponeerde matrix terug. |
| `matrix:Clone()  -> matrix` | Geeft een onafhankelijke kopie terug. |

### MATRIX2

Factory voor matrices van 2 bij 2.

<div class="callout warn">
Beschikbaar als de global MATRIX2. Get en Set controleren hun indices niet; houd ze binnen 1 tot 2.
</div>

| Methode | Beschrijving |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | Geeft de identiteitsmatrix terug. |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | Maakt een matrix uit zijn vier elementen in row-major-volgorde. |
| `MATRIX2:Rotation(radians)  -> matrix2` | Geeft een rotatiematrix tegen de klok in terug. |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | Geeft een schaalmatrix terug. |

### Matrix2-handle

Een matrix van 2 bij 2.

| Methode | Beschrijving |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | Geeft het element op rij r, kolom c terug. |
| `matrix2:Set(r, c, v)  -> nil` | Stelt het element op rij r, kolom c in. |
| `matrix2:Mul(o)  -> matrix2` | Geeft het matrixproduct terug. |
| `matrix2:MulVec(v)  -> vector2` | Geeft het product met een 2D-vector terug. |
| `matrix2:Scale(s)  -> matrix2` | Geeft de matrix terug met elk element vermenigvuldigd met s. |
| `matrix2:Transpose()  -> matrix2` | Geeft de getransponeerde matrix terug. |
| `matrix2:Determinant()  -> number` | Geeft de determinant terug. |
| `matrix2:Clone()  -> matrix2` | Geeft een onafhankelijke kopie terug. |

### MATRIX3

Factory voor matrices van 3 bij 3.

<div class="callout warn">
Beschikbaar als de global MATRIX3. Get en Set controleren hun indices niet; houd ze binnen 1 tot 3.
</div>

| Methode | Beschrijving |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | Geeft de identiteitsmatrix terug. |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | Maakt een matrix uit zijn negen elementen in row-major-volgorde. |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | Geeft een rotatiematrix terug, opgebouwd uit de hoeken yaw, pitch en roll. |

### Matrix3-handle

Een matrix van 3 bij 3.

| Methode | Beschrijving |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | Geeft het element op rij r, kolom c terug. |
| `matrix3:Set(r, c, v)  -> nil` | Stelt het element op rij r, kolom c in. |
| `matrix3:Mul(o)  -> matrix3` | Geeft het matrixproduct terug. |
| `matrix3:MulVec(v)  -> vector3` | Geeft het product met een 3D-vector terug. |
| `matrix3:Scale(s)  -> matrix3` | Geeft de matrix terug met elk element vermenigvuldigd met s. |
| `matrix3:Transpose()  -> matrix3` | Geeft de getransponeerde matrix terug. |
| `matrix3:Determinant()  -> number` | Geeft de determinant terug. |
| `matrix3:Clone()  -> matrix3` | Geeft een onafhankelijke kopie terug. |

### MATRIX4

Factory voor matrices van 4 bij 4.

<div class="callout warn">
Beschikbaar als de global MATRIX4. Get en Set controleren hun indices niet; houd ze binnen 1 tot 4. Translation slaat de offsets op in de vierde kolom, zodat ze via MulVec op een vector4 met W = 1 van toepassing zijn.
</div>

| Methode | Beschrijving |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | Geeft de identiteitsmatrix terug. |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | Geeft een translatiematrix terug. |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | Geeft een schaalmatrix terug. |

### Matrix4-handle

Een matrix van 4 bij 4.

| Methode | Beschrijving |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | Geeft het element op rij r, kolom c terug. |
| `matrix4:Set(r, c, v)  -> nil` | Stelt het element op rij r, kolom c in. |
| `matrix4:Mul(o)  -> matrix4` | Geeft het matrixproduct terug. |
| `matrix4:MulVec(v)  -> vector4` | Geeft het product met een 4D-vector terug. |
| `matrix4:Scale(s)  -> matrix4` | Geeft de matrix terug met elk element vermenigvuldigd met s. |
| `matrix4:Transpose()  -> matrix4` | Geeft de getransponeerde matrix terug. |
| `matrix4:Clone()  -> matrix4` | Geeft een onafhankelijke kopie terug. |

## Quaternions

### QUATERNION

Factory voor quaternions.

<div class="callout warn">
Beschikbaar als de global QUATERNION. De identiteit is (0, 0, 0, 1). Rotatiemethoden gaan uit van eenheidsquaternions; roep Normalized aan nadat je er een met de hand hebt opgebouwd.
</div>

| Methode | Beschrijving |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | Geeft het identiteitsquaternion terug. |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | Maakt een quaternion uit zijn vier componenten. |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | Geeft de rotatie van `angle` radialen om de as (ax, ay, az) terug. De methode normaliseert de as; een nulas levert de identiteit op. |

### Quaternion-handle

Een quaternion met de velden X, Y, Z en W.

| Methode | Beschrijving |
| --- | --- |
| `quaternion.X  -> number` | De x-component (leesbaar en schrijfbaar veld). |
| `quaternion.Y  -> number` | De y-component (leesbaar en schrijfbaar veld). |
| `quaternion.Z  -> number` | De z-component (leesbaar en schrijfbaar veld). |
| `quaternion.W  -> number` | De w-component (leesbaar en schrijfbaar veld). |
| `quaternion:Mul(o)  -> quaternion` | Geeft het Hamilton-product this * o terug. Het resultaat toepassen roteert eerst met o, daarna met this. |
| `quaternion:Dot(o)  -> number` | Geeft het inproduct terug. |
| `quaternion:Length()  -> number` | Geeft de lengte terug. |
| `quaternion:Normalized()  -> quaternion` | Geeft een kopie met eenheidslengte terug, of de identiteit als de lengte bijna nul is. |
| `quaternion:Conjugate()  -> quaternion` | Geeft (-X, -Y, -Z, W) terug, de omgekeerde rotatie van een eenheidsquaternion. |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | Roteert de vector (vx, vy, vz) en geeft de resulterende x, y en z terug. |
| `quaternion:Slerp(o, t)  -> quaternion` | Geeft de sferische lineaire interpolatie richting o met fractie t terug, langs de kortste boog. |
| `quaternion:Clone()  -> quaternion` | Geeft een onafhankelijke kopie terug. |
| `quaternion:Unpack()  -> number, number, number, number` | Geeft X, Y, Z en W als vier waarden terug. |
