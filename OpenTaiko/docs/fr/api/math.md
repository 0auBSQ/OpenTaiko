<!-- api/math.md -->

# Mathématiques

Vecteurs, matrices et quaternions.

Chaque type se présente sous la forme d'une globale fabrique (VECTOR2, MATRIX4, QUATERNION, ...) qui crée des valeurs, et d'un type de handle portant les opérations. Conventions partagées par tous les types de cette page :

- Lisez et écrivez directement les champs de composantes (X, Y, Z, W). L'accès indexé (`Get`, `Set`) commence à 1.
- Les méthodes arithmétiques renvoient une nouvelle valeur et laissent leurs opérandes intacts. Seule `Set` modifie une valeur sur place.
- Les angles sont en radians.
- Les méthodes qui renvoient plusieurs nombres (`Unpack`, `RotateVec`) les renvoient sous forme de valeurs Lua multiples.

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## Vecteurs

### VECTOR

Fabrique de vecteurs de longueur quelconque.

<div class="callout warn">
Disponible comme la globale VECTOR. Les opérations entre deux vecteurs exigent des tailles égales ; une différence de taille renvoie un vecteur de longueur nulle (ou 0 pour les résultats scalaires) et ne lève aucune erreur.
</div>

| Méthode | Description |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | Crée un vecteur de longueur n rempli de zéros. |

### Handle de vecteur

Un vecteur de longueur quelconque avec des composantes indexées à partir de 1.

| Méthode | Description |
| --- | --- |
| `vector:Size()  -> number` | Renvoie le nombre de composantes. |
| `vector:Get(i)  -> number` | Renvoie la i-ème composante, ou 0 si i est hors limites. |
| `vector:Set(i, value)  -> nil` | Définit la i-ème composante ; la méthode ignore les indices hors limites. |
| `vector:Add(o)  -> vector` | Renvoie la somme composante par composante. |
| `vector:Sub(o)  -> vector` | Renvoie la différence composante par composante. |
| `vector:Mul(o)  -> vector` | Renvoie le produit composante par composante. |
| `vector:Scale(s)  -> vector` | Renvoie le vecteur dont chaque composante est multipliée par s. |
| `vector:Dot(o)  -> number` | Renvoie le produit scalaire. |
| `vector:Length()  -> number` | Renvoie la longueur euclidienne. |
| `vector:LengthSq()  -> number` | Renvoie la longueur au carré. |
| `vector:Distance(o)  -> number` | Renvoie la distance euclidienne à o. |
| `vector:Normalized()  -> vector` | Renvoie une copie de longueur unitaire, ou un vecteur nul si la longueur est proche de zéro. |
| `vector:Lerp(o, t)  -> vector` | Renvoie l'interpolation linéaire vers o selon la fraction t. |
| `vector:Clone()  -> vector` | Renvoie une copie indépendante. |

### VECTOR2

Fabrique de vecteurs 2D.

<div class="callout warn">
Disponible comme la globale VECTOR2.
</div>

| Méthode | Description |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | Crée un vecteur 2D. |
| `VECTOR2:Zero()  -> vector2` | Renvoie (0, 0). |
| `VECTOR2:One()  -> vector2` | Renvoie (1, 1). |

### Handle de vector2

Un vecteur 2D avec les champs X et Y.

| Méthode | Description |
| --- | --- |
| `vector2.X  -> number` | La composante x (champ en lecture et en écriture). |
| `vector2.Y  -> number` | La composante y (champ en lecture et en écriture). |
| `vector2:Add(o)  -> vector2` | Renvoie la somme composante par composante. |
| `vector2:Sub(o)  -> vector2` | Renvoie la différence composante par composante. |
| `vector2:Mul(o)  -> vector2` | Renvoie le produit composante par composante. |
| `vector2:Scale(s)  -> vector2` | Renvoie le vecteur multiplié par s. |
| `vector2:Negate()  -> vector2` | Renvoie (-X, -Y). |
| `vector2:Dot(o)  -> number` | Renvoie le produit scalaire. |
| `vector2:Cross(o)  -> number` | Renvoie le produit vectoriel scalaire (X * o.Y - Y * o.X). |
| `vector2:Length()  -> number` | Renvoie la longueur. |
| `vector2:LengthSq()  -> number` | Renvoie la longueur au carré. |
| `vector2:Distance(o)  -> number` | Renvoie la distance à o. |
| `vector2:Normalized()  -> vector2` | Renvoie une copie de longueur unitaire, ou (0, 0) si la longueur est proche de zéro. |
| `vector2:Lerp(o, t)  -> vector2` | Renvoie l'interpolation linéaire vers o selon la fraction t. |
| `vector2:Rotate(radians)  -> vector2` | Renvoie le vecteur pivoté dans le sens antihoraire de l'angle donné. |
| `vector2:Clone()  -> vector2` | Renvoie une copie indépendante. |
| `vector2:Set(x, y)  -> nil` | Définit X et Y sur place. |
| `vector2:Unpack()  -> number, number` | Renvoie X et Y sous forme de deux valeurs. |
| `vector2:ToString()  -> string` | Renvoie "(X, Y)". |

### VECTOR3

Fabrique de vecteurs 3D.

<div class="callout warn">
Disponible comme la globale VECTOR3.
</div>

| Méthode | Description |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | Crée un vecteur 3D. |
| `VECTOR3:Zero()  -> vector3` | Renvoie (0, 0, 0). |
| `VECTOR3:One()  -> vector3` | Renvoie (1, 1, 1). |

### Handle de vector3

Un vecteur 3D avec les champs X, Y et Z.

| Méthode | Description |
| --- | --- |
| `vector3.X  -> number` | La composante x (champ en lecture et en écriture). |
| `vector3.Y  -> number` | La composante y (champ en lecture et en écriture). |
| `vector3.Z  -> number` | La composante z (champ en lecture et en écriture). |
| `vector3:Add(o)  -> vector3` | Renvoie la somme composante par composante. |
| `vector3:Sub(o)  -> vector3` | Renvoie la différence composante par composante. |
| `vector3:Mul(o)  -> vector3` | Renvoie le produit composante par composante. |
| `vector3:Scale(s)  -> vector3` | Renvoie le vecteur multiplié par s. |
| `vector3:Negate()  -> vector3` | Renvoie (-X, -Y, -Z). |
| `vector3:Dot(o)  -> number` | Renvoie le produit scalaire. |
| `vector3:Cross(o)  -> vector3` | Renvoie le produit vectoriel. |
| `vector3:Length()  -> number` | Renvoie la longueur. |
| `vector3:LengthSq()  -> number` | Renvoie la longueur au carré. |
| `vector3:Distance(o)  -> number` | Renvoie la distance à o. |
| `vector3:Normalized()  -> vector3` | Renvoie une copie de longueur unitaire, ou (0, 0, 0) si la longueur est proche de zéro. |
| `vector3:Lerp(o, t)  -> vector3` | Renvoie l'interpolation linéaire vers o selon la fraction t. |
| `vector3:Reflect(n)  -> vector3` | Renvoie le vecteur réfléchi par rapport à la normale n (passez une n de longueur unitaire). |
| `vector3:Clone()  -> vector3` | Renvoie une copie indépendante. |
| `vector3:Set(x, y, z)  -> nil` | Définit X, Y et Z sur place. |
| `vector3:Unpack()  -> number, number, number` | Renvoie X, Y et Z sous forme de trois valeurs. |
| `vector3:ToString()  -> string` | Renvoie "(X, Y, Z)". |

### VECTOR4

Fabrique de vecteurs 4D.

<div class="callout warn">
Disponible comme la globale VECTOR4.
</div>

| Méthode | Description |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | Crée un vecteur 4D. |
| `VECTOR4:Zero()  -> vector4` | Renvoie (0, 0, 0, 0). |
| `VECTOR4:One()  -> vector4` | Renvoie (1, 1, 1, 1). |

### Handle de vector4

Un vecteur 4D avec les champs X, Y, Z et W.

| Méthode | Description |
| --- | --- |
| `vector4.X  -> number` | La composante x (champ en lecture et en écriture). |
| `vector4.Y  -> number` | La composante y (champ en lecture et en écriture). |
| `vector4.Z  -> number` | La composante z (champ en lecture et en écriture). |
| `vector4.W  -> number` | La composante w (champ en lecture et en écriture). |
| `vector4:Add(o)  -> vector4` | Renvoie la somme composante par composante. |
| `vector4:Sub(o)  -> vector4` | Renvoie la différence composante par composante. |
| `vector4:Mul(o)  -> vector4` | Renvoie le produit composante par composante. |
| `vector4:Scale(s)  -> vector4` | Renvoie le vecteur multiplié par s. |
| `vector4:Negate()  -> vector4` | Renvoie (-X, -Y, -Z, -W). |
| `vector4:Dot(o)  -> number` | Renvoie le produit scalaire. |
| `vector4:Length()  -> number` | Renvoie la longueur. |
| `vector4:LengthSq()  -> number` | Renvoie la longueur au carré. |
| `vector4:Distance(o)  -> number` | Renvoie la distance à o. |
| `vector4:Normalized()  -> vector4` | Renvoie une copie de longueur unitaire, ou (0, 0, 0, 0) si la longueur est proche de zéro. |
| `vector4:Lerp(o, t)  -> vector4` | Renvoie l'interpolation linéaire vers o selon la fraction t. |
| `vector4:Clone()  -> vector4` | Renvoie une copie indépendante. |
| `vector4:Set(x, y, z, w)  -> nil` | Définit X, Y, Z et W sur place. |
| `vector4:Unpack()  -> number, number, number, number` | Renvoie X, Y, Z et W sous forme de quatre valeurs. |
| `vector4:ToString()  -> string` | Renvoie "(X, Y, Z, W)". |

## Matrices

Toutes les matrices sont en ordre ligne-majeur : `Get(r, c)` lit la ligne r, colonne c, toutes deux à partir de 1. `MulVec` traite le vecteur comme un vecteur colonne (résultat = M * v).

### MATRIX

Fabrique de matrices de taille quelconque.

<div class="callout warn">
Disponible comme la globale MATRIX. Les opérations aux tailles incompatibles renvoient une matrice vide 0 par 0 (ou un vecteur de longueur nulle) et ne lèvent aucune erreur.
</div>

| Méthode | Description |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | Crée une matrice remplie de zéros. |
| `MATRIX:Identity(n)  -> matrix` | Crée une matrice identité n par n. |

### Handle de matrice

Une matrice de taille quelconque.

<div class="callout warn">
Add et Sub exigent des dimensions identiques ; Mul exige que le nombre de colonnes de cette matrice soit égal au nombre de lignes de l'autre ; MulVec exige que la taille du vecteur soit égale au nombre de colonnes.
</div>

| Méthode | Description |
| --- | --- |
| `matrix:RowCount()  -> number` | Renvoie le nombre de lignes. |
| `matrix:ColCount()  -> number` | Renvoie le nombre de colonnes. |
| `matrix:Get(r, c)  -> number` | Renvoie l'élément à la ligne r, colonne c, ou 0 si hors limites. |
| `matrix:Set(r, c, v)  -> nil` | Définit l'élément à la ligne r, colonne c ; la méthode ignore les indices hors limites. |
| `matrix:Add(o)  -> matrix` | Renvoie la somme élément par élément. |
| `matrix:Sub(o)  -> matrix` | Renvoie la différence élément par élément. |
| `matrix:Scale(s)  -> matrix` | Renvoie la matrice dont chaque élément est multiplié par s. |
| `matrix:Mul(o)  -> matrix` | Renvoie le produit matriciel. |
| `matrix:MulVec(v)  -> vector` | Renvoie le produit avec un vecteur (voir VECTOR). |
| `matrix:Transpose()  -> matrix` | Renvoie la matrice transposée. |
| `matrix:Clone()  -> matrix` | Renvoie une copie indépendante. |

### MATRIX2

Fabrique de matrices 2 par 2.

<div class="callout warn">
Disponible comme la globale MATRIX2. Get et Set ne vérifient pas leurs indices ; gardez-les entre 1 et 2.
</div>

| Méthode | Description |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | Renvoie la matrice identité. |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | Crée une matrice à partir de ses quatre éléments en ordre ligne-majeur. |
| `MATRIX2:Rotation(radians)  -> matrix2` | Renvoie une matrice de rotation dans le sens antihoraire. |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | Renvoie une matrice de mise à l'échelle. |

### Handle de matrix2

Une matrice 2 par 2.

| Méthode | Description |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | Renvoie l'élément à la ligne r, colonne c. |
| `matrix2:Set(r, c, v)  -> nil` | Définit l'élément à la ligne r, colonne c. |
| `matrix2:Mul(o)  -> matrix2` | Renvoie le produit matriciel. |
| `matrix2:MulVec(v)  -> vector2` | Renvoie le produit avec un vecteur 2D. |
| `matrix2:Scale(s)  -> matrix2` | Renvoie la matrice dont chaque élément est multiplié par s. |
| `matrix2:Transpose()  -> matrix2` | Renvoie la matrice transposée. |
| `matrix2:Determinant()  -> number` | Renvoie le déterminant. |
| `matrix2:Clone()  -> matrix2` | Renvoie une copie indépendante. |

### MATRIX3

Fabrique de matrices 3 par 3.

<div class="callout warn">
Disponible comme la globale MATRIX3. Get et Set ne vérifient pas leurs indices ; gardez-les entre 1 et 3.
</div>

| Méthode | Description |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | Renvoie la matrice identité. |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | Crée une matrice à partir de ses neuf éléments en ordre ligne-majeur. |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | Renvoie une matrice de rotation construite à partir des angles de lacet, de tangage et de roulis. |

### Handle de matrix3

Une matrice 3 par 3.

| Méthode | Description |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | Renvoie l'élément à la ligne r, colonne c. |
| `matrix3:Set(r, c, v)  -> nil` | Définit l'élément à la ligne r, colonne c. |
| `matrix3:Mul(o)  -> matrix3` | Renvoie le produit matriciel. |
| `matrix3:MulVec(v)  -> vector3` | Renvoie le produit avec un vecteur 3D. |
| `matrix3:Scale(s)  -> matrix3` | Renvoie la matrice dont chaque élément est multiplié par s. |
| `matrix3:Transpose()  -> matrix3` | Renvoie la matrice transposée. |
| `matrix3:Determinant()  -> number` | Renvoie le déterminant. |
| `matrix3:Clone()  -> matrix3` | Renvoie une copie indépendante. |

### MATRIX4

Fabrique de matrices 4 par 4.

<div class="callout warn">
Disponible comme la globale MATRIX4. Get et Set ne vérifient pas leurs indices ; gardez-les entre 1 et 4. Translation stocke les décalages dans la quatrième colonne, si bien qu'elle s'applique à un vector4 avec W = 1 via MulVec.
</div>

| Méthode | Description |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | Renvoie la matrice identité. |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | Renvoie une matrice de translation. |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | Renvoie une matrice de mise à l'échelle. |

### Handle de matrix4

Une matrice 4 par 4.

| Méthode | Description |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | Renvoie l'élément à la ligne r, colonne c. |
| `matrix4:Set(r, c, v)  -> nil` | Définit l'élément à la ligne r, colonne c. |
| `matrix4:Mul(o)  -> matrix4` | Renvoie le produit matriciel. |
| `matrix4:MulVec(v)  -> vector4` | Renvoie le produit avec un vecteur 4D. |
| `matrix4:Scale(s)  -> matrix4` | Renvoie la matrice dont chaque élément est multiplié par s. |
| `matrix4:Transpose()  -> matrix4` | Renvoie la matrice transposée. |
| `matrix4:Clone()  -> matrix4` | Renvoie une copie indépendante. |

## Quaternions

### QUATERNION

Fabrique de quaternions.

<div class="callout warn">
Disponible comme la globale QUATERNION. L'identité est (0, 0, 0, 1). Les méthodes de rotation supposent des quaternions unitaires ; appelez Normalized après en avoir construit un à la main.
</div>

| Méthode | Description |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | Renvoie le quaternion identité. |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | Crée un quaternion à partir de ses quatre composantes. |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | Renvoie la rotation de `angle` radians autour de l'axe (ax, ay, az). La méthode normalise l'axe ; un axe nul donne l'identité. |

### Handle de quaternion

Un quaternion avec les champs X, Y, Z et W.

| Méthode | Description |
| --- | --- |
| `quaternion.X  -> number` | La composante x (champ en lecture et en écriture). |
| `quaternion.Y  -> number` | La composante y (champ en lecture et en écriture). |
| `quaternion.Z  -> number` | La composante z (champ en lecture et en écriture). |
| `quaternion.W  -> number` | La composante w (champ en lecture et en écriture). |
| `quaternion:Mul(o)  -> quaternion` | Renvoie le produit de Hamilton this * o. Appliquer le résultat fait pivoter d'abord par o, puis par this. |
| `quaternion:Dot(o)  -> number` | Renvoie le produit scalaire. |
| `quaternion:Length()  -> number` | Renvoie la longueur. |
| `quaternion:Normalized()  -> quaternion` | Renvoie une copie de longueur unitaire, ou l'identité si la longueur est proche de zéro. |
| `quaternion:Conjugate()  -> quaternion` | Renvoie (-X, -Y, -Z, W), la rotation inverse d'un quaternion unitaire. |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | Fait pivoter le vecteur (vx, vy, vz) et renvoie les x, y et z résultants. |
| `quaternion:Slerp(o, t)  -> quaternion` | Renvoie l'interpolation linéaire sphérique vers o selon la fraction t, en prenant l'arc le plus court. |
| `quaternion:Clone()  -> quaternion` | Renvoie une copie indépendante. |
| `quaternion:Unpack()  -> number, number, number, number` | Renvoie X, Y, Z et W sous forme de quatre valeurs. |
