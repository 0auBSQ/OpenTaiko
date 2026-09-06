<!-- api/math.md -->

# Matemáticas

Vectores, matrices y cuaterniones.

Cada tipo viene como una global de fábrica (VECTOR2, MATRIX4, QUATERNION, ...) que crea valores, y un tipo de handle con las operaciones. Convenciones compartidas por todos los tipos de esta página:

- Lee y escribe los campos de componentes (X, Y, Z, W) directamente. El acceso indexado (`Get`, `Set`) empieza en 1.
- Los métodos aritméticos devuelven un valor nuevo y dejan intactos sus operandos. Solo `Set` modifica un valor en el sitio.
- Los ángulos están en radianes.
- Los métodos que devuelven varios números (`Unpack`, `RotateVec`) los devuelven como múltiples valores Lua.

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## Vectores

### VECTOR

Fábrica de vectores de cualquier longitud.

<div class="callout warn">
Disponible como la global VECTOR. Las operaciones entre dos vectores requieren tamaños iguales; una discrepancia de tamaño devuelve un vector de longitud cero (o 0 para resultados escalares) y no lanza ningún error.
</div>

| Método | Descripción |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | Crea un vector de longitud n relleno con ceros. |

### Handle de vector

Un vector de cualquier longitud con componentes indexados desde 1.

| Método | Descripción |
| --- | --- |
| `vector:Size()  -> number` | Devuelve el número de componentes. |
| `vector:Get(i)  -> number` | Devuelve el componente i-ésimo, o 0 si i está fuera de rango. |
| `vector:Set(i, value)  -> nil` | Establece el componente i-ésimo; el método ignora los índices fuera de rango. |
| `vector:Add(o)  -> vector` | Devuelve la suma componente a componente. |
| `vector:Sub(o)  -> vector` | Devuelve la diferencia componente a componente. |
| `vector:Mul(o)  -> vector` | Devuelve el producto componente a componente. |
| `vector:Scale(s)  -> vector` | Devuelve el vector con cada componente multiplicado por s. |
| `vector:Dot(o)  -> number` | Devuelve el producto escalar. |
| `vector:Length()  -> number` | Devuelve la longitud euclidiana. |
| `vector:LengthSq()  -> number` | Devuelve la longitud al cuadrado. |
| `vector:Distance(o)  -> number` | Devuelve la distancia euclidiana a o. |
| `vector:Normalized()  -> vector` | Devuelve una copia de longitud unitaria, o un vector cero si la longitud es casi cero. |
| `vector:Lerp(o, t)  -> vector` | Devuelve la interpolación lineal hacia o en la fracción t. |
| `vector:Clone()  -> vector` | Devuelve una copia independiente. |

### VECTOR2

Fábrica de vectores 2D.

<div class="callout warn">
Disponible como la global VECTOR2.
</div>

| Método | Descripción |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | Crea un vector 2D. |
| `VECTOR2:Zero()  -> vector2` | Devuelve (0, 0). |
| `VECTOR2:One()  -> vector2` | Devuelve (1, 1). |

### Handle de vector2

Un vector 2D con campos X e Y.

| Método | Descripción |
| --- | --- |
| `vector2.X  -> number` | El componente x (campo de lectura y escritura). |
| `vector2.Y  -> number` | El componente y (campo de lectura y escritura). |
| `vector2:Add(o)  -> vector2` | Devuelve la suma componente a componente. |
| `vector2:Sub(o)  -> vector2` | Devuelve la diferencia componente a componente. |
| `vector2:Mul(o)  -> vector2` | Devuelve el producto componente a componente. |
| `vector2:Scale(s)  -> vector2` | Devuelve el vector multiplicado por s. |
| `vector2:Negate()  -> vector2` | Devuelve (-X, -Y). |
| `vector2:Dot(o)  -> number` | Devuelve el producto escalar. |
| `vector2:Cross(o)  -> number` | Devuelve el producto vectorial escalar (X * o.Y - Y * o.X). |
| `vector2:Length()  -> number` | Devuelve la longitud. |
| `vector2:LengthSq()  -> number` | Devuelve la longitud al cuadrado. |
| `vector2:Distance(o)  -> number` | Devuelve la distancia a o. |
| `vector2:Normalized()  -> vector2` | Devuelve una copia de longitud unitaria, o (0, 0) si la longitud es casi cero. |
| `vector2:Lerp(o, t)  -> vector2` | Devuelve la interpolación lineal hacia o en la fracción t. |
| `vector2:Rotate(radians)  -> vector2` | Devuelve el vector rotado en sentido antihorario el ángulo indicado. |
| `vector2:Clone()  -> vector2` | Devuelve una copia independiente. |
| `vector2:Set(x, y)  -> nil` | Establece X e Y en el sitio. |
| `vector2:Unpack()  -> number, number` | Devuelve X e Y como dos valores. |
| `vector2:ToString()  -> string` | Devuelve "(X, Y)". |

### VECTOR3

Fábrica de vectores 3D.

<div class="callout warn">
Disponible como la global VECTOR3.
</div>

| Método | Descripción |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | Crea un vector 3D. |
| `VECTOR3:Zero()  -> vector3` | Devuelve (0, 0, 0). |
| `VECTOR3:One()  -> vector3` | Devuelve (1, 1, 1). |

### Handle de vector3

Un vector 3D con campos X, Y y Z.

| Método | Descripción |
| --- | --- |
| `vector3.X  -> number` | El componente x (campo de lectura y escritura). |
| `vector3.Y  -> number` | El componente y (campo de lectura y escritura). |
| `vector3.Z  -> number` | El componente z (campo de lectura y escritura). |
| `vector3:Add(o)  -> vector3` | Devuelve la suma componente a componente. |
| `vector3:Sub(o)  -> vector3` | Devuelve la diferencia componente a componente. |
| `vector3:Mul(o)  -> vector3` | Devuelve el producto componente a componente. |
| `vector3:Scale(s)  -> vector3` | Devuelve el vector multiplicado por s. |
| `vector3:Negate()  -> vector3` | Devuelve (-X, -Y, -Z). |
| `vector3:Dot(o)  -> number` | Devuelve el producto escalar. |
| `vector3:Cross(o)  -> vector3` | Devuelve el producto vectorial. |
| `vector3:Length()  -> number` | Devuelve la longitud. |
| `vector3:LengthSq()  -> number` | Devuelve la longitud al cuadrado. |
| `vector3:Distance(o)  -> number` | Devuelve la distancia a o. |
| `vector3:Normalized()  -> vector3` | Devuelve una copia de longitud unitaria, o (0, 0, 0) si la longitud es casi cero. |
| `vector3:Lerp(o, t)  -> vector3` | Devuelve la interpolación lineal hacia o en la fracción t. |
| `vector3:Reflect(n)  -> vector3` | Devuelve el vector reflejado respecto a la normal n (pasa una n de longitud unitaria). |
| `vector3:Clone()  -> vector3` | Devuelve una copia independiente. |
| `vector3:Set(x, y, z)  -> nil` | Establece X, Y y Z en el sitio. |
| `vector3:Unpack()  -> number, number, number` | Devuelve X, Y y Z como tres valores. |
| `vector3:ToString()  -> string` | Devuelve "(X, Y, Z)". |

### VECTOR4

Fábrica de vectores 4D.

<div class="callout warn">
Disponible como la global VECTOR4.
</div>

| Método | Descripción |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | Crea un vector 4D. |
| `VECTOR4:Zero()  -> vector4` | Devuelve (0, 0, 0, 0). |
| `VECTOR4:One()  -> vector4` | Devuelve (1, 1, 1, 1). |

### Handle de vector4

Un vector 4D con campos X, Y, Z y W.

| Método | Descripción |
| --- | --- |
| `vector4.X  -> number` | El componente x (campo de lectura y escritura). |
| `vector4.Y  -> number` | El componente y (campo de lectura y escritura). |
| `vector4.Z  -> number` | El componente z (campo de lectura y escritura). |
| `vector4.W  -> number` | El componente w (campo de lectura y escritura). |
| `vector4:Add(o)  -> vector4` | Devuelve la suma componente a componente. |
| `vector4:Sub(o)  -> vector4` | Devuelve la diferencia componente a componente. |
| `vector4:Mul(o)  -> vector4` | Devuelve el producto componente a componente. |
| `vector4:Scale(s)  -> vector4` | Devuelve el vector multiplicado por s. |
| `vector4:Negate()  -> vector4` | Devuelve (-X, -Y, -Z, -W). |
| `vector4:Dot(o)  -> number` | Devuelve el producto escalar. |
| `vector4:Length()  -> number` | Devuelve la longitud. |
| `vector4:LengthSq()  -> number` | Devuelve la longitud al cuadrado. |
| `vector4:Distance(o)  -> number` | Devuelve la distancia a o. |
| `vector4:Normalized()  -> vector4` | Devuelve una copia de longitud unitaria, o (0, 0, 0, 0) si la longitud es casi cero. |
| `vector4:Lerp(o, t)  -> vector4` | Devuelve la interpolación lineal hacia o en la fracción t. |
| `vector4:Clone()  -> vector4` | Devuelve una copia independiente. |
| `vector4:Set(x, y, z, w)  -> nil` | Establece X, Y, Z y W en el sitio. |
| `vector4:Unpack()  -> number, number, number, number` | Devuelve X, Y, Z y W como cuatro valores. |
| `vector4:ToString()  -> string` | Devuelve "(X, Y, Z, W)". |

## Matrices

Todas las matrices son por filas (row-major): `Get(r, c)` lee la fila r, columna c, ambas desde 1. `MulVec` trata el vector como un vector columna (resultado = M * v).

### MATRIX

Fábrica de matrices de cualquier tamaño.

<div class="callout warn">
Disponible como la global MATRIX. Las operaciones con tamaños incompatibles devuelven una matriz vacía de 0 por 0 (o un vector de longitud cero) y no lanzan ningún error.
</div>

| Método | Descripción |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | Crea una matriz rellena con ceros. |
| `MATRIX:Identity(n)  -> matrix` | Crea una matriz identidad de n por n. |

### Handle de matriz

Una matriz de cualquier tamaño.

<div class="callout warn">
Add y Sub requieren dimensiones coincidentes; Mul requiere que el número de columnas de esta matriz sea igual al número de filas de la otra; MulVec requiere que el tamaño del vector sea igual al número de columnas.
</div>

| Método | Descripción |
| --- | --- |
| `matrix:RowCount()  -> number` | Devuelve el número de filas. |
| `matrix:ColCount()  -> number` | Devuelve el número de columnas. |
| `matrix:Get(r, c)  -> number` | Devuelve el elemento en la fila r, columna c, o 0 si está fuera de rango. |
| `matrix:Set(r, c, v)  -> nil` | Establece el elemento en la fila r, columna c; el método ignora los índices fuera de rango. |
| `matrix:Add(o)  -> matrix` | Devuelve la suma elemento a elemento. |
| `matrix:Sub(o)  -> matrix` | Devuelve la diferencia elemento a elemento. |
| `matrix:Scale(s)  -> matrix` | Devuelve la matriz con cada elemento multiplicado por s. |
| `matrix:Mul(o)  -> matrix` | Devuelve el producto de matrices. |
| `matrix:MulVec(v)  -> vector` | Devuelve el producto con un vector (consulta VECTOR). |
| `matrix:Transpose()  -> matrix` | Devuelve la matriz transpuesta. |
| `matrix:Clone()  -> matrix` | Devuelve una copia independiente. |

### MATRIX2

Fábrica de matrices de 2 por 2.

<div class="callout warn">
Disponible como la global MATRIX2. Get y Set no comprueban sus índices; mantenlos entre 1 y 2.
</div>

| Método | Descripción |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | Devuelve la matriz identidad. |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | Crea una matriz a partir de sus cuatro elementos en orden por filas. |
| `MATRIX2:Rotation(radians)  -> matrix2` | Devuelve una matriz de rotación en sentido antihorario. |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | Devuelve una matriz de escalado. |

### Handle de matriz2

Una matriz de 2 por 2.

| Método | Descripción |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | Devuelve el elemento en la fila r, columna c. |
| `matrix2:Set(r, c, v)  -> nil` | Establece el elemento en la fila r, columna c. |
| `matrix2:Mul(o)  -> matrix2` | Devuelve el producto de matrices. |
| `matrix2:MulVec(v)  -> vector2` | Devuelve el producto con un vector 2D. |
| `matrix2:Scale(s)  -> matrix2` | Devuelve la matriz con cada elemento multiplicado por s. |
| `matrix2:Transpose()  -> matrix2` | Devuelve la matriz transpuesta. |
| `matrix2:Determinant()  -> number` | Devuelve el determinante. |
| `matrix2:Clone()  -> matrix2` | Devuelve una copia independiente. |

### MATRIX3

Fábrica de matrices de 3 por 3.

<div class="callout warn">
Disponible como la global MATRIX3. Get y Set no comprueban sus índices; mantenlos entre 1 y 3.
</div>

| Método | Descripción |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | Devuelve la matriz identidad. |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | Crea una matriz a partir de sus nueve elementos en orden por filas. |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | Devuelve una matriz de rotación construida a partir de los ángulos de guiñada, cabeceo y alabeo. |

### Handle de matriz3

Una matriz de 3 por 3.

| Método | Descripción |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | Devuelve el elemento en la fila r, columna c. |
| `matrix3:Set(r, c, v)  -> nil` | Establece el elemento en la fila r, columna c. |
| `matrix3:Mul(o)  -> matrix3` | Devuelve el producto de matrices. |
| `matrix3:MulVec(v)  -> vector3` | Devuelve el producto con un vector 3D. |
| `matrix3:Scale(s)  -> matrix3` | Devuelve la matriz con cada elemento multiplicado por s. |
| `matrix3:Transpose()  -> matrix3` | Devuelve la matriz transpuesta. |
| `matrix3:Determinant()  -> number` | Devuelve el determinante. |
| `matrix3:Clone()  -> matrix3` | Devuelve una copia independiente. |

### MATRIX4

Fábrica de matrices de 4 por 4.

<div class="callout warn">
Disponible como la global MATRIX4. Get y Set no comprueban sus índices; mantenlos entre 1 y 4. Translation guarda los desplazamientos en la cuarta columna, así que se aplica a un vector4 con W = 1 mediante MulVec.
</div>

| Método | Descripción |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | Devuelve la matriz identidad. |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | Devuelve una matriz de traslación. |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | Devuelve una matriz de escalado. |

### Handle de matriz4

Una matriz de 4 por 4.

| Método | Descripción |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | Devuelve el elemento en la fila r, columna c. |
| `matrix4:Set(r, c, v)  -> nil` | Establece el elemento en la fila r, columna c. |
| `matrix4:Mul(o)  -> matrix4` | Devuelve el producto de matrices. |
| `matrix4:MulVec(v)  -> vector4` | Devuelve el producto con un vector 4D. |
| `matrix4:Scale(s)  -> matrix4` | Devuelve la matriz con cada elemento multiplicado por s. |
| `matrix4:Transpose()  -> matrix4` | Devuelve la matriz transpuesta. |
| `matrix4:Clone()  -> matrix4` | Devuelve una copia independiente. |

## Cuaterniones

### QUATERNION

Fábrica de cuaterniones.

<div class="callout warn">
Disponible como la global QUATERNION. La identidad es (0, 0, 0, 1). Los métodos de rotación asumen cuaterniones unitarios; llama a Normalized tras construir uno a mano.
</div>

| Método | Descripción |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | Devuelve el cuaternión identidad. |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | Crea un cuaternión a partir de sus cuatro componentes. |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | Devuelve la rotación de `angle` radianes alrededor del eje (ax, ay, az). El método normaliza el eje; un eje cero produce la identidad. |

### Handle de cuaternión

Un cuaternión con campos X, Y, Z y W.

| Método | Descripción |
| --- | --- |
| `quaternion.X  -> number` | El componente x (campo de lectura y escritura). |
| `quaternion.Y  -> number` | El componente y (campo de lectura y escritura). |
| `quaternion.Z  -> number` | El componente z (campo de lectura y escritura). |
| `quaternion.W  -> number` | El componente w (campo de lectura y escritura). |
| `quaternion:Mul(o)  -> quaternion` | Devuelve el producto de Hamilton this * o. Aplicar el resultado rota primero por o y luego por this. |
| `quaternion:Dot(o)  -> number` | Devuelve el producto escalar. |
| `quaternion:Length()  -> number` | Devuelve la longitud. |
| `quaternion:Normalized()  -> quaternion` | Devuelve una copia de longitud unitaria, o la identidad si la longitud es casi cero. |
| `quaternion:Conjugate()  -> quaternion` | Devuelve (-X, -Y, -Z, W), la rotación inversa de un cuaternión unitario. |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | Rota el vector (vx, vy, vz) y devuelve las x, y y z resultantes. |
| `quaternion:Slerp(o, t)  -> quaternion` | Devuelve la interpolación lineal esférica hacia o en la fracción t, tomando el arco más corto. |
| `quaternion:Clone()  -> quaternion` | Devuelve una copia independiente. |
| `quaternion:Unpack()  -> number, number, number, number` | Devuelve X, Y, Z y W como cuatro valores. |
