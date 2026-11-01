<!-- api/math.md -->

# Math

Vectors, matrices and quaternions.

Each type comes as a factory global (VECTOR2, MATRIX4, QUATERNION, ...) that creates values, and a handle type with the operations. Conventions shared by every type on this page:

- Read and write component fields (X, Y, Z, W) directly. Indexed access (`Get`, `Set`) is 1-based.
- Arithmetic methods return a new value and leave their operands untouched. Only `Set` modifies a value in place.
- Angles are in radians.
- Methods that return several numbers (`Unpack`, `RotateVec`) return them as multiple Lua values.

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## Vectors

### VECTOR

Factory for vectors of any length.

<div class="callout warn">
Available as the global VECTOR. Operations between two vectors require equal sizes; a size mismatch returns a zero-length vector (or 0 for scalar results) and raises no error.
</div>

| Method | Description |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | Creates a zero-filled vector of length n. |

### Vector handle

A vector of any length with 1-indexed components.

| Method | Description |
| --- | --- |
| `vector:Size()  -> number` | Returns the number of components. |
| `vector:Get(i)  -> number` | Returns the i-th component, or 0 if i is out of range. |
| `vector:Set(i, value)  -> nil` | Sets the i-th component; the method ignores out-of-range indices. |
| `vector:Add(o)  -> vector` | Returns the component-wise sum. |
| `vector:Sub(o)  -> vector` | Returns the component-wise difference. |
| `vector:Mul(o)  -> vector` | Returns the component-wise product. |
| `vector:Scale(s)  -> vector` | Returns the vector with every component multiplied by s. |
| `vector:Dot(o)  -> number` | Returns the dot product. |
| `vector:Length()  -> number` | Returns the Euclidean length. |
| `vector:LengthSq()  -> number` | Returns the squared length. |
| `vector:Distance(o)  -> number` | Returns the Euclidean distance to o. |
| `vector:Normalized()  -> vector` | Returns a unit-length copy, or a zero vector if the length is near zero. |
| `vector:Lerp(o, t)  -> vector` | Returns the linear interpolation toward o by fraction t. |
| `vector:Clone()  -> vector` | Returns an independent copy. |

### VECTOR2

Factory for 2D vectors.

<div class="callout warn">
Available as the global VECTOR2.
</div>

| Method | Description |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | Creates a 2D vector. |
| `VECTOR2:Zero()  -> vector2` | Returns (0, 0). |
| `VECTOR2:One()  -> vector2` | Returns (1, 1). |

### Vector2 handle

A 2D vector with X and Y fields.

| Method | Description |
| --- | --- |
| `vector2.X  -> number` | The x component (readable and writable field). |
| `vector2.Y  -> number` | The y component (readable and writable field). |
| `vector2:Add(o)  -> vector2` | Returns the component-wise sum. |
| `vector2:Sub(o)  -> vector2` | Returns the component-wise difference. |
| `vector2:Mul(o)  -> vector2` | Returns the component-wise product. |
| `vector2:Scale(s)  -> vector2` | Returns the vector multiplied by s. |
| `vector2:Negate()  -> vector2` | Returns (-X, -Y). |
| `vector2:Dot(o)  -> number` | Returns the dot product. |
| `vector2:Cross(o)  -> number` | Returns the scalar cross product (X * o.Y - Y * o.X). |
| `vector2:Length()  -> number` | Returns the length. |
| `vector2:LengthSq()  -> number` | Returns the squared length. |
| `vector2:Distance(o)  -> number` | Returns the distance to o. |
| `vector2:Normalized()  -> vector2` | Returns a unit-length copy, or (0, 0) if the length is near zero. |
| `vector2:Lerp(o, t)  -> vector2` | Returns the linear interpolation toward o by fraction t. |
| `vector2:Rotate(radians)  -> vector2` | Returns the vector rotated counter-clockwise by the given angle. |
| `vector2:Clone()  -> vector2` | Returns an independent copy. |
| `vector2:Set(x, y)  -> nil` | Sets X and Y in place. |
| `vector2:Unpack()  -> number, number` | Returns X and Y as two values. |
| `vector2:ToString()  -> string` | Returns "(X, Y)". |

### VECTOR3

Factory for 3D vectors.

<div class="callout warn">
Available as the global VECTOR3.
</div>

| Method | Description |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | Creates a 3D vector. |
| `VECTOR3:Zero()  -> vector3` | Returns (0, 0, 0). |
| `VECTOR3:One()  -> vector3` | Returns (1, 1, 1). |

### Vector3 handle

A 3D vector with X, Y and Z fields.

| Method | Description |
| --- | --- |
| `vector3.X  -> number` | The x component (readable and writable field). |
| `vector3.Y  -> number` | The y component (readable and writable field). |
| `vector3.Z  -> number` | The z component (readable and writable field). |
| `vector3:Add(o)  -> vector3` | Returns the component-wise sum. |
| `vector3:Sub(o)  -> vector3` | Returns the component-wise difference. |
| `vector3:Mul(o)  -> vector3` | Returns the component-wise product. |
| `vector3:Scale(s)  -> vector3` | Returns the vector multiplied by s. |
| `vector3:Negate()  -> vector3` | Returns (-X, -Y, -Z). |
| `vector3:Dot(o)  -> number` | Returns the dot product. |
| `vector3:Cross(o)  -> vector3` | Returns the cross product. |
| `vector3:Length()  -> number` | Returns the length. |
| `vector3:LengthSq()  -> number` | Returns the squared length. |
| `vector3:Distance(o)  -> number` | Returns the distance to o. |
| `vector3:Normalized()  -> vector3` | Returns a unit-length copy, or (0, 0, 0) if the length is near zero. |
| `vector3:Lerp(o, t)  -> vector3` | Returns the linear interpolation toward o by fraction t. |
| `vector3:Reflect(n)  -> vector3` | Returns the vector reflected about the normal n (pass a unit-length n). |
| `vector3:Clone()  -> vector3` | Returns an independent copy. |
| `vector3:Set(x, y, z)  -> nil` | Sets X, Y and Z in place. |
| `vector3:Unpack()  -> number, number, number` | Returns X, Y and Z as three values. |
| `vector3:ToString()  -> string` | Returns "(X, Y, Z)". |

### VECTOR4

Factory for 4D vectors.

<div class="callout warn">
Available as the global VECTOR4.
</div>

| Method | Description |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | Creates a 4D vector. |
| `VECTOR4:Zero()  -> vector4` | Returns (0, 0, 0, 0). |
| `VECTOR4:One()  -> vector4` | Returns (1, 1, 1, 1). |

### Vector4 handle

A 4D vector with X, Y, Z and W fields.

| Method | Description |
| --- | --- |
| `vector4.X  -> number` | The x component (readable and writable field). |
| `vector4.Y  -> number` | The y component (readable and writable field). |
| `vector4.Z  -> number` | The z component (readable and writable field). |
| `vector4.W  -> number` | The w component (readable and writable field). |
| `vector4:Add(o)  -> vector4` | Returns the component-wise sum. |
| `vector4:Sub(o)  -> vector4` | Returns the component-wise difference. |
| `vector4:Mul(o)  -> vector4` | Returns the component-wise product. |
| `vector4:Scale(s)  -> vector4` | Returns the vector multiplied by s. |
| `vector4:Negate()  -> vector4` | Returns (-X, -Y, -Z, -W). |
| `vector4:Dot(o)  -> number` | Returns the dot product. |
| `vector4:Length()  -> number` | Returns the length. |
| `vector4:LengthSq()  -> number` | Returns the squared length. |
| `vector4:Distance(o)  -> number` | Returns the distance to o. |
| `vector4:Normalized()  -> vector4` | Returns a unit-length copy, or (0, 0, 0, 0) if the length is near zero. |
| `vector4:Lerp(o, t)  -> vector4` | Returns the linear interpolation toward o by fraction t. |
| `vector4:Clone()  -> vector4` | Returns an independent copy. |
| `vector4:Set(x, y, z, w)  -> nil` | Sets X, Y, Z and W in place. |
| `vector4:Unpack()  -> number, number, number, number` | Returns X, Y, Z and W as four values. |
| `vector4:ToString()  -> string` | Returns "(X, Y, Z, W)". |

## Matrices

All matrices are row-major: `Get(r, c)` reads row r, column c, both 1-based. `MulVec` treats the vector as a column vector (result = M * v).

### MATRIX

Factory for matrices of any size.

<div class="callout warn">
Available as the global MATRIX. Size-incompatible operations return an empty 0 by 0 matrix (or a zero-length vector) and raise no error.
</div>

| Method | Description |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | Creates a zero-filled matrix. |
| `MATRIX:Identity(n)  -> matrix` | Creates an n by n identity matrix. |

### Matrix handle

A matrix of any size.

<div class="callout warn">
Add and Sub require matching dimensions; Mul requires this matrix's column count to equal the other's row count; MulVec requires the vector size to equal the column count.
</div>

| Method | Description |
| --- | --- |
| `matrix:RowCount()  -> number` | Returns the number of rows. |
| `matrix:ColCount()  -> number` | Returns the number of columns. |
| `matrix:Get(r, c)  -> number` | Returns the element at row r, column c, or 0 if out of range. |
| `matrix:Set(r, c, v)  -> nil` | Sets the element at row r, column c; the method ignores out-of-range indices. |
| `matrix:Add(o)  -> matrix` | Returns the element-wise sum. |
| `matrix:Sub(o)  -> matrix` | Returns the element-wise difference. |
| `matrix:Scale(s)  -> matrix` | Returns the matrix with every element multiplied by s. |
| `matrix:Mul(o)  -> matrix` | Returns the matrix product. |
| `matrix:MulVec(v)  -> vector` | Returns the product with a vector (see VECTOR). |
| `matrix:Transpose()  -> matrix` | Returns the transposed matrix. |
| `matrix:Clone()  -> matrix` | Returns an independent copy. |

### MATRIX2

Factory for 2 by 2 matrices.

<div class="callout warn">
Available as the global MATRIX2. Get and Set do not check their indices; keep them within 1 to 2.
</div>

| Method | Description |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | Returns the identity matrix. |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | Creates a matrix from its four elements in row-major order. |
| `MATRIX2:Rotation(radians)  -> matrix2` | Returns a counter-clockwise rotation matrix. |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | Returns a scaling matrix. |

### Matrix2 handle

A 2 by 2 matrix.

| Method | Description |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | Returns the element at row r, column c. |
| `matrix2:Set(r, c, v)  -> nil` | Sets the element at row r, column c. |
| `matrix2:Mul(o)  -> matrix2` | Returns the matrix product. |
| `matrix2:MulVec(v)  -> vector2` | Returns the product with a 2D vector. |
| `matrix2:Scale(s)  -> matrix2` | Returns the matrix with every element multiplied by s. |
| `matrix2:Transpose()  -> matrix2` | Returns the transposed matrix. |
| `matrix2:Determinant()  -> number` | Returns the determinant. |
| `matrix2:Clone()  -> matrix2` | Returns an independent copy. |

### MATRIX3

Factory for 3 by 3 matrices.

<div class="callout warn">
Available as the global MATRIX3. Get and Set do not check their indices; keep them within 1 to 3.
</div>

| Method | Description |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | Returns the identity matrix. |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | Creates a matrix from its nine elements in row-major order. |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | Returns a rotation matrix built from yaw, pitch and roll angles. |

### Matrix3 handle

A 3 by 3 matrix.

| Method | Description |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | Returns the element at row r, column c. |
| `matrix3:Set(r, c, v)  -> nil` | Sets the element at row r, column c. |
| `matrix3:Mul(o)  -> matrix3` | Returns the matrix product. |
| `matrix3:MulVec(v)  -> vector3` | Returns the product with a 3D vector. |
| `matrix3:Scale(s)  -> matrix3` | Returns the matrix with every element multiplied by s. |
| `matrix3:Transpose()  -> matrix3` | Returns the transposed matrix. |
| `matrix3:Determinant()  -> number` | Returns the determinant. |
| `matrix3:Clone()  -> matrix3` | Returns an independent copy. |

### MATRIX4

Factory for 4 by 4 matrices.

<div class="callout warn">
Available as the global MATRIX4. Get and Set do not check their indices; keep them within 1 to 4. Translation stores the offsets in the fourth column, so it applies to a vector4 with W = 1 through MulVec.
</div>

| Method | Description |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | Returns the identity matrix. |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | Returns a translation matrix. |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | Returns a scaling matrix. |

### Matrix4 handle

A 4 by 4 matrix.

| Method | Description |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | Returns the element at row r, column c. |
| `matrix4:Set(r, c, v)  -> nil` | Sets the element at row r, column c. |
| `matrix4:Mul(o)  -> matrix4` | Returns the matrix product. |
| `matrix4:MulVec(v)  -> vector4` | Returns the product with a 4D vector. |
| `matrix4:Scale(s)  -> matrix4` | Returns the matrix with every element multiplied by s. |
| `matrix4:Transpose()  -> matrix4` | Returns the transposed matrix. |
| `matrix4:Clone()  -> matrix4` | Returns an independent copy. |

## Quaternions

### QUATERNION

Factory for quaternions.

<div class="callout warn">
Available as the global QUATERNION. The identity is (0, 0, 0, 1). Rotation methods assume unit quaternions; call Normalized after building one by hand.
</div>

| Method | Description |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | Returns the identity quaternion. |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | Creates a quaternion from its four components. |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | Returns the rotation of `angle` radians about the axis (ax, ay, az). The method normalizes the axis; a zero axis yields the identity. |

### Quaternion handle

A quaternion with X, Y, Z and W fields.

| Method | Description |
| --- | --- |
| `quaternion.X  -> number` | The x component (readable and writable field). |
| `quaternion.Y  -> number` | The y component (readable and writable field). |
| `quaternion.Z  -> number` | The z component (readable and writable field). |
| `quaternion.W  -> number` | The w component (readable and writable field). |
| `quaternion:Mul(o)  -> quaternion` | Returns the Hamilton product this * o. Applying the result rotates by o first, then by this. |
| `quaternion:Dot(o)  -> number` | Returns the dot product. |
| `quaternion:Length()  -> number` | Returns the length. |
| `quaternion:Normalized()  -> quaternion` | Returns a unit-length copy, or the identity if the length is near zero. |
| `quaternion:Conjugate()  -> quaternion` | Returns (-X, -Y, -Z, W), the inverse rotation of a unit quaternion. |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | Rotates the vector (vx, vy, vz) and returns the resulting x, y and z. |
| `quaternion:Slerp(o, t)  -> quaternion` | Returns the spherical linear interpolation toward o by fraction t, taking the shortest arc. |
| `quaternion:Clone()  -> quaternion` | Returns an independent copy. |
| `quaternion:Unpack()  -> number, number, number, number` | Returns X, Y, Z and W as four values. |
