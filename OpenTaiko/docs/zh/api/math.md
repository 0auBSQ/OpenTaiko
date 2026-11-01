<!-- api/math.md -->

# 数学

向量、矩阵和四元数。

每种类型都有一个创建值的工厂全局对象（VECTOR2、MATRIX4、QUATERNION 等）和一个带有运算的句柄类型。本页所有类型共用的约定：

- 直接读写分量字段（X、Y、Z、W）。索引访问（`Get`、`Set`）从 1 起。
- 算术方法返回新值，不改变操作数。只有 `Set` 会就地修改值。
- 角度以弧度为单位。
- 返回多个数字的方法（`Unpack`、`RotateVec`）以多个 Lua 值返回它们。

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## 向量

### VECTOR

任意长度向量的工厂。

<div class="callout warn">
以全局对象 VECTOR 的形式提供。两个向量之间的运算要求长度相等；长度不匹配时返回零长度向量（标量结果则返回 0），不抛出错误。
</div>

| 方法 | 说明 |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | 创建一个长度为 n、全零的向量。 |

### 向量句柄

分量从 1 起索引的任意长度向量。

| 方法 | 说明 |
| --- | --- |
| `vector:Size()  -> number` | 返回分量数量。 |
| `vector:Get(i)  -> number` | 返回第 i 个分量；i 超出范围时返回 0。 |
| `vector:Set(i, value)  -> nil` | 设置第 i 个分量；该方法会忽略超出范围的索引。 |
| `vector:Add(o)  -> vector` | 返回逐分量之和。 |
| `vector:Sub(o)  -> vector` | 返回逐分量之差。 |
| `vector:Mul(o)  -> vector` | 返回逐分量之积。 |
| `vector:Scale(s)  -> vector` | 返回每个分量都乘以 s 的向量。 |
| `vector:Dot(o)  -> number` | 返回点积。 |
| `vector:Length()  -> number` | 返回欧几里得长度。 |
| `vector:LengthSq()  -> number` | 返回长度的平方。 |
| `vector:Distance(o)  -> number` | 返回到 o 的欧几里得距离。 |
| `vector:Normalized()  -> vector` | 返回单位长度的副本；长度接近零时返回零向量。 |
| `vector:Lerp(o, t)  -> vector` | 返回向 o 按比例 t 的线性插值。 |
| `vector:Clone()  -> vector` | 返回一个独立的副本。 |

### VECTOR2

2D 向量的工厂。

<div class="callout warn">
以全局对象 VECTOR2 的形式提供。
</div>

| 方法 | 说明 |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | 创建一个 2D 向量。 |
| `VECTOR2:Zero()  -> vector2` | 返回 (0, 0)。 |
| `VECTOR2:One()  -> vector2` | 返回 (1, 1)。 |

### Vector2 句柄

带有 X 和 Y 字段的 2D 向量。

| 方法 | 说明 |
| --- | --- |
| `vector2.X  -> number` | x 分量（可读写字段）。 |
| `vector2.Y  -> number` | y 分量（可读写字段）。 |
| `vector2:Add(o)  -> vector2` | 返回逐分量之和。 |
| `vector2:Sub(o)  -> vector2` | 返回逐分量之差。 |
| `vector2:Mul(o)  -> vector2` | 返回逐分量之积。 |
| `vector2:Scale(s)  -> vector2` | 返回乘以 s 的向量。 |
| `vector2:Negate()  -> vector2` | 返回 (-X, -Y)。 |
| `vector2:Dot(o)  -> number` | 返回点积。 |
| `vector2:Cross(o)  -> number` | 返回标量叉积（X * o.Y - Y * o.X）。 |
| `vector2:Length()  -> number` | 返回长度。 |
| `vector2:LengthSq()  -> number` | 返回长度的平方。 |
| `vector2:Distance(o)  -> number` | 返回到 o 的距离。 |
| `vector2:Normalized()  -> vector2` | 返回单位长度的副本；长度接近零时返回 (0, 0)。 |
| `vector2:Lerp(o, t)  -> vector2` | 返回向 o 按比例 t 的线性插值。 |
| `vector2:Rotate(radians)  -> vector2` | 返回逆时针旋转给定角度后的向量。 |
| `vector2:Clone()  -> vector2` | 返回一个独立的副本。 |
| `vector2:Set(x, y)  -> nil` | 就地设置 X 和 Y。 |
| `vector2:Unpack()  -> number, number` | 以两个值返回 X 和 Y。 |
| `vector2:ToString()  -> string` | 返回 "(X, Y)"。 |

### VECTOR3

3D 向量的工厂。

<div class="callout warn">
以全局对象 VECTOR3 的形式提供。
</div>

| 方法 | 说明 |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | 创建一个 3D 向量。 |
| `VECTOR3:Zero()  -> vector3` | 返回 (0, 0, 0)。 |
| `VECTOR3:One()  -> vector3` | 返回 (1, 1, 1)。 |

### Vector3 句柄

带有 X、Y 和 Z 字段的 3D 向量。

| 方法 | 说明 |
| --- | --- |
| `vector3.X  -> number` | x 分量（可读写字段）。 |
| `vector3.Y  -> number` | y 分量（可读写字段）。 |
| `vector3.Z  -> number` | z 分量（可读写字段）。 |
| `vector3:Add(o)  -> vector3` | 返回逐分量之和。 |
| `vector3:Sub(o)  -> vector3` | 返回逐分量之差。 |
| `vector3:Mul(o)  -> vector3` | 返回逐分量之积。 |
| `vector3:Scale(s)  -> vector3` | 返回乘以 s 的向量。 |
| `vector3:Negate()  -> vector3` | 返回 (-X, -Y, -Z)。 |
| `vector3:Dot(o)  -> number` | 返回点积。 |
| `vector3:Cross(o)  -> vector3` | 返回叉积。 |
| `vector3:Length()  -> number` | 返回长度。 |
| `vector3:LengthSq()  -> number` | 返回长度的平方。 |
| `vector3:Distance(o)  -> number` | 返回到 o 的距离。 |
| `vector3:Normalized()  -> vector3` | 返回单位长度的副本；长度接近零时返回 (0, 0, 0)。 |
| `vector3:Lerp(o, t)  -> vector3` | 返回向 o 按比例 t 的线性插值。 |
| `vector3:Reflect(n)  -> vector3` | 返回关于法线 n 反射后的向量（请传入单位长度的 n）。 |
| `vector3:Clone()  -> vector3` | 返回一个独立的副本。 |
| `vector3:Set(x, y, z)  -> nil` | 就地设置 X、Y 和 Z。 |
| `vector3:Unpack()  -> number, number, number` | 以三个值返回 X、Y 和 Z。 |
| `vector3:ToString()  -> string` | 返回 "(X, Y, Z)"。 |

### VECTOR4

4D 向量的工厂。

<div class="callout warn">
以全局对象 VECTOR4 的形式提供。
</div>

| 方法 | 说明 |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | 创建一个 4D 向量。 |
| `VECTOR4:Zero()  -> vector4` | 返回 (0, 0, 0, 0)。 |
| `VECTOR4:One()  -> vector4` | 返回 (1, 1, 1, 1)。 |

### Vector4 句柄

带有 X、Y、Z 和 W 字段的 4D 向量。

| 方法 | 说明 |
| --- | --- |
| `vector4.X  -> number` | x 分量（可读写字段）。 |
| `vector4.Y  -> number` | y 分量（可读写字段）。 |
| `vector4.Z  -> number` | z 分量（可读写字段）。 |
| `vector4.W  -> number` | w 分量（可读写字段）。 |
| `vector4:Add(o)  -> vector4` | 返回逐分量之和。 |
| `vector4:Sub(o)  -> vector4` | 返回逐分量之差。 |
| `vector4:Mul(o)  -> vector4` | 返回逐分量之积。 |
| `vector4:Scale(s)  -> vector4` | 返回乘以 s 的向量。 |
| `vector4:Negate()  -> vector4` | 返回 (-X, -Y, -Z, -W)。 |
| `vector4:Dot(o)  -> number` | 返回点积。 |
| `vector4:Length()  -> number` | 返回长度。 |
| `vector4:LengthSq()  -> number` | 返回长度的平方。 |
| `vector4:Distance(o)  -> number` | 返回到 o 的距离。 |
| `vector4:Normalized()  -> vector4` | 返回单位长度的副本；长度接近零时返回 (0, 0, 0, 0)。 |
| `vector4:Lerp(o, t)  -> vector4` | 返回向 o 按比例 t 的线性插值。 |
| `vector4:Clone()  -> vector4` | 返回一个独立的副本。 |
| `vector4:Set(x, y, z, w)  -> nil` | 就地设置 X、Y、Z 和 W。 |
| `vector4:Unpack()  -> number, number, number, number` | 以四个值返回 X、Y、Z 和 W。 |
| `vector4:ToString()  -> string` | 返回 "(X, Y, Z, W)"。 |

## 矩阵

所有矩阵都是行主序的：`Get(r, c)` 读取第 r 行第 c 列，两者都从 1 起。`MulVec` 把向量视为列向量（结果 = M * v）。

### MATRIX

任意尺寸矩阵的工厂。

<div class="callout warn">
以全局对象 MATRIX 的形式提供。尺寸不兼容的运算返回空的 0 x 0 矩阵（或零长度向量），不抛出错误。
</div>

| 方法 | 说明 |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | 创建一个全零矩阵。 |
| `MATRIX:Identity(n)  -> matrix` | 创建一个 n x n 单位矩阵。 |

### 矩阵句柄

任意尺寸的矩阵。

<div class="callout warn">
Add 和 Sub 要求维度一致；Mul 要求本矩阵的列数等于另一个矩阵的行数；MulVec 要求向量长度等于列数。
</div>

| 方法 | 说明 |
| --- | --- |
| `matrix:RowCount()  -> number` | 返回行数。 |
| `matrix:ColCount()  -> number` | 返回列数。 |
| `matrix:Get(r, c)  -> number` | 返回第 r 行第 c 列的元素；超出范围时返回 0。 |
| `matrix:Set(r, c, v)  -> nil` | 设置第 r 行第 c 列的元素；该方法会忽略超出范围的索引。 |
| `matrix:Add(o)  -> matrix` | 返回逐元素之和。 |
| `matrix:Sub(o)  -> matrix` | 返回逐元素之差。 |
| `matrix:Scale(s)  -> matrix` | 返回每个元素都乘以 s 的矩阵。 |
| `matrix:Mul(o)  -> matrix` | 返回矩阵乘积。 |
| `matrix:MulVec(v)  -> vector` | 返回与向量的乘积（见 VECTOR）。 |
| `matrix:Transpose()  -> matrix` | 返回转置矩阵。 |
| `matrix:Clone()  -> matrix` | 返回一个独立的副本。 |

### MATRIX2

2 x 2 矩阵的工厂。

<div class="callout warn">
以全局对象 MATRIX2 的形式提供。Get 和 Set 不检查索引；请保持在 1 到 2 之内。
</div>

| 方法 | 说明 |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | 返回单位矩阵。 |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | 由按行主序给出的四个元素创建矩阵。 |
| `MATRIX2:Rotation(radians)  -> matrix2` | 返回一个逆时针旋转矩阵。 |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | 返回一个缩放矩阵。 |

### Matrix2 句柄

2 x 2 矩阵。

| 方法 | 说明 |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | 返回第 r 行第 c 列的元素。 |
| `matrix2:Set(r, c, v)  -> nil` | 设置第 r 行第 c 列的元素。 |
| `matrix2:Mul(o)  -> matrix2` | 返回矩阵乘积。 |
| `matrix2:MulVec(v)  -> vector2` | 返回与 2D 向量的乘积。 |
| `matrix2:Scale(s)  -> matrix2` | 返回每个元素都乘以 s 的矩阵。 |
| `matrix2:Transpose()  -> matrix2` | 返回转置矩阵。 |
| `matrix2:Determinant()  -> number` | 返回行列式。 |
| `matrix2:Clone()  -> matrix2` | 返回一个独立的副本。 |

### MATRIX3

3 x 3 矩阵的工厂。

<div class="callout warn">
以全局对象 MATRIX3 的形式提供。Get 和 Set 不检查索引；请保持在 1 到 3 之内。
</div>

| 方法 | 说明 |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | 返回单位矩阵。 |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | 由按行主序给出的九个元素创建矩阵。 |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | 返回由偏航、俯仰和翻滚角构建的旋转矩阵。 |

### Matrix3 句柄

3 x 3 矩阵。

| 方法 | 说明 |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | 返回第 r 行第 c 列的元素。 |
| `matrix3:Set(r, c, v)  -> nil` | 设置第 r 行第 c 列的元素。 |
| `matrix3:Mul(o)  -> matrix3` | 返回矩阵乘积。 |
| `matrix3:MulVec(v)  -> vector3` | 返回与 3D 向量的乘积。 |
| `matrix3:Scale(s)  -> matrix3` | 返回每个元素都乘以 s 的矩阵。 |
| `matrix3:Transpose()  -> matrix3` | 返回转置矩阵。 |
| `matrix3:Determinant()  -> number` | 返回行列式。 |
| `matrix3:Clone()  -> matrix3` | 返回一个独立的副本。 |

### MATRIX4

4 x 4 矩阵的工厂。

<div class="callout warn">
以全局对象 MATRIX4 的形式提供。Get 和 Set 不检查索引；请保持在 1 到 4 之内。Translation 把偏移存储在第四列，因此通过 MulVec 作用于 W = 1 的 vector4。
</div>

| 方法 | 说明 |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | 返回单位矩阵。 |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | 返回一个平移矩阵。 |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | 返回一个缩放矩阵。 |

### Matrix4 句柄

4 x 4 矩阵。

| 方法 | 说明 |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | 返回第 r 行第 c 列的元素。 |
| `matrix4:Set(r, c, v)  -> nil` | 设置第 r 行第 c 列的元素。 |
| `matrix4:Mul(o)  -> matrix4` | 返回矩阵乘积。 |
| `matrix4:MulVec(v)  -> vector4` | 返回与 4D 向量的乘积。 |
| `matrix4:Scale(s)  -> matrix4` | 返回每个元素都乘以 s 的矩阵。 |
| `matrix4:Transpose()  -> matrix4` | 返回转置矩阵。 |
| `matrix4:Clone()  -> matrix4` | 返回一个独立的副本。 |

## 四元数

### QUATERNION

四元数的工厂。

<div class="callout warn">
以全局对象 QUATERNION 的形式提供。单位元为 (0, 0, 0, 1)。旋转方法假定单位四元数；手动构建后请调用 Normalized。
</div>

| 方法 | 说明 |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | 返回单位四元数。 |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | 由四个分量创建四元数。 |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | 返回绕轴 (ax, ay, az) 旋转 `angle` 弧度的旋转。该方法会归一化轴；零轴得到单位元。 |

### 四元数句柄

带有 X、Y、Z 和 W 字段的四元数。

| 方法 | 说明 |
| --- | --- |
| `quaternion.X  -> number` | x 分量（可读写字段）。 |
| `quaternion.Y  -> number` | y 分量（可读写字段）。 |
| `quaternion.Z  -> number` | z 分量（可读写字段）。 |
| `quaternion.W  -> number` | w 分量（可读写字段）。 |
| `quaternion:Mul(o)  -> quaternion` | 返回哈密顿积 this * o。应用结果时先按 o 旋转，再按 this 旋转。 |
| `quaternion:Dot(o)  -> number` | 返回点积。 |
| `quaternion:Length()  -> number` | 返回长度。 |
| `quaternion:Normalized()  -> quaternion` | 返回单位长度的副本；长度接近零时返回单位元。 |
| `quaternion:Conjugate()  -> quaternion` | 返回 (-X, -Y, -Z, W)，即单位四元数的逆旋转。 |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | 旋转向量 (vx, vy, vz) 并返回结果的 x、y 和 z。 |
| `quaternion:Slerp(o, t)  -> quaternion` | 返回向 o 按比例 t 的球面线性插值，取最短弧。 |
| `quaternion:Clone()  -> quaternion` | 返回一个独立的副本。 |
| `quaternion:Unpack()  -> number, number, number, number` | 以四个值返回 X、Y、Z 和 W。 |
